# VB → C# conversion

## ByRef arguments that need a temporary

Sources: [passing arguments by value and by reference](https://learn.microsoft.com/dotnet/visual-basic/programming-guide/language-features/procedures/passing-arguments-by-value-and-by-reference) and Roslyn's VB lowering in [`LocalRewriter_Call.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_Call.vb): `RewriteByRefArgumentWithCopyBack` creates the temporary, and `VisitCall` wraps the call in a sequence whose side effects after the value are the copy-backs. Behaviour below is covered by `Tests/TestData/SelfVerifyingTests/VBToCS/ByRefWriteBackPlacementTests.vb`.

### VB

* A ByRef parameter accepts any expression. If it isn't a variable (a property, a method result, a constant, an omitted `Optional` argument), VB passes a temporary.
* For a writable property (or a `ReadOnly` auto-property inside its constructor), VB copies the temporary back **straight after the call returns**, before the rest of the enclosing expression or statement runs.
* One call copies back its arguments from the last to the first (`copyBackArray.ReverseContents()` in `LocalRewriter_Call.vb`). It's observable when two arguments write back to the same place, e.g. `Two(p.Value, p.Value)`. A call nested in an argument has already copied back by then.

### C#

* `ref`/`out` need a variable, so the temporary must be declared in a statement before the call.
* There is no expression-level copy-back, so a write-back is a statement too.

### Implementation

`ArgumentConverter.HoistByRefDeclaration` hoists the temporary (`AdditionalDeclaration`) and, when VB copies back, the write-back (`AdditionalAssignment`) into the current `PerScopeState` scope. `PerScopeState.CreateLocalsAsync` emits them before and after the converted statement. The output takes the simplest form that keeps VB's timing:

| Where the call is | C# | Code |
|---|---|---|
| A statement, as the last thing evaluated, including the right of an assignment whose target isn't read by the write-back (`Q = F(P)`) | temporary before, write-back after the statement | `PerScopeState.CreateLocalsAsync` |
| `Return`, `Throw`, `Yield` | `var ret = call; write-back; return ret;` (only when there is a write-back) | `PerScopeState.WithWriteBacksBefore` |
| Followed by other evaluation, or in a condition or block header (`If`, `ElseIf`, loops, `Select Case`, `Case`, `For`, `For Each`, `Using`, `SyncLock`, `With`, `Catch When`), or in a `Dim` declarator that isn't the only one, or the right of an assignment to something the write-back reads (`P = F(P)`, `x = F(h.Items(x))`, `h = F(h.P)`): C# evaluates the assignment target first and stores last, as VB does, and the write-back happens in between | a local function that declares, calls and writes back, called in place | `NameExpressionNodeVisitor.RequiresLocalFunction`, `HoistAndCallLocalFunctionAsync` |
| A field initializer | a method that declares, writes back and returns, called from the field (static) or the constructor (instance) | `CommonConversions.WithHoistedNodesInMethodAsync`, `PerScopeState.CreateReturnBlockAsync` |

Within each of these, one call's write-backs are hoisted after its whole argument list, last argument first, so a nested call's come before them: `ArgumentConverter.WithWriteBacksInVbOrderAsync`.

Known gaps:
* Arguments of `New` (object creation) in a block header still write back after the block.
* A non-constant `For ... Step` is re-evaluated each iteration (not ByRef-specific).
* A lambda parameter inside a ByRef argument (`Function(x) Inc(x.P)`) hoists the local function outside the lambda, where `x` isn't in scope (CS0103).
* An auto-property initializer (`Public Property X As Integer = Inc(P)`) leaves a `ph…` placeholder (CS0103): it doesn't go through the field initializer path.
* Partial-class instance initializers run in part order rather than declaration order.
* A `ReadOnly` auto-property is assignable in its type's constructor and initializers, so VB writes it back there, but `GetRefConversionForExpression` treats every read-only property as not writable. A fix also needs the C# write-back target to be assignable from the field initializer method.
