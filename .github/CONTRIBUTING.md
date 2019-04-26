## What to do
1. Raise an issue against the repo to give room for discussion.
2. Clone the repo and write a test case - copy the closest existing test as a starting point (or look for one converting the other way).
3. Check how the code in the opposite conversion works - there may already be a solution to the problem.
4. Implement a solution. If it involves a lot of code changes, discuss on the issue to avoid conflicts and check it's the best approach before putting lots of work in.

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

## Resources
* Lots of high level introductions exist, e.g. https://github.com/dotnet/roslyn/wiki/Roslyn-Overview Getting deeper information is a lot harder. If you see good resources, PR them to this document!
