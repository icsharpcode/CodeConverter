# VB → C# unstructured error handling (`On Error`, `Resume`) — design

Status: the model, shape selection, `Err` usage, shapes 1 (structured), 2 (resume next), 3 (handler function) and 4 (dynamic, for resume next only) are implemented. What the emitters can't express keeps the previous output (`PendingShapeFallback`: one `try`/`catch` when there's exactly one label and one `On Error GoTo`, otherwise statement by statement with `#error`). Tracks [icsharpcode/CodeConverter#426](https://github.com/icsharpcode/CodeConverter/issues/426).

Sources:
* VB: [`LocalRewriter_UnstructuredExceptionHandling.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_UnstructuredExceptionHandling.vb) (the lowering, with pseudo-code of the emitted method), the `RegisterUnstructuredExceptionHandlingResumeTarget` calls in the sibling `LocalRewriter_*.vb` files (which statements are resume points), and the [On Error](https://learn.microsoft.com/dotnet/visual-basic/language-reference/statements/on-error-statement) / [Resume](https://learn.microsoft.com/dotnet/visual-basic/language-reference/statements/resume-statement) docs.
* C#: [the `goto` statement](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/statements#13104-the-goto-statement) (a label is only in scope within its block, so no jumping into a block or a `try`) and [exception filters](https://learn.microsoft.com/dotnet/csharp/language-reference/statements/exception-handling-statements#exception-filters).
* Every behaviour in "Requirements" was checked by compiling and running VB (.NET 10). Every C# shape in "Output shapes" was hand-written, run, and compared with the VB output, except where marked *not spiked*.

## Requirements

### VB

A method containing `On Error` or `Resume` is compiled as one `Try` around the whole body, with three hidden locals:

| Local | Meaning |
|---|---|
| active handler | none, resume-next, or one of the method's handler labels. Set by each `On Error` statement **when it executes**. |
| resume target | non-zero while "in a handler", i.e. from an error being caught until `Resume*` or `On Error GoTo -1`. |
| current statement | index of the statement being executed, only tracked if the method contains `Resume` / `Resume Next` / `On Error Resume Next`. |

The single catch is `Catch ex As Exception When activeHandler <> 0 And resumeTarget = 0`. Consequences:

* The active handler is runtime state, not lexical: `If flag Then On Error Resume Next` changes how every later statement behaves.
* Any exception is caught, including ones thrown deep inside callees.
* An error while in a handler fails the filter and propagates to the caller. As it is a filter, nothing is unwound first.
* The resume point is the innermost statement. Each declarator of `Dim a = F(), b = G()` is its own resume point. After `If c Then Boom()` fails, `Resume` re-runs `Boom()` without re-testing `c`.
* A failing condition resumes at the next resume point in source order, which is usually **inside** the block:

  | Failing expression | `Resume Next` continues at |
  |---|---|
  | `If` / `ElseIf` condition | first statement of that branch |
  | `While` / `Do While` condition (tested at the top) | first statement of the body |
  | `Loop Until` / `Loop While` condition (tested at the bottom) | after the loop |
  | `Select Case` expression | after `End Select` |
  | `Case` expression | first statement of that case |
  | `For` start value, `For Each` collection | the body, once, with a default loop variable; then after the loop |

* `Using`, `SyncLock` and any other statement lowered to a compiler-generated `Try` is a single resume point: an error inside resumes after the block, and the dispose/unlock runs. `On Error` can't appear inside them, in a method with `Try`, or in lambdas, iterators and async methods.
* `Err`:
  * set on every caught error;
  * cleared by every `On Error` statement and every `Resume*` statement;
  * cleared on leaving the method only if it is still in a handler. So after `On Error Resume Next` swallows an error, `Err` stays set, even after the method returns.
* Code that runs into a handler label without an error is ordinary code: an error in it is caught and enters the handler from the top. This includes cleanup code reached by `Resume Cleanup` while the same handler is still active: if the cleanup fails, the handler runs again, resumes to the cleanup again, and can loop forever.

### C#

* `goto` can leave a block, a `try` or a `catch`, but can't enter one. So a method-level catch can never resume in the middle of a loop or `If`.
* Iterators and async methods are compiled to state machines that can re-enter nested blocks, but not after an exception: an exception leaving `MoveNext` finishes the iterator, and `yield` isn't allowed in a `try` that has a `catch`.
* `catch (Exception ex) when (flag)` matches VB's filter, including not unwinding when it's false.
* Local functions can capture locals, but not `ref`/`out` parameters.
* Locals are block scoped and must be definitely assigned. VB locals are method-lifetime and zero-initialised, so declaring them earlier as `= default` is exact.

## Approach

Every idea that catches at method level needs to jump back in, which needs the whole body flattened to a `switch` in a loop. Instead, **catch at the statement that fails**. Then nothing needs to jump inwards:

| VB | C#, inside the failing statement's own `catch` |
|---|---|
| `Resume Next` | leave the catch |
| `Resume` | `goto` a label directly before that `try` (same block) |
| `Resume L` / `GoTo L`, `L` at method level | `goto L` (out of a catch is legal) |
| handler code | a local function called from the catch. An exception in it propagates, like VB's "in a handler". |
| `Exit Sub` / `Return` in the handler | the local function returns "exit", the catch returns |

The method keeps its structure, loops stay loops, and the cost is proportional to how much of the method is covered by a resuming handler.

### Model

A pass over the method body builds a model before any statement is converted, so the statement visitors don't each need to know the rules:

* **Active handler per statement**: none, resume-next, or a handler label. Static when every `On Error` statement is a direct child of the method body and no label/`GoTo` crosses the regions between them. Otherwise *dynamic*: a local records the handler and the catches filter on it.
* **Handler regions**: for each handler label, the statements from the label to where control leaves, and the set of ways it leaves: `Resume Next`, `Resume`, `Resume L`, `GoTo L`, exit, falls off the end.
* **Guards**: the resume points that have an active handler (per the table in "VB" above).
* **Hoisted locals**: locals declared within a guard and used outside it.
* **`Err` is used**: see below.

### Output shapes

Chosen per method, simplest first. `…` is where the `Err` calls go when `Err` is used.

1. **Structured.** One `On Error GoTo H` covering a run of method-level statements, `H` at method level, and the handler never resumes into the covered statements (it exits, falls off the end, or does `Resume L`/`GoTo L` to a method-level label after the covered statements or after the handler).

   ```csharp
   try
   {
       // covered statements
   }
   catch (Exception ex)
   {
       // handler
   }
   // statements from L onwards
   ```
   When the handler leaves to a label after itself (`Resume Done` / `GoTo Done`, with `Done:` following the handler), the statements from the end of the handler's region on come after the `try`, unprotected, and the handler's jump to them is a fall through. Like the code after a resume label, they are only protected (segmented form) when the handler is written for re-entry, which isn't supported for a label after the handler: that keeps the fallback, as does a jump into a nested block of those statements from outside.
   This is the existing conversion, generalised, with locals hoisted (fixes the existing `RemainingScopeIssueCharacterizationAsync` output). When the handler is written for re-entry, the statements after `L` get their own `try` too (see "Secondary errors").

2. **Resume next.** The active handler is resume-next. Each guard becomes

   ```csharp
   try { Statement(); } catch { }
   ```
   * `If` condition: `bool cond = true; try { cond = …; } catch { }` then `if (cond)`. `ElseIf` chains become nested `if`/`else` so each condition has somewhere to go.
   * Loop condition: a local function, so the loop stays a loop: `bool Cond() { try { return …; } catch { return true; } }` and `while (Cond())`. Bottom-tested conditions return whichever value leaves the loop.
   * `Using`, `SyncLock`, `Select Case` and `For`/`For Each`: one guard around the whole statement, with the statements inside guarded individually where VB makes them resume points (not inside `Using`/`SyncLock`). A failing header skips the block.
   * `Case` expressions: a failing one enters that case, and the cases after it aren't tried (spiked: `Case 2` / `Case Boom()` / `Case Else` on 9 runs the body of the `Boom()` case). The `when` condition of each label whose expressions can fail moves into a local function that returns true if evaluating it fails (`bool CaseMatches() { try { return selectValue == Boom(); } catch { return true; } }`), so the cases before it are still tried and the ones after it are not. The pattern variable is replaced by a local holding the `Select` expression. Evaluating every case up front would run side effects VB wouldn't. A failing `Select` expression skips the whole statement, as before.
   * A `Dim` inside a loop whose initializer can fail is declared (`= default`) before the outermost loop, because a VB local keeps its value from the previous iteration when the initializer fails.
   * A statement is only guarded when it can fail (`ThrowAnalysis`): constants, locals and parameters of primitive types, their comparisons, integer `+`, `-` and `*` (and `+=`, `-=`, `*=`) over locals and constants of one type (the output is unchecked, so overflow wraps; `Decimal`, `Double`, `Single`, `/`, `\`, `Mod` and `^` still count as failing), creating a lambda (its body doesn't run), `Exit`, `GoTo` and labels aren't. Every `Dim` gets `= default` so that skipping its assignment still compiles.
   * `With` is guarded like a `Select` whose expression fails. A loop whose condition can fail, or a `Select` with a `Case` expression that can fail, isn't expressible when it reads a `ref`/`out` parameter (a local function can't capture it): the method keeps the fallback.

3. **Handler function.** `On Error GoTo H` where the handler resumes into the covered statements. The handler becomes a local function and each guard calls it:

   ```csharp
   void H() { /* handler */ }
   try { Statement(); } catch { H(); }
   ```
   If the handler leaves in two ways, it returns a `bool` and its name says what `true` means (`Retries`: run the statement again; `Exits`: leave the method):

   ```csharp
   bool FailedRetries() { /* handler */ }
   s3: try { Statement(); } catch { if (FailedRetries()) goto s3; }
   ```
   Labels are only emitted for `Resume`. `Return expr` in the handler of a function is assigned to the return variable (or a `result` local) that the caller returns. A handler with three ways out, `Resume L` or `GoTo L` keeps the fallback, as does a method that reaches the handler label without an error (an error inside the inline run would resume after the call, not after the failing statement).

4. **Dynamic.** The active handler isn't statically known. Same guards, filtered: `catch (Exception) when (resumeNext)`, with `resumeNext` assigned where the `On Error` statements were. Only implemented when the only handlers are resume-next and none; with handler labels (an `int` flag) the fallback stays.

   A method whose only `On Error` statements are `On Error GoTo 0` (no `Resume`) never has a handler active, so there is no shape: each converts to `ProjectData.ClearProjectError()` when `Err` is maintained (VB clears `Err` there, checked by running it), otherwise to nothing, and the statements are converted as usual.

5. **Fallback.** Anything else keeps today's behaviour, statement-by-statement conversion plus `#error`:
   * the handler jumps to a label inside a nested block;
   * `On Error GoTo -1`;
   * a handler that falls through into a label, without a `GoTo` or `Resume` to it, when the covered code also jumps to that label (`H:` handler, `Done:` next, `GoTo Done` in the covered code): the label would be reached from the `try` and from the `catch`;
   * a `Resume Next` handler (`On Error GoTo Failed` ... `Failed: Resume Next`) around a `For Each` (or any loop, `Using`, `SyncLock`, `Select Case`) whose body statements can fail. A common legacy shape, but every body statement needs its own guard inside the guarded statement, and the handler's own error would be caught again by the outer guard where VB lets it propagate. `On Error Resume Next` in the same place is converted (shape 2);
   * a handler that resumes to a label (`Resume Retry`) when the code after the label can fail, with no re-entry guard (see "Secondary errors"), and either nothing before the label can fail or the handler runs `On Error GoTo 0` (a one-shot retry): the first error would come from code the plain form leaves unprotected;
   * a handler local function would need to capture a `ref`/`out` parameter;
   * a local that has to be split into a declaration and an assignment (a guarded `Dim`, or one the handler or later code uses) has a type that can't be written: an anonymous type, or a lambda's anonymous delegate type. `var q = default;` doesn't compile, and declaring it later would put it out of scope;
   * `On Error Resume Next` and `On Error GoTo <label>` in the same method;
   * `On Error GoTo 0` inside a handler (dynamic with a handler label) unless the structured shape applies: it also deactivates the handler for what the handler then resumes to. The structured shape converts it like any `On Error` statement there (a clear of `Err` when `Err` is maintained, otherwise nothing: VB sets `Err.Number` to 0 there, checked by running it), since the `catch` already lets later errors propagate. It keeps the fallback when the handler is also written for re-entry (see "Secondary errors"), because the segmented form would protect code that VB leaves unprotected after the handler resumed to it;
   * a guarded `Dim` in a loop (declared before the loop) that has the same name as another local of the method, e.g. in an earlier `If` block: the hoisted declaration would be in scope of the other one's block (CS0136). The hoisting that exists without `On Error` (`HoistVariablesBeforeLoopWhenNeeded`) has the same limitation and is out of scope;
   * a shape that needs a local function (a handler function, or a loop condition or `Case` expression moved out of its statement) in an instance member of a `Structure`: a local function in a struct can't use `this` (CS1673). Static members and plain guards are fine;
   * (handler function) a handler that only `Resume`s (retry only, so no way out: not implemented); several handlers; a guard inside a guarded block (`For Each` with statements inside): the handler's own error would be caught again by the outer guard, but VB lets it propagate; a failing loop condition or `Case` expression when the handler can leave the method (a local function can't return from the method).

   A flattened `switch`-in-a-loop state machine would cover these, but the output is something nobody would keep.

Alternatives considered:
* *Iterator of thunks*: `IEnumerable<Action> Steps() { …; yield return () => Statement(); … }` driven by `foreach (var step in Steps()) try { step(); } catch { }`. It works (the iterator survives because the exception is thrown by the driver, not inside `MoveNext`) and is one line per statement, but lambdas can't capture `ref`/`out` parameters, `Return`/`Exit`/`GoTo` need extra plumbing, it allocates per statement, and stepping through it is unpleasant.
* *`Guard(() => Statement())` helper* (issue comment 6): same capture limits. Not factored out to start with; the inline `try`/`catch` is the only form.
* *Filter that inspects the stack trace* (issue comment 6): not needed, VB catches errors from callees too.

## Decisions

### `Err` is only maintained when it seems to be used

`ProjectData.SetProjectError(ex)` / `ClearProjectError()` are emitted in a converted method when either holds:

* its containing type (all partial parts) references `Err` (`Microsoft.VisualBasic.Information.Err`);
* the project contains an **`Err` helper**: a method that references `Err` but has no `On Error` statement of its own, so it can only be reading an error that some other method caught (typically a shared `LogError()` called from handlers anywhere).

The helper search is one pass per project, cached: find identifiers named `Err`, bind only those, and check the containing method for `On Error` statements. Only reads count: `Err.Raise` and `Err.Clear` don't depend on an earlier error. A read inside a `Catch` block reads that block's own error, so doesn't make an `Err` helper.

When `Err` is maintained:

* each catch starts with `SetProjectError(ex)`;
* `ClearProjectError()` goes where an `On Error` or `Resume*` statement was, and on leaving a handler by exit. `Return expr` is wrapped in `try { return expr; } finally { ProjectData.ClearProjectError(); }` unless `expr` is a literal or the return variable, since VB evaluates it (possibly reading `Err`) before clearing.

Otherwise catches are bare `catch { }` and need no `Microsoft.VisualBasic` reference.

Known differences: `Err` read through reflection, late binding, or from another project isn't seen. After `GoTo L` out of a handler, VB clears `Err` when the method is left; the C# leaves it set.

### Throwing loop headers skip the loop

VB runs the body once with a default loop variable. C# output skips the loop (one guard around the whole statement). Not worth lowering loops for.

A failing `Step` expression is worse in VB: it runs the body with a default loop variable and then never leaves the loop (checked by running it: the body ran until stopped by a counter). The C# output skips the loop and terminates, so there is nothing to preserve.

### Secondary errors: propagate, unless the handler is written for re-entry

A *secondary error* is one raised in code that runs after the handler has finished but while that same handler is still the active one:

```vb
    On Error GoTo H
    Work()
Cleanup:
    conn.Close()   ' a failure here is a secondary error
    Exit Sub
H:
    Log(Err.Description)
    Resume Cleanup
```

The same applies to handler code reached without an error (no `Exit Sub` before the label).

VB re-enters `H`, which here means log, close, fail, log, close … forever. Looping forever is never what was meant, so by default only the code before the first resume label is in the `try`, and a secondary error propagates to the caller.

The exact form is cheap, because the handler never resumes into the middle of the cleanup: split the covered statements into **segments** at the labels the handler resumes to, give each segment its own `try`, and put the handler after them, at method level, where each `catch` jumps to it:

```csharp
try { Work(); } catch (Exception ex) { ProjectData.SetProjectError(ex); goto H; }
Cleanup:
try { conn.Close(); return; } catch (Exception ex) { ProjectData.SetProjectError(ex); goto H; }
H:
Log(Information.Err().Description);
ProjectData.ClearProjectError();
goto Cleanup;
```

All its ways out are method-level jumps or exits, so it needs no local function and no action value to say how it left (C# has no local enums, and a nested one would clash between partial parts). The segmented form isn't used when the handler leaves by `GoTo L` (the code at `L` would still be "in a handler" in VB, so isn't protected) or is also reached without an error.

Which form is used:

| Situation | Output |
|---|---|
| No secondary error possible: the code after the resume label can't throw (only exits, jumps, assignments of locals/constants), or starts with its own `On Error` statement | plain structured (they're the same thing) |
| Secondary error possible and the handler has a **re-entry guard** | exact, segmented form |
| Secondary error possible, no re-entry guard | plain structured: the secondary error propagates |
| Secondary error possible, no guard, and nothing before the resume label can fail | fallback: the first error would come from the unprotected code and propagate, which VB wouldn't do (a retry loop: `i = 0` / `Retry:` / `Work()` / handler `Resume Retry`) |
| Secondary error possible, no guard, and the handler runs `On Error GoTo 0` before resuming | fallback: VB protects the first pass through the code after the label but not the retry (`Open()` / `Again:` / `Work()`, handler `On Error GoTo 0` then `Resume Again`), which neither the plain nor the segmented form does |

A re-entry guard is a condition in the handler or in the code after the resume label (`If`, `Select Case`, loop condition, `If(…)`) that reads a local, field or property which is assigned in the handler or in the code after the resume label. For example `attempts += 1 : If attempts > 3 Then Exit Sub`, or `If cleaningUp Then Exit Sub` with `cleaningUp = True` in the cleanup, or a retry that counts itself: `Retry: i += 1 : If i < 3 Then Work()`. A condition in the code after the label only counts for code the handler resumes into inside the covered statements: a tail after the handler (`Resume Cleanup` with `Cleanup:` after the handler, `If conn IsNot Nothing Then Close(conn)` / `conn = Nothing`) is plain structured unless a condition in the *handler* reads something assigned in the handler or the tail. Without one, a second pass through the handler does exactly what the first did, so re-entry can't have been relied on to terminate.

Conditions on `Err.Number` alone don't count: they distinguish kinds of first error. No warnings are emitted in any of these cases.

## Open questions

* Formatting: the guards rely on `try { … } catch { }` staying on one line. If the formatter expands them, shape 2 is four lines per statement.

## Where each part lives

All in this directory unless stated. Planned parts are in italics.

| Requirement | Class |
|---|---|
| Entry point, one emitter per shape | `OnErrorMethodBodyConverter`, called from `LambdaConverter.ConvertMethodBodyStatementsAsync` |
| Active handler per statement, dynamic detection, handler regions and exits | `OnErrorModel`, `ActiveHandler`, `HandlerRegion`, `HandlerExit` (analysis only, no C# syntax) |
| Which statements are resume points, and where a failing condition resumes | `ResumePoints`, `ThrowAnalysis` |
| `Err` usage in the type, `Err` helpers in the project (one cached pass per project) | `ErrUsage`, created in `VBToCSProjectContentsConverter`, reached through `CommonConversions.ErrUsage` |
| Choosing a shape | `OnErrorShapeSelector` |
| Structured layout, segments | `StructuredPlanner`, `StructuredPlan` |
| Secondary errors: can the resumed-to code throw, re-entry guard | `ThrowAnalysis`, `ReEntryGuard` |
| `On Error` / `Resume` statements met by the method body visitor | `IOnErrorStatementConverter`, set on `MethodBodyExecutableStatementVisitor.OnErrorStatementConverter`; `StructuredStatementConverter` |
| Shape 1 output, hoisting, `Err` calls | `StructuredShapeEmitter`, `LocalHoisting`, `ErrStatements`, `ClearErrOnReturnRewriter` |
| Shape 2 output: guards for every resume point, `Dim` splitting, `If`/loop condition guards | `ResumeNextShapeEmitter`, `GuardingStatementVisitor` (put in front of the method body visitor with `DecorateStatementVisitor`, so each statement at any depth is guarded), `StatementGuards`, `GuardPolicy`, `DeclarationSplitter`, `DefaultInitializers`, `ResumeNextStatementConverter` |
| Shape 3 output: which ways the handler leaves, what the function returns, what each guard's `catch` does | `HandlerFunctionPlanner`, `HandlerFunctionPlan`, `HandlerFunctionShapeEmitter`, `HandlerFunctionGuardPolicy`, `HandlerFunctionStatementConverter`, `HandlerExitRewriter`, `MissingReturn` |
| Shape 4 output | `ResumeNextShapeEmitter` with `DynamicGuardPolicy` |
| Comments of a handler statement that converts to nothing (an `On Error GoTo 0` with `Err` not maintained, the handler label), kept with the next statement of the same block rather than carried back into the `try` by the line-based trivia mapping | `ElidedStatementTrivia`, used for the handler of the structured shape by `OnErrorConversionContext.ConvertHandlerAsync` |

Tests: conversion tests in `Tests/CSharp/StatementTests/OnErrorStatementTests.cs`, and self-verifying tests in `Tests/TestData/SelfVerifyingTests/VBToCS/OnError*.vb` that log each step and compare the VB and C# logs (the scenarios used to check this design: failing `If`/`ElseIf`/`While`/`Loop Until` conditions, `Select`, `Using`, multiple declarators, `Return`, `On Error GoTo 0`, handler with `Resume Next` inside a loop, `Resume` retry with a counter, fall-through into a handler, handler that throws, cleanup idiom, conditional `On Error`).
