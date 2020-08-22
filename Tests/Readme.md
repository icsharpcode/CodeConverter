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
  * These are the only tests where the output is not in the repository.
  * They convert a test method, and ensure it still passes.
  * These can give great confidence that the conversion is correct.
  * It's also possible for some conversion bugs to make the test pass for the wrong reason (in the extreme, imagine that the entire test method body became empty)

## Other guidelines

* In general, the source and target of all test cases should compile.
  * Exception: Things in the MissingSemanticModelInfo folder which specifically test common cases of incomplete input.
* Some older tests have compile errors in the input or output. These will be gradually fixed over time.

