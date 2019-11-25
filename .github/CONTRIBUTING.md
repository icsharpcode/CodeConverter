## Before making changes
1. Raise an issue against the repo to give room for discussion.
2. Clone the repo and write a test case - copy the closest existing test as a starting point (or look for one converting the other way).
3. Check how the code in the opposite conversion works - there may already be a solution to the problem.
4. Implement a solution. If it involves a lot of code changes, discuss on the issue to avoid conflicts and check it's the best approach before putting lots of work in.

## Deciding what the output should be
Sometimes it's hard to figure out the "correct" conversion for some code. Our priorities are to generate
1. Code that behaves the same at runtime (excluding use of reflection) or a compile error where we can't do that
2. Code that has the same public API - avoiding binary-level and source-level breaks (this could actually be a library option)
3. Idiomatic "good" code in the target language
4. Code easy to refactor to even more idiomatic "good" code (not done automatically because it affects one of the above)
5. Code that resembles the input

Example:
VB sets a function return type to be "Object" if not specified. 

If that function isn't on a public API, we could choose a more specific type if we're sure it's valid.

If that function is on a public API, we could make internal callers use a different method with a specific type, and have the public method just call through to that method. We may want to flag such cases with an attribute, comment or #warning pragma encouraging refactoring to remove the "Object" overload if it's not required by public API consumers.

## Test types
See [Tests/Readme.md](https://github.com/icsharpcode/CodeConverter/blob/master/Tests/Readme.md)

## How to get started changing code
At its heart, there is a visitor pattern with a method for each syntax type. If you don't know what a Syntax Tree is, that's definitely worth [looking up](https://github.com/dotnet/roslyn/wiki/Roslyn-Overview). There are lots of tests, set a breakpoint somewhere like `VisitCompilationUnit`, then run them in debug mode. If you step through the code, you'll see how it walks down the syntax tree converting piece by piece. If you want to find the name of the syntax for some specific code, use [Roslyn Quoter](https://roslynquoter.azurewebsites.net/)

## For documentation, prefer:
* Anything process/project related to be visible on GitHub (e.g. these bullet points)
* Anything code-related (i.e. why things are done a certain way, or broad overviews) to be xmldoc in the relevant part of code
* Anything code-behaviour related to be in a test wherever possible

I have a preference for a small amount of good documentation over a large amount of bad documentation.
At the moment there's just a very small amount of first draft documentation. Consider this a request for feedback on what's most confusing.

## Implementation advice
* Always try to convert directly between the VB and C# model, avoid converting then post-processing the converted result. This prevents the code getting tangled interdependencies, and means you have the full semantic model available to make an accurate conversion.
* Aim to use symbols rather than syntax wherever possible. Remember, lots of the problems you need to solve have already been solved by the compiler - finding it is the hard part. http://source.roslyn.io helps a bit
* Avoid using the `SyntaxFactory.Parse*` methods in general - it leads to getting mixed up between which language a string is from, and means you don't learn how the syntax trees are formed. You can use https://roslynquoter.azurewebsites.net/ to help find the correct methods to use.

## Moving away from legacy patterns in the code
* Just do: The code currently cares about both a correct conversion, **and** creating readable output in many places. But since we've introduced [`ReduceAsync`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.simplification.simplifier?view=roslyn-dotnet), it's generally best to fully qualify and parenthesize things and allow it to be auto-simplified later.
* Just do: The code currently makes heavy use of reading the syntax tree. In the vast majority of cases, we should move to using ISymbol and IOperation available from the semantic model. We should also be using `GetCsSymbolOrNull` in some cases where there's a possibility of the CSharp model having different information (though bear in mind it will often be null when there's even a minor compilation issue in the source, so we need a fallback).
* Investigate: The code heavily uses SyntaxFactory at the moment. In future I intend to make further use of [`SyntaxGenerator`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.editing.syntaxgenerator?view=roslyn-dotnet).

## Resources
* Lots of high level Roslyn introductions exist, e.g. https://github.com/dotnet/roslyn/wiki/Roslyn-Overview Getting deeper information is a lot harder. If you see good resources, PR them to this document!
* Understanding VB/C# differences:
 * https://en.wikipedia.org/wiki/Comparison_of_C_Sharp_and_Visual_Basic_.NET#Features_of_Visual_Basic_.NET_not_found_in_C#
 * https://anthonydgreen.net/2019/02/12/exhausting-list-of-differences-between-vb-net-c/
 * Roslyn source for [CSharp binder](http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Binder/Binder_Expressions.cs,365) vs [VB binder](http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.VisualBasic/Binding/Binder_Expressions.vb,43)

## Codebase details
* All parallelism is controlled by Env.MaxDop. When a debugger is attached to a debug build, it sets parallelism to 1. If you're seeing a transient issue when the debugger isn't attached but can't reproduce the issue with the debugger, set this to a large number instead.