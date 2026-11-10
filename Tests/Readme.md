# Tests

* Most tests are "characterization" tests, meaning they represent the current output under [these guidelines](https://github.com/icsharpcode/CodeConverter/blob/master/.github/CONTRIBUTING.md#deciding-what-the-output-should-be). This does not mean that it's the ideal output.
* If you need to change an existing characterization, that's fine, so long as you're convinced it's no less correct than what was previously there.
* See TestConstants for an automated way to rewrite the expected output en masse 
 * Run hooks/enable-hooks.cmd to enable a pre-commit hook which stops you committing the flag when true

## Types

* Single file characterization tests (e.g. ExpressionTests.cs)
  * When there are several test files that could accomodate a new test, pick the most specifically named one that covers what you're aiming to test.
* Multi file characterization tests (e.g. MultiFileSolutionAndProjectTests.cs)
* Single file self-verifying tests (e.g. EnumTests.vb)
  * VB-to-C# fixtures in `TestData/SelfVerifyingTests/VBToCS/*.vb` are discovered automatically by [the shared theory](CSharp/SelfVerifyingTests.cs); no extra C# runner is needed. Include `SelfVerifyingTests.VisualBasicToCSharpAsync` when filtering tests to run these fixtures.
  * These are the only tests where the output is not in the repository.
  * They convert a test method, and ensure it still passes.
  * These can give great confidence that the conversion is correct.
  * It's also possible for some conversion bugs to make the test pass for the wrong reason (in the extreme, imagine that the entire test method body became empty)

## Cross-platform (Windows and Linux)

All tests must pass on both. Handle differences in the most general place, not per test:

* **Git** (`.gitattributes`): `* text eol=crlf`, so checked-out source and test data have CRLF on every OS. Raw newlines in verbatim string literals in tests are therefore always `\r\n`. Don't change this.
* **Production code**: output uses the current OS's newline (`Environment.NewLine`). Never hardcode `\` or `/` when handling paths; accept both separators when reading project/solution/resx text (see `PathConverter.EscapeSeparatorAgnostic`), since those files are usually written on Windows.
* **Tests**: write expectations naturally, don't think about line endings or OS.
  * Conversion tests compare via `OurAssert.EqualIgnoringNewlines`/`Utils.HomogenizeEol`. Use `Utils.HomogenizeEol` on both sides for any other string comparison.
  * Use `Utils.RootedPath(...)` for fake absolute paths instead of `C:\...`.
  * Only a test whose point is the exact line ending should assert on one (e.g. `NewlineTests`).
  * Self-verifying `.vb` tests run on the current OS: avoid depending on `Environment.NewLine` (e.g. `XDocument.ToString()` output) without normalising.

## Other guidelines

* In general, the source and target of all test cases should compile.
  * Exception: Things in the MissingSemanticModelInfo folder which specifically test common cases of incomplete input.
* Some older tests have compile errors in the input or output. These will be gradually fixed over time.

