# VB → C# query expressions

How VB query syntax (`From x In xs Where ... Select ...`) is converted, and the language rules each part relies on.

Sources:
* VB: [`Binder_Query.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Binding/Binder_Query.vb) (each `Bind*Clause` documents its method-call translation) and the [query expression docs](https://learn.microsoft.com/dotnet/visual-basic/language-reference/queries/). Behaviour noted below was also checked by compiling and running VB, and is covered by the self-verifying tests.
* C#: [query expressions in the spec](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/expressions#1222-query-expressions) and [`Binder_Query.cs`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Binder/Binder_Query.cs).

## Requirements

### VB

At each point in a query, a set of range variables is in scope. The **element** passed between operators is that set: the value itself for one variable, otherwise an anonymous type with a `Key` property per variable (so it has value equality).

| Clause | Scope afterwards |
|---|---|
| `From`, `Let`, `Join`, `Aggregate` (within a query) | adds variables |
| `Where`, `Order By`, `Distinct`, `Skip`, `Take`, `Skip While`, `Take While` | unchanged |
| `Select` | **replaced** by the selected values (a single unnamed value can't be referenced) |
| `Group By keys Into aggregations` | **replaced** by the keys then aggregations |
| `Group Join y In ys ... Into aggregations` | adds the aggregations, not `y` |

* A query without a final `Select` returns the element.
* An aggregation (`Sum(x.A)`, `Count()`, `Group`, or any extension method) is a call on the group with a lambda over the group's element: `Group By` items if given, otherwise the element before grouping.
* `Aggregate` with several aggregations evaluates its source once.
* Joins can be nested before `On`: `Join y In ys Join z In zs On z Equals y On x Equals y`.
* `From x As T In xs` converts each element with `CType`, which isn't C#'s `Cast<T>()`. Non-generic sources are implicitly `Cast(Of Object)`.
* `Option Compare Text` affects comparison operators in clauses only. `Group By`, `Distinct` and `Join` use default equality, `Order By` the default comparer.

### C#

* Clauses only add range variables, except that `join ... into g` doesn't add the joined variable. The only way to drop variables is `select/group ... into x`, which leaves only `x`.
* A range variable can't be redeclared, or clash with a local (CS1930, CS1931).
* `join`: the outer key may only use earlier variables, the inner key only the joined one (CS1937, CS1938). Several conditions need a composite key.
* No clauses for `Distinct`, `Skip`, `Take`, `SkipWhile`, `TakeWhile` or aggregates.
* `from T x in xs` means `xs.Cast<T>()`.

## Implementation

`QueryClauseConverter` converts clauses left to right into a `CsQueryBuilder`, which tracks:
* `Scope`: the VB range variables in scope. The element is always built from this.
* `QueryRangeVariable.CsExpression`: the C# expression that evaluates each one, e.g. `x`, `g.Key.P1`, `g.Sum(t => t.A)`.

`RangeVariableReplacements` is checked when converting any identifier, so every reference to a range variable symbol (including in nested lambdas and queries) becomes its current `CsExpression`.

Since the element comes from `Scope`, C# can keep variables in scope that VB has dropped. The output stays as plain as the input allows:

| VB | C# | Code |
|---|---|---|
| `From`, `Let`, `Where`, `Order By` | same clause | `QueryClauseConverter.cs` |
| `Select` (last clause) | `select` | `ConvertFinalSelectAsync` |
| `Select` (otherwise) | no clause: each value is substituted or a `let`; scope replaced | `ConvertSelectAsync` |
| `Distinct`, `Skip`, `Take`, `Skip While`, `Take While` | `(... select element).Method()`, then `from e in` that to continue | `CsQueryBuilder.ApplyMethod` |
| `Group By` | `group element by key into g`; keys become `g.Key[.Name]`, aggregations `g.F(e => ...)` | `QueryClauseConverter.Grouping.cs` |
| `Aggregate` | `source.F(...)`; several share one evaluation of the source | `QueryClauseConverter.Grouping.cs` |
| `Join`, `Group Join` | `join [into]`, inner side chosen by which side references the joined variable | `QueryClauseConverter.Joins.cs` |
| Nested joins | flat `join`s where valid, otherwise a subquery joined as one variable (VB's own `xs.Join(ys.Join(zs, ...))`) | `FlatJoinPlanner`, `DeclareJoinedSubqueryAsync` |
| `From x As T` | `from T x` where the conversion is the same as `Cast<T>`, otherwise `xs.Select(x => (T)x)` | `QuerySourceConverter` |
| unsupported shapes | `NotImplementedException`, which shows as a compile error comment | |

### Substitute or `let`

A value (`Let`, mid-query `Select`, aggregation) is substituted into its uses when that can't change behaviour (`SubstitutionRules`):
* Unless it's cheap to repeat, it must be evaluated for the same elements, in the same order, the same number of times. VB evaluates it once per element reaching its clause, in declaration order. Substituted, it's evaluated:
  * where the element is next materialized, if it has no references before then;
  * or at a single reference in the final `Select`, if that isn't in a lambda, nested query or conditionally evaluated operand (`If`, `AndAlso`, `OrElse`, `?.`), and the final `Select` evaluates nothing before it but cheap expressions and earlier values.

  Only `Let` clauses may come in between: others filter, reorder or repeat elements, or (`Select`) drop the value. An unused value with nowhere to be evaluated stays a `let`.
* Lambdas stay in a `let`, typed as a delegate, since C# can't infer a lambda's type there.
* No name it references may be captured by a later declaration. C# keeps variables VB has dropped, so `Select y = x Where ys.Any(Function(x) x > y)` must not substitute `x` for `y`; the same goes for locals and loop variables in later lambdas. A later range variable can capture a field or local (`Select v = field Let field = 1 Select v`), though range variables clashing with each other are renamed instead.

### Types

C# infers a range variable's type from its value, so values get VB's declared type (`Let s As String = x`) where it differs, `Nothing` is cast to its type, and lambdas are created as their delegate type. `Where` and `Skip`/`Take While` conditions are converted to `Boolean` as in `If` statements, so a null `Boolean?` is false.

### Names

VB names are kept unless they clash with a C# range variable VB has dropped, which then gets a numeric suffix. Generated names (`Group`, or `x` for an element) avoid everything visible to or declared within the query, earlier names in it, and enclosing queries.

### Comparisons

VB's own translation (`ITranslatedQueryOperation`) shows whether a query's lambdas compile to delegates or to expression trees (`ExpressionTreeDetector`, `IQueryTracker.IsWithinExpressionTree`). A later `Aggregate` clause is a separate query over its own source, so is checked separately.
* Delegates (LINQ to objects) get VB's comparison semantics, exactly as outside a query: `Nothing = ""`, `Option Compare Text`, nullable logic. In a `Where` (and the operands of `OrElse`/`AndAlso` within it) Nothing is treated as False, so C#'s lifted operators are used where they're already false whenever VB would give Nothing: `n == 1`, `n > 1`, but `n is { } a && a != 1`.
* Expression trees (e.g. `IQueryable` from EF) get plain C# operators (`x == s`). The provider decides the semantics, e.g. the database collation decides case sensitivity, and it may not translate VB's runtime helpers or C# patterns at all ([#316](https://github.com/icsharpcode/CodeConverter/issues/316), [#894](https://github.com/icsharpcode/CodeConverter/issues/894)).

## Limitations

* Values that are cheap to repeat (names, member accesses and literals) are always substituted, so a property getter with side effects could run a different number of times.
* An `IQueryable` evaluated in memory (e.g. `list.AsQueryable()`, or a provider evaluating a comparison client-side) runs the plain C# comparisons, so differs from VB for `Nothing = ""`, nullable logic and `Option Compare Text`.

## Tests

* `Tests/CSharp/ExpressionTests/LinqQuery*Tests.cs`: output for each clause and reported issue.
* `Tests/TestData/SelfVerifyingTests/VBToCS/Linq*.vb`: run the VB and the converted C#, and compare results.
