# VB → C# ByRef arguments that need a temporary

Sources: [passing arguments by value and by reference](https://learn.microsoft.com/dotnet/visual-basic/programming-guide/language-features/procedures/passing-arguments-by-value-and-by-reference) and Roslyn's VB lowering in [`LocalRewriter_Call.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_Call.vb): `RewriteByRefArgumentWithCopyBack` creates the temporary, and `VisitCall` wraps the call in a sequence whose side effects after the value are the copy-backs. Behaviour below is covered by `Tests/TestData/SelfVerifyingTests/VBToCS/ByRefWriteBackPlacementTests.vb`.

## VB

* A ByRef parameter accepts any expression. If it isn't a variable (a property, a method result, a constant, an omitted `Optional` argument), VB passes a temporary.
* For a writable property (or a `ReadOnly` auto-property inside its constructor), VB copies the temporary back **straight after the call returns**, before the rest of the enclosing expression or statement runs.
* One call copies back its arguments from the last to the first (`copyBackArray.ReverseContents()` in `LocalRewriter_Call.vb`). It's observable when two arguments write back to the same place, e.g. `Two(p.Value, p.Value)`. A call nested in an argument has already copied back by then.

## C#

* `ref`/`out` need a variable, so the temporary must be declared in a statement before the call.
* There is no expression-level copy-back, so a write-back is a statement too.

## Implementation

`ByRefArgumentHoister.HoistByRefDeclaration` hoists the temporary (`AdditionalDeclaration`) and, when VB copies back, the write-back (`AdditionalAssignment`) into the current `PerScopeState` scope. `PerScopeState.CreateLocalsAsync` emits them before and after the converted statement. The output takes the simplest form that keeps VB's timing:

| Where the call is | C# | Code |
|---|---|---|
| A statement, as the last thing evaluated, including the right of an assignment whose target isn't read by the write-back (`Q = F(P)`) | temporary before, write-back after the statement | `PerScopeState.CreateLocalsAsync` |
| `=` to a plain name or `Me.X` that the write-back reads (`P = F(P)`, `x = F(h.Items(x))`, `h = F(h.P)`) | `var ret = call; write-back; P = ret;` | `WriteBackPlacement.IsSimpleTargetReadByWriteBack`, `MethodBodyExecutableStatementVisitor.VisitAssignmentStatement` |
| `Return`, `Throw`, `Yield` | `var ret = call; write-back; return ret;` (only when there is a write-back) | `PerScopeState.WithWriteBacksBefore` |
| Followed by other evaluation, or in a condition or block header (`If`, `ElseIf`, loops, `Select Case`, `Case`, `For`, `For Each`, `Using`, `SyncLock`, `With`, `Catch When`), or in one of multiple `Dim` declarators, or the right of a compound assignment, or one whose target has a receiver or index, that the write-back reads (`h.P = F(h.P)`, `x += F(h.Items(x))`): C# evaluates the target first and stores last, as VB does, and the write-back happens in between | a local function that declares, calls and writes back, called in place | `WriteBackPlacement.RequiresLocalFunction`, `HoistAndCallLocalFunctionAsync` |
| A field initializer | a method that declares, writes back and returns, called from the field (static) or the constructor (instance) | `CommonConversions.WithHoistedNodesInMethodAsync`, `PerScopeState.CreateReturnBlockAsync` |

Within each of these, one call's write-backs are hoisted after its whole argument list, last argument first, so a nested call's come before them: `ByRefArgumentHoister.WithWriteBacksInVbOrderAsync`.

In a `Structure`, a local function can't use `this` (CS1673). When the call uses an instance member, the local function takes `ref S self` and is called with `ref this`, and its body uses `self` for `this` (the converted body always qualifies instance members, which are only simplified later). `ref this` is legal wherever VB can use `Me` in a structure; VB has no read-only members (where it's CS1605) and no `Me` in a structure's lambdas (BC36638): `WriteBackPlacement.HoistAndCallLocalFunctionAsync`.

A local function can't use a ref local either (CS8175), such as the `ref var withBlock` of a `With` over a structure field. References to a ref local are marked with its type, and the local function takes each one it uses from outside its body as a `ref` parameter of the same name: `RefLocalReferences`. The enclosing method's ByRef parameters are passed the same way (CS1628), including one reused directly as a `With` receiver (`With b : Inc(.X)` becomes `Inc(ref b.X)`): `WriteBackPlacement.DescendantsResolvingWithReceivers`.

The classes in this folder hold the ByRef-specific decisions. The placement mechanics stay with the code they extend: the hoisted node types and `PerScopeState` (the hoisting for every feature), `MethodBodyExecutableStatementVisitor` (jumps) and `CommonConversions` (field initializers, whose method wraps any hoisted statement, not only ByRef ones).

### What the write-back writes to

Source: Roslyn's [`UseTwiceRewriter.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/UseTwiceRewriter.vb), which `RewriteByRefArgumentWithCopyBack` uses to read the target and then write it with the setter. Behaviour is covered by `Tests/TestData/SelfVerifyingTests/VBToCS/ByRefParameterizedProperty*.vb`.

* VB evaluates the target's receiver and index arguments once, and the write-back reuses their values. It copies each into a temporary except literals, constants and `Me`.
* The converter hoists a `tmp` only where the second evaluation could give a different value: `ByRefTargetTemporaries`.
  * A write is anything in `VariableWrites`: an assignment, compound assignment, ByRef or late-bound argument, `For`/`For Each` control variable, `ReDim`, `Erase` or `Mid` statement.
  * A local or `ByVal` parameter is reused unless the enclosing expression, or any lambda in the same method (outside a method, the outermost enclosing lambda), writes it: the call could run that lambda. Only those can. A `ByRef` parameter aliases a variable the call might reach, and a `Static` local outlives the call like a field, so both are copied.
  * An intrinsic unary or binary operator, or a conversion, on reusable operands is reused when it gives the same result, or throws the same exception, again: between numeric, `Boolean`, `Char` and enum types (or their nullables), or an identity, widening reference or boxing conversion. Anything involving `String` or `Date` can read the current culture, which the call can change (`d & ""` gives `1.5` or `1,5`), one on `Object` is late bound, and a user-defined one runs user code, so those are copied.
  * A field is reused only if, in addition, the call can't change it (`IFieldMutationOracle.MayChangeDuringCall`). `FieldWriteAnalysis` answers that once per project (created by `VBToCSProjectContentsConverter`, matching fields by documentation ID since each document gets its own compilation), soundly without knowing what the call reaches: a field can't change if it's `Const` or `ReadOnly`, or if only this compilation can write it (it, or a type containing it, is `Private`, or `Friend` with no `InternalsVisibleTo`) and nothing in it writes it outside construction. Construction (a constructor writing its own instance's field, a shared constructor writing a shared field, or a field or property initializer) has finished before anything can read the field, but a lambda in it may run later, so that isn't excluded. A structure field (other than a primitive or enum) is always treated as changeable, since calling a method on it can change it. So is an auto-property's backing field (`_Idx`), which is written through the property, and any instance field of a structure, even `ReadOnly`: assigning the whole structure (`s = New S(9)`) overwrites it.
  * A call graph of method references could narrow this to the fields the call can transitively write. To stay sound it would treat overridable and interface calls as reaching every implementation, delegate invocations and late-bound calls as reaching anything a delegate or late-bound call could, and calls outside the compilation as reaching any field they can see.
* A parameterized property (not the default one) becomes `get_X`/`set_X` methods, so its write-back calls the setter with the same index arguments: `ByRefArgumentHoister.CreateWriteBack`, `AdditionalAssignment.ViaSetter`. It takes its place in the call's reversed write-back order like any other.
* A `ReadOnly` property isn't written back (`GetRefConversionForExpression`), so it's evaluated once and needs no temporaries.

| VB target | C# |
|---|---|
| `Qr(i, 2)`, `Qr(i + 1, -i)`, `other.Qr(_index, 2)` (`_index` a `Private` field written only in constructors) | `get_Qr(i, 2)` … `set_Qr(i, 2, arg)` |
| `other.Qr(_writtenElsewhere, 2)`, `other.Qr(PublicIndex, 2)` | `var tmp = _writtenElsewhere;` … `other.set_Qr(tmp, 2, arg)` |
| `other.Qr(NextIndex(), 2)` | `var tmp = NextIndex();` … `other.get_Qr(tmp, 2)` … `other.set_Qr(tmp, 2, arg)` |
| `GetC().Qr(1, 2)`, `GetC().Plain` | `var tmp = GetC();` … `tmp.set_Qr(1, 2, arg)`, `tmp.Plain = arg` |
| `other(NextIndex())`, `Me(i, 5)` (default property, indexer or array) | `var tmp = NextIndex();` … `other[tmp] = arg`, `this[i, 5] = arg`: the container follows the receiver rules |
| `Two(other.Qr(i, 2), i)` | `var tmp = i;` … `other.set_Qr(tmp, 2, arg)` |
| `cells(NextIndex()).Text`, `GetHolder().Cell.Text` (`Cell` is a structure) | `var tmp = NextIndex();` … `cells[tmp].Text = arg`, `var tmp = GetHolder();` … `tmp.Cell.Text = arg`: a structure variable is written in place (`ByRefTargetTemporaries.HoistInstance`) |

Known gaps:
* A parameterized property with a `ParamArray` converts to a setter with `params` before the value (`set_PA(params int[] idx, string value)`, CS0231), so it can't be written back.
* An array element whose array isn't a variable, passed to a ByRef parameter of a wider type (`BumpLong(GetArr()(Next1()))` with an `Integer` array and a `ByRef Long` parameter), isn't written back: `IsRefArrayAcces` only treats it as an array element when the array is passed inline. VB writes it back.
* The receiver of an extension method that takes it ByRef has an empty `NoneOperation` in Roslyn's VB operation tree, so it gets none of the above: a parameterized property there stays `this.Qr(2)` (CS1061), and a receiver like `GetC().Plain` is evaluated twice. A fix would rebuild the target from symbols and syntax.
* A late-bound parameterized property (`SetValue(o.Qr(1))` with `o As Object`) converts to `((dynamic)o).Qr(1)`, which throws `RuntimeBinderException`, and isn't written back. VB reads and writes it through `NewLateBinding.LateGet`/`LateSet` (or `LateIndexGet`/`LateIndexSet`).
* Arguments of `New` (object creation) in a block header still write back after the block.
* A non-constant `For ... Step` is re-evaluated each iteration (not ByRef-specific).
* An assignment is only seen to change what's written back when the same symbol appears in both, e.g. `h.P = F(h.P)`. So:
  * `h1.P = F(h2.P)` where `h1 Is h2` still writes back after the assignment.
  * `a.P = F(b.P)` takes the local function even when `a` and `b` are different objects, because two names can always refer to the same object.
* `WF = New W(WF.Name)`, a ByRef constructor argument in an assignment, writes back to the new object: object creation doesn't take the assignment placement.
* A setter with side effects on the written-back target, e.g. `Other = F(P)` where `Other`'s setter writes `P`, runs before the write-back instead of after it.
* A lambda parameter inside a ByRef argument (`Function(x) Inc(x.P)`) hoists the local function outside the lambda, where `x` isn't in scope (CS0103).
* An auto-property initializer (`Public Property X As Integer = Inc(P)`) leaves a `ph…` placeholder (CS0103): it doesn't go through the field initializer path.
* Partial-class instance initializers run in part order rather than declaration order.
* A member of a structure returned by a property (`Inc(InnerProp.P)`) writes back with `InnerProp.P = argv`, which C# rejects because `InnerProp` returns a copy (CS1612).
* `Select Case Inc(P)` with `Case Inc(P)` gives `case var @case when ...` (CS0825).
* A `ReadOnly` auto-property is assignable in its type's constructor and initializers, so VB writes it back there, but `GetRefConversionForExpression` treats every read-only property as not writable. A fix also needs the C# write-back target to be assignable from the field initializer method.
