# CodeConverter

Converts between VB.NET and C# using Roslyn, via a command line tool (`CodeConv/`), a Visual Studio extension (`Vsix/`) and a web interface (`Web/`).

## Repository
- Solution: `CodeConverter.slnx`. Mostly targets .NET Core.
- `CodeConverter/`: the conversion logic. For VB -> C#, the main visitors have a method per syntax kind:
  - `CSharp/DeclarationNodeVisitor.cs`
  - `CSharp/MethodBodyExecutableStatementVisitor.cs`
  - `CSharp/ExpressionNodeVisitor.cs`
  - Surrounding visitors keep cross-cutting concerns (e.g. comments and trivia) out of the way.
- `Tests/`: an extensive characterization test suite (see Tests/Readme.md).
- `.github/workflows/dotnet.yml`: CI that builds and tests on Windows and Linux, runs code analysis and reports coverage. It's the reference for what the build and tests need.
- `.github/CONTRIBUTING.md`: how to decide what the output should be, and how to get started in the code.

# Default workflow for any bug fix or feature

When asked to "fix bug X" or "implement Y", follow these phases unless told otherwise. Scale each phase to the change: a one-line fix needs a test and a commit, not a design doc. Never skip the phases that protect correctness (tests, review) because a change looks small.

## 1. Understand before changing
- Read the issue, any linked issues, and the code that handles the construct today. Find the root cause, not just where the symptom shows. Other issues in the area might help inform the direction the code should be going.
- Generalise a fix rather than hardcoding details of the particular case reported.
- Establish the actual semantics from primary sources, never from memory:
  - the Roslyn source (the VB/C# binders and lowering): https://github.com/dotnet/roslyn
  - the language docs on learn.microsoft.com
  - running both languages: compile a VB snippet, inspect `IOperation`/symbols, and run it.
- Follow CONTRIBUTING.md's output priorities: same runtime behaviour, then same public API, then idiomatic output, then easy to refactor further, then resembling the input. Prefer progressive fallback (the simplest correct form where it applies, uglier forms only where the input needs them) over output that is always correct but ugly.
- Watch for cross-cutting settings (Option Compare/Strict/Infer, nullable logic, expression trees vs delegates) that change semantics.

## 2. Design (non-trivial changes)
- If the existing approach, or next step is whack-a-mole, step back. Write a short design note covering the rules and constraints of both languages, how they interact, and the chosen approach.
- If the VB and C# shapes differ, model the situation in code (an intermediate representation or a separate pass) rather than threading special cases through the recursive visitors.
- Keep simple paths separate from complex fallbacks, not a tangle of `if`s. Give classes clear single responsibilities; use partial classes only when a split would be artificial.
- If the design needs a decision from the user (a behaviour change, or a trade-off between correctness and readability), present the options with context and a recommendation. Don't guess.

## 3. Implement with strict TDD and atomic commits
- Red, green, refactor. Write the failing test first, from the issue's exact repro, and confirm it fails for the right reason.
- Take inspiration from the existing tests and the shape of the surrounding code.
- Use both kinds of test where behaviour is at stake (see Tests/Readme.md):
  - conversion tests (`TestConversionVisualBasicToCSharpAsync`) pin the output. The harness reports compile errors of the syntax tree, but doesn't reparse the text.
  - self-verifying tests (`Tests/TestData/SelfVerifyingTests/VBToCS/*.vb`) run the VB and the converted C# and compare results. They also catch text-only compile errors. For evaluation order and side effects, log the calls and assert on the log.
- Test the interesting edge cases beyond the issue as well: nulls, `Nothing`, side effects, shadowing, nesting.
- Commits are atomic and each one builds. Do refactors in their own commits before the behaviour change ("pure refactor first, then new code"). Don't mix them.
- Keep refactoring to what the task needs. Once green, make further refactors that clearly improve quality, as separate commits.
- Don't change existing test expectations unless the new output is verified better or correct. Explain each change in the commit or PR.

## 4. Documentation pushed down to the code
- The code says *what*: use names, and extract methods or classes, so the code reads with only rare essential comments.
- XML doc comments say *why*: the VB/C# rule or constraint behind a decision, with a source where useful. Don't repeat what the code does.
- When an area needs an overview (several classes working together on a larger concept), add or update a terse README.md in that directory. Map each requirement to where it's implemented and to its source information; leave out history and old versions. Example: `CodeConverter/CSharp/Queries/README.md`.

## 5. Verify, then review blind until it converges
- Run the full suite and compare it against the baseline of known environment failures. Report any difference.
- Measure test adequacy over the changed code:
  - Coverage, always: `dotnet test ... --collect:"XPlat Code Coverage"`. List the changed lines that are uncovered or have uncovered branches.
  - Mutation testing (Stryker.NET) is optional, because it is slow (about an hour for a few hundred mutants). Use it for complex logic. Limit `mutate` to the changed files, or line ranges such as `File.cs{30..67}`, and the test filter to the relevant tests. Prefer `--since` or a stored baseline (below).
  - Self-verifying tests convert during test discovery, so Stryker doesn't count their kills. Check a survivor against the `.vb` tests before treating it as a gap.
- Then run blind review rounds:
  - Each round is a fresh subagent given the diff and the coverage/mutation data, but not your conclusions. List the design decisions already made so they aren't re-reported.
  - Reviewers must verify each finding by running it, either as a temporary conversion test or a temporary self-verifying test, and clean up afterwards.
  - They classify each finding as bug, ugly output, quality or nitpick. They also classify survivors and gaps as real bug, missing test, dead code or harmless.
  - Fix each real finding test-first, with atomic commits, then start the next round with fresh data.
  - Stop when a round finds only nitpicks, or when its fixes start reversing earlier fixes (a flip-flop). Report what was declined, and why.

## 6. Deliver
- When writing a draft PR descript, use "Fixes #N" lines for each issue resolved, a summary mapped to the issues, what needs the reviewer's attention or decision, test counts, and how the suite compares with the baseline.

# Tooling notes
- Build and run a subset: `dotnet build Tests/Tests.csproj -f net10.0`, then `dotnet test Tests/Tests.csproj -f net10.0 --no-build --filter "FullyQualifiedName~Linq"`.
- Mutation testing with Stryker.NET (`dotnet-stryker`):
  - Run it from `Tests/` in test-project mode, because the solution includes a web .esproj it can't load.
  - Example config: `"mutate": ["**/CSharp/Queries/*.cs"]`, `"test-case-filter": "FullyQualifiedName~Linq|FullyQualifiedName~CSharp.SelfVerifyingTests"`, `"coverage-analysis": "perTest"`.
  - Planned: a CI workflow stores a baseline mutation report as an artifact or cache, and local runs use `--with-baseline` (disk provider) or `--since:master`. Then each iteration only re-tests mutants in changed files.
