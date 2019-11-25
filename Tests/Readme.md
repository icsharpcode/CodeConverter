# Tests

* Most tests are "characterization" tests, meaning they represent the current output. This does not mean that it's the ideal output.
* If you need to change an existing characterization, that's fine, so long as you're convinced it's no less correct than what was previously there.
* See TestConstants for an automated way to rewrite the expected output en masse.

## Types

* Single file characterization tests (e.g. ExpressionTests.cs)
* Multi file characterization tests (e.g. MultiFileSolutionAndProjectTests.cs)
* Single file self-verifying tests (e.g. EnumTests.vb)
  * These are the only tests where the output is not in the repository.
  * They convert a test method, and ensure it still passes.
  * These can give great confidence that the conversion is correct.
  * It's also possible for some conversion bugs to make the test pass for the wrong reason (in the extreme, imagine that the entire test method body became empty)

## Other guidelines

* In general, the source and target of all test cases should compile.
* To test this, flip the define the "ShowCompilationErrors" compile time constant, or tweak the condition in ProjectConversion that uses it, then run the tests.
* Some older tests still do not compile, these will be gradually fixed over time using the above method.
* When testing behaviour specific to non-compiling code (e.g. due to a missing reference), put the test in a subfolder called MissingSemanticModelInfo



