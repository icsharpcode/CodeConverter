## What to do
1. Raise an issue against the repo to give room for discussion.
2. Clone the repo and write a test case - copy the closest existing test as a starting point (or look for one converting the other way).
3. Check how the code in the opposite conversion works - there may already be a solution to the problem.
4. Implement a solution. If it involves a lot of code changes, discuss on the issue to avoid conflicts and check it's the best approach before putting lots of work in.

## How to get started changing code
At its heart, there is a visitor pattern with a method for each syntax type. If you don't know what a Syntax Tree is, that's definitely worth [looking up](https://github.com/dotnet/roslyn/wiki/Roslyn-Overview). There are lots of tests, set a breakpoint somewhere like `VisitCompilationUnit`, then run them in debug mode. If you step through the code, you'll see how it walks down the syntax tree converting piece by piece. If you want to find the name of the syntax for some specific code, use [Roslyn Quoter](https://roslynquoter.azurewebsites.net/)

## For documentation, prefer:
* Anything process/project related to be visible on GitHub (e.g. these bullet points)
* Anything code-related (i.e. why things are done a certain way, or broad overviews) to be xmldoc in the relevant part of code
* Anything code-behaviour related to be in a test wherever possible

I have a preference for a small amount of good documentation over a large amount of bad documentation.
At the moment there's just a very small amount of quite bad documentation...so my apologies there.

## Implementation advice
* Always try to convert directly between the VB and C# model, avoid converting then post-processing the converted result. This prevents the code getting tangled interdependencies, and means you have the full semantic model available to make an accurate conversion.
* Aim to use symbols rather than syntax wherever possible. Remember, lots of the problems you need to solve have already been solved by the compiler - finding it is the hard part. http://source.roslyn.io helps a bit
* Avoid using the `SyntaxFactory.Parse*` methods in general - it leads to getting mixed up between which language a string is from, and means you don't learn how the syntax trees are formed. You can use https://roslynquoter.azurewebsites.net/ to help find the correct methods to use.
* The code heavily uses SyntaxFactory at the moment. In future I intend to investigate making some use of [`SyntaxGenerator`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.editing.syntaxgenerator?view=roslyn-dotnet).
* The code currently has to care about both a correct conversion, and creating readable output. In future I hope to use more built-in functions to simplify the code (e.g. `ReduceAsync`). That will allow most of the code to focus on generating code that's correct, and then automatically tidy up the result to remove redundant qualification, parentheses etc.

## Resources
* https://en.wikipedia.org/wiki/Comparison_of_C_Sharp_and_Visual_Basic_.NET#Features_of_Visual_Basic_.NET_not_found_in_C#
* Lots of high level introductions exist, e.g. https://github.com/dotnet/roslyn/wiki/Roslyn-Overview Getting deeper information is a lot harder. If you see good resources, PR them to this document!
