# VB → C# query expression conversion

This describes how VB query syntax (`From x In xs Where ... Select ...`) is converted to C#, why it's done this way, and the constraints it works within.
The code is in this folder: `QueryClauseConverter` (one VB clause at a time) builds into a `CsQueryBuilder` (the C# query under construction), and `RangeVariableReplacements` lets the normal expression conversion substitute the right C# expression for each VB range variable reference.

## Why the previous approach didn't work

The previous converter split the VB clauses into "segments" up front based on clause kinds, converted each clause syntactically, and patched up the shapes afterwards (e.g. turning a VB `Select` into a C# `let`).
Most of the reported bugs come from the same root cause: **VB and C# disagree about which range variables are in scope after each clause, and what a query returns when it doesn't end in `Select`**. Converting clause syntax one-to-one can't represent that, so each new combination needed another special case:

| Issue | Symptom | Root cause |
|---|---|---|
| [#1163](https://github.com/icsharpcode/CodeConverter/issues/1163) | `Select i = ... Where ...` returned the original element | Implicit final select used the first `From` variable, not VB's scope |
| [#1172](https://github.com/icsharpcode/CodeConverter/issues/1172), [#1016](https://github.com/icsharpcode/CodeConverter/issues/1016) | `Select a, b Order By ...` threw / produced invalid order | Multi-variable `Select` mid-query wasn't representable |
| [#931](https://github.com/icsharpcode/CodeConverter/issues/931) | Query without `Select` returned only the first variable | VB returns an anonymous type of *all* range variables in scope |
| [#936](https://github.com/icsharpcode/CodeConverter/issues/936) | `On a.X - 1 Equals b.Y` threw | Join sides were decided by the *syntax* of the left expression |
| [#935](https://github.com/icsharpcode/CodeConverter/issues/935) | `Group Join ... Into Group` threw | Unnamed `Group` aggregation assumed to have a name |
| [#1014](https://github.com/icsharpcode/CodeConverter/issues/1014), [#298](https://github.com/icsharpcode/CodeConverter/issues/298) | `Group x By a, b Into ...` threw | Only single keys/items were handled |
| [#1272](https://github.com/icsharpcode/CodeConverter/issues/1272), [#867](https://github.com/icsharpcode/CodeConverter/issues/867) | Aggregations (`Into Max(...)`) silently dropped | Aggregation functions weren't converted for `Group By` |
| [#948](https://github.com/icsharpcode/CodeConverter/issues/948), [#1180](https://github.com/icsharpcode/CodeConverter/issues/1180) | References to group keys didn't compile, group renamed | After `Group By`, VB keys are range variables, C# only has `g.Key` |

## VB query semantics

Source of truth: the VB binder, [`Binder_Query.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Binding/Binder_Query.vb), whose doc comments give the method-call translation of each operator, plus the [VB spec on query expressions](https://learn.microsoft.com/dotnet/visual-basic/language-reference/queries/).
The behaviours below were also confirmed by compiling and running VB.

At every point in a VB query there's a set of **range variables in scope** (the "control variables"). The *element* flowing through the query is that set: a single variable is the value itself, several form an anonymous type with one property per variable (flat, in declaration order, all `Key` properties so they have value equality).

| VB clause | Translation (Binder_Query.vb) | Scope afterwards |
|---|---|---|
| `From x In xs` (first) | source (implicitly `AsEnumerable()`/`Cast(Of Object)()` where needed) | `{x}` |
| `From y In ys` / `, y In ys` | `SelectMany` | adds `y` |
| `From x As T In xs` | `Select(Function(x) CType(x, T))` (not `Cast`) | |
| `Let a = e` | `Select(... New With {..., a})` | adds `a` |
| `Where c` | `Where` | unchanged |
| `Order By k [Descending], ...` | `OrderBy`/`ThenBy` | unchanged |
| `Select a = e1, e2` (not last) | `Select` | **replaced** by the selected names (an unnamed single expression has no accessible name) |
| `Distinct`, `Skip n`, `Take n` | method call | unchanged |
| `Skip While c`, `Take While c` | method call with lambda | unchanged |
| `Join y In ys On a Equals b [And ...]` | `Join` (sides may be written either way round; nested `Join`s allowed before `On`) | adds `y` |
| `Group Join y In ys On ... Into g = Group, n = Count()` | `GroupJoin` | adds the aggregations, **not** `y` |
| `Group [items] By keys Into aggs` | `GroupBy` | **replaced** by keys then aggregations (`Into Group` names a variable `Group`) |
| `Aggregate y In ys [ops] Into f(...)` (within a query) | `Let f = (From y In ys [ops]).f(...)`; several aggregations share one evaluated group | adds the aggregations |
| `Aggregate y In ys [ops] Into ...` (whole query) | one value, or an anonymous type for several aggregations; the source is evaluated once | n/a |

* If the query doesn't end with `Select`, the result is the element at the end, e.g. `From x In xs, y In ys` returns `{x, y}`, `Group By x Into Group` returns `{x, Group}`.
* Aggregation functions (`Sum(x.A)`, `Count()`, `Any(cond)`, any extension method) are calls on the group with a lambda over the *group's element*. In `Group By`, that's the `Group` items if given, otherwise the element before grouping; references inside resolve to the range variables of that element.
* **Option Compare Text** only affects string comparison *operators* (`=`, `<`, ...) in clause expressions. `Group By`, `Distinct` and `Join` use the default equality comparer, and `Order By` the default comparer, regardless of `Option Compare` (see `LinqOptionCompareTextTests.vb`).

## C# query semantics and constraints

From the [C# spec](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/expressions#1222-query-expressions) and [`Binder_Query.cs`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Binder/Binder_Query.cs):

* Grammar: `from` *body-clause*\* (`select` | `group ... by ...`) (`into` *id* *body*)?. Body clauses are only `from`, `let`, `where`, `join [into]`, `orderby`.
* Variables only accumulate within a body: `from`, `let`, `join` add one; `join ... into g` adds `g` but not the joined variable. The only way to drop variables is to end the body and continue with `into`, after which *only* the continuation variable is in scope.
* A range variable can't be redeclared while in scope (CS1930), or clash with a local/parameter in scope (CS1931/CS0136).
* `join x in xs on outer equals inner`: the outer key may only use earlier variables, the inner key only `x` (CS1937/CS1938). Multiple conditions must be combined into one key, e.g. matching anonymous types.
* No clauses for `Distinct`/`Skip`/`Take`/`SkipWhile`/`TakeWhile`/aggregates: these need method calls.
* `from T x in xs` means `xs.Cast<T>()`, which only does reference/unboxing conversions, unlike VB's `CType`.
* A query must end in `select` or `group`. A generated trailing `select x` where `x` is the only `from` variable can be dropped (VB adds one to a bare `From x In xs`), but an explicit `Select x` is kept: it changes the result's type from e.g. an array to `IEnumerable`, in both languages.

## Design

### Model

`QueryClauseConverter` walks the VB clauses left to right, maintaining:

1. **VB scope** (`CsQueryBuilder.Scope`): the `QueryRangeVariable`s VB has in scope, in order. This alone determines the element.
2. **How C# evaluates each VB variable** (`QueryRangeVariable.CsExpression`): `x`, `g.Key.P1`, `g.Sum(t => t.A)`, `e.x` etc. This changes as the C# query evolves.
3. **The C# query so far** (`CsQueryBuilder`): the first `from`, completed bodies ended by `select/group ... into`, the current body's clauses, *or* just a source expression when the query has been interrupted by a method call.

`RangeVariableReplacements` maps each VB range variable **symbol** to its `QueryRangeVariable`. `NameExpressionNodeVisitor.ConvertIdentifierNameAsync` checks it first, so every reference anywhere in a clause (including nested lambdas and nested queries) is converted to the current C# expression. Using symbols rather than names means shadowing, VB's case-insensitivity, and references resolved to an inner/outer query are all decided by the VB binder, not by us.

Because the element is always constructed from the VB scope, the C# query is free to keep extra variables in scope (it can't drop them anyway) without changing the result. This removes the need for most continuations.

### Clause mapping

| VB | C# |
|---|---|
| `From`, `Let`, `Where`, `Order By` | same clause |
| `Select` (last) | `select e` / `select new { ... }` |
| `Select` (not last) | no clause: each selected value is defined (see below) and the scope replaced; clashing names are renamed (`let x1 = x * 2`) |
| `Join` | `join`, with the inner side chosen by which side references the joined variable's symbol; nested joins flattened, each condition attached to the earliest join where all it references is in scope |
| `Join`/`Group Join` with nested joins that can't be flattened | the joined variable and nested joins become a subquery (VB's own translation is `xs.Join(ys.Join(zs, ...), ...)`), joined as one compound variable whose members the VB variables refer to, e.g. `join x1 in (from y in ys join z in zs ... select new { y, z }) on x equals x1.y + x1.z` |
| `Group Join` | `join ... into g`, aggregations defined over `g` with the joined element |
| `Group By` | `group element by key into g`; keys become `g.Key` / `g.Key.Name`, aggregations `g.F(element => ...)` |
| `Distinct`, `Skip`, `Take`, `Skip While`, `Take While` | **method boundary**: materialize the element with `select`, call the method, and if more clauses follow, resume with `from e in (...).Method()` mapping each variable to `e` / `e.Name` |
| `Aggregate` (within) | aggregations defined over the nested query (held in a `let` when several share it) |
| `Aggregate` (whole query) | `source.F(...)`, or `new { a = source.F(...), ... }` |

### Defining a value: inline or `let`

`Let`, mid-query `Select` and aggregations all produce a value for a new variable. `CsQueryBuilder.Define` either substitutes the expression at each use (nicest output) or adds a `let` clause. Substitution must not change how many times the value is evaluated, so it's used when (`QueryClauseConverter.CanInline`):

* the expression is cheap to repeat (identifier/member access chain/literal), or
* it isn't referenced by later clauses (so it's at most evaluated as part of the element, once), or
* its only reference is in the final `Select` and not inside a nested lambda/query,

counting only references before the next method boundary or the end of a join subquery (after which the variable is read from the materialized element).
Substitution is never used if a lambda parameter written later in the query has the same name as an identifier in the expression: C# keeps variables in scope that VB has dropped, so in `Select y = x Where ys.Any(Function(x) x > y)`, substituting `x` for `y` would be captured by the lambda's `x`.
This is what turns `Group By ... Into Summary = Sum(t.A) Where Summary > 0 Select ..., Summary` into `let Summary = Group.Sum(t => t.A)`, but `Into ValueMax = Max(item.Value) Select ..., ValueMax` into `ValueMax = Group.Max(item => item.Value)` directly in the select.

### Progressive fallback

Output gets less idiomatic only as the input requires it:

1. One-to-one clauses, with `select` built from VB's scope.
2. `let` for values used more than once; renamed variables only on clashes.
3. `group ... into` continuation for `Group By`.
4. Method calls wrapped round a query for operators with no clause, resuming with `from` only if needed (`xs.Skip(1)` stays `xs.Skip(1)` when nothing else happens).
5. For the rare whole-query `Aggregate` with several aggregations over a non-trivial source: `new[] { source }.Select(Group => new { ... }).Single()` so the source is evaluated once.
6. Nested joins that can't be flattened (a `Group Join` among them, or a condition whose inner side uses several joined variables) join against a subquery of the nested joins.
7. Anything unrepresentable throws `NotImplementedException`, which becomes a compile error comment in the output.

### Names

VB-declared names are used as-is unless they clash with a C# range variable still in scope (e.g. VB's `Select x = x * 2` makes the old `x` inaccessible, C# doesn't), in which case a number is appended.
Generated names (`Group` for groups VB doesn't name, `x` for compound element parameters) avoid anything visible from the query (including enclosing lambda parameters), anything declared within it (e.g. lambda parameters), every name used earlier in it, and names in enclosing queries (tracked via the stack of active builders, which also lets nested `Aggregate` queries avoid clashes).
Where VB infers an anonymous type member name (`New With {ValueMax}`), the C# member is named explicitly if the substituted expression would infer a different name.

### Comments and line mapping

Clause expressions keep their source mapping. Inlined values are moved, so their mapping is removed unless they're used straight away (next clause materializes the element), and a generated `select` before a method call is mapped to that clause's line so line breaks/comments stay in order.

### Cross-cutting: comparisons, nullables and Option Compare Text

Inside queries `IQueryTracker.IsWithinQuery` makes comparisons simple (`==`, no VB null/empty-string coercion) so that expression-tree providers like EF can translate them; this predates this change and is deliberately applied to all queries.
The exception is `Option Compare Text` for queries that run as delegates: dropping it would change which strings match, so equality stays case-insensitive. Whether a query is an expression tree comes from the VB compiler's translation (`ITranslatedQueryOperation`): if any lambda argument is an `Expression<T>`, it is. Only the operator calls the compiler generates count, not user lambdas within the query, and a query nested within an expression tree is part of it. In an expression tree, VB emits `CompareString(..., TextCompare:=True)` which providers translate to plain equality anyway, so `==` is kept there (consistent with how expression lambdas were already converted, see `Issue316_LambdaExpressionEqualityCheckAsync`).

## Known limitations and trade-offs

* Expressions with side effects can be evaluated at a different point relative to sorting/filtering (e.g. an inlined `Let` evaluated after `Order By`), but the same number of times per element. Queries generally can't guarantee this anyway (they may be translated to SQL).
* Comparisons in queries other than Option Compare Text keep the simple expression-tree-friendly form, so VB's `Nothing = ""` string semantics and three-valued nullable logic aren't reproduced in LINQ to objects queries (unlike method-syntax lambdas). This was an existing deliberate trade-off; `IsWithinExpressionTree` now makes it possible to revisit.
* Clauses after an initial `Aggregate ... Into` aren't converted.
* A comma separated `From x In {1}, y In ...` gets a blank line after the first variable, a trivia mapping quirk that predates this change.

## Tests

* `Tests/CSharp/ExpressionTests/LinqQueryScopeTests.cs`: scope after `Select`/`Let`, implicit final select, method boundaries, typed sources.
* `LinqQueryJoinTests.cs`, `LinqQueryGroupByTests.cs`, `LinqQueryAggregateTests.cs`: per clause, including each reported issue.
* `LinqExpressionTests.cs`: older characterisation tests and Option Compare Text output.
* `Tests/TestData/SelfVerifyingTests/VBToCS/LinqExpressionTests.vb` and `LinqOptionCompareTextTests.vb`: run the VB and the converted C# and compare results.
