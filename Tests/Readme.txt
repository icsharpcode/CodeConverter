In general, the source and target of all test cases should compile.
* To test this, flip the define the "ShowCompilationErrors" compile time constant, or tweak the condition in ProjectConversion that uses it, then run the tests.
* Some older tests still do not compile, these will be gradually fixed over time using the above method.
* When testing behaviour specific to non-compiling code (e.g. due to a missing reference), put the test in a subfolder called MissingSemanticModelInfo