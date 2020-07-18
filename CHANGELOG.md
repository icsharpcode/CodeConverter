# Changelog
All notable changes to the code converter will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [Unreleased]


### Vsix


### VB -> C#
* Convert parameterized properties with optional parameters [#597](https://github.com/icsharpcode/CodeConverter/issues/597)

### C# -> VB


## [8.1.6] - 2020-07-12


### Vsix
* Fix file extension and location of single converted file [#589](https://github.com/icsharpcode/CodeConverter/issues/589)

### VB -> C#
* Correct logic for conversion "objectWithOverloadedEquals Is Nothing" [#591](https://github.com/icsharpcode/CodeConverter/issues/591)
* Coercing enum to a string now correctly uses its numeric value [#590](https://github.com/icsharpcode/CodeConverter/issues/590)
* Correct conversion for equality of overloaded types [#594](https://github.com/icsharpcode/CodeConverter/issues/594)

### C# -> VB


## [8.1.5] - 2020-07-04


### Vsix
* Workaround Visual Studio 16.7+ that was causing VB->CS conversion to fail [#586](https://github.com/icsharpcode/CodeConverter/issues/586)

### VB -> C#
* Handle Option Compare Text case insensitive comparisons in switch statements [#579](https://github.com/icsharpcode/CodeConverter/issues/579)
* Fix compilation error when switching with enum cases [#549](https://github.com/icsharpcode/CodeConverter/issues/549)
* Improve numeric casts [#580](https://github.com/icsharpcode/CodeConverter/issues/580)
* Add ref to conversion of RaiseEvent where needed [#584](https://github.com/icsharpcode/CodeConverter/issues/584)
* Rename clashing type memvers [#420](https://github.com/icsharpcode/CodeConverter/issues/420)
* Fix conversion for string implicitly converted to enum [#476](https://github.com/icsharpcode/CodeConverter/issues/476)

### C# -> VB
* Rename explicit method implementations where needed [#492](https://github.com/icsharpcode/CodeConverter/issues/492)
* Include type information in conversion of default(someType) [#486](https://github.com/icsharpcode/CodeConverter/issues/486)

## [8.1.4] - 2020-06-26


### Vsix
* Fixed UnauthorizedAccessException when converting single file/snippet

### VB -> C#
* When converting ReDim Preserve to Array.Resize, "ref" is now added
* Create delegating method for renamed implementations [#443](https://github.com/icsharpcode/CodeConverter/issues/443), [#444](https://github.com/icsharpcode/CodeConverter/issues/444)

### C# -> VB


## [8.1.3] - 2020-05-23

### Known issues
* Need to replace "Array.Resize(" with "Array.Resize(ref "

### Vsix


### VB -> C#
* Improve post-conversion experience for designer files - [#569](https://github.com/icsharpcode/CodeConverter/issues/569)
* Optimize away some redundant casts and conversions with strings/chars - [#388](https://github.com/icsharpcode/CodeConverter/issues/388)
* Improve performance of single file conversion - [#546](https://github.com/icsharpcode/CodeConverter/issues/546)
* Add AsEnumerable where needed in linq "in" clause - [#544](https://github.com/icsharpcode/CodeConverter/issues/544)
* Remove redundant empty string coalesce in string comparison - [#500](https://github.com/icsharpcode/CodeConverter/issues/500)
* Convert VB comparison operators - [#396](https://github.com/icsharpcode/CodeConverter/issues/396)
* Convert Redim Preserve of 1D array to Array.Resize - [#501](https://github.com/icsharpcode/CodeConverter/issues/501)
* Use C#7.3 compatible null check

### C# -> VB


## [8.1.2] - 2020-05-03


### Vsix


### VB -> C#

* Improve multi-declaration field conversion for arrays - [#559](https://github.com/icsharpcode/CodeConverter/issues/559)
* Add parentheses around ternary statement - [#565](https://github.com/icsharpcode/CodeConverter/issues/565)
* When converting ForEach loop, avoid duplicate variable compilation issue [#558](https://github.com/icsharpcode/CodeConverter/issues/558)
* Improvements to for loop with missing semantic info - [#482](https://github.com/icsharpcode/CodeConverter/issues/482)
* Fix logic issue when converting property passed byref - [#324](https://github.com/icsharpcode/CodeConverter/issues/324)
* Fix logic issue when converting expression passed in byref within conditional expression - [#310](https://github.com/icsharpcode/CodeConverter/issues/310)
* Added constructors now only added to the relevant type - not other types in the same file
* Converted non-static field initializers moved to constructor - [#281](https://github.com/icsharpcode/CodeConverter/issues/281)
* Convert assignments using "Mid" built-in function
* Improve conversion of array initializer types
* Improve detection of compile-time constant case labels

### C# -> VB


## [8.1.1] - 2020-04-21


### Vsix

* Fixes conversion in VS2017

### VB -> C#


### C# -> VB


## [8.1.0] - 2020-04-19


### Vsix


### VB -> C#

* Convert files with legacy vb6 file extensions (e.g. .cls, .frm)
* Fix for converting For...Next...Step loops with a variable step that's sometimes negative [#453](https://github.com/icsharpcode/CodeConverter/issues/453)
* Fix for abstract readonly/writeonly property conversion including a private accessor
* Generated parameterless constructors are now public by default
* Multiple classes in the same file no longer affect each other's constructors
* Cast expressions are now parenthesized when converted
* Fix nullref fatal error dialog for delegates with omitted argument types [#560](https://github.com/icsharpcode/CodeConverter/issues/560)

### C# -> VB


## [8.0.4] - 2020-03-31


### Vsix


### VB -> C#

* Fixes error thrown when converting single file from VB project with resx files

### C# -> VB


## [8.0.3] - 2020-03-30

Known issue: Converting single file from VB project with resx files throws an error. To workaround, use a different version, or convert the whole project at once.

### Vsix


### VB -> C#

* All resx files now moved to project root [#551](https://github.com/icsharpcode/CodeConverter/issues/551)
* Register event handlers for DesignerGenerated [#550](https://github.com/icsharpcode/CodeConverter/issues/550)
* Improve qualification with arguments of unknown type [#481](https://github.com/icsharpcode/CodeConverter/issues/481)
* Omit conversion in string concatenation where possible [#508](https://github.com/icsharpcode/CodeConverter/issues/508)
* Use ToString for numeric types rather than Conversions.ToString
* Convert optional ref parameters - fixes #91
* Always convert Call statement to method call [#445](https://github.com/icsharpcode/CodeConverter/issues/445)
* Avoid compilation errors when converting const Dates [#213](https://github.com/icsharpcode/CodeConverter/issues/213)
* Evaluate simple compile time conversions within const declarations

### C# -> VB


## [8.0.2] - 2020-03-19


### Vsix


### VB -> C#

* Avoid extra newlines in doc comments [#334](https://github.com/icsharpcode/CodeConverter/pull/334)
* Avoid duplicate generated constructors [#543](https://github.com/icsharpcode/CodeConverter/issues/543)
* Comparisons of value/generic types to Nothing now convert to the correct corresponding C# 8 operator
* Resources are now correctly namespaced after conversion [#540](https://github.com/icsharpcode/CodeConverter/issues/540)
* LangVersion is now set to "Latest" in csproj [e87fef11](https://github.com/icsharpcode/CodeConverter/commit/e87fef11338b8d136b9f0e2d97fa3e7f6b7d86a6)
* Improve Winforms designer event experience after conversion [#547](https://github.com/icsharpcode/CodeConverter/issues/547)
* Don't output solution conversion for in-memory solution file
* Empty files are no longer skipped [#423](https://github.com/icsharpcode/CodeConverter/issues/423)
* Specify type suffix for decimal and float literals containing decimal point [#548](https://github.com/icsharpcode/CodeConverter/issues/548)


### C# -> VB


## [8.0.1] - 2020-03-13


### Vsix

* Fixes conversion in VS2017

### VB -> C#


### C# -> VB


## [8.0.0] - 2020-03-11

* Known issue: Breaks VS2017 support - please use [7.9.0](https://github.com/icsharpcode/CodeConverter/releases/tag/7.9.0) until the next version is released
* Improve performance and feedback for large projects containing large files

### API

* IEnumerable<Task<ConversionResult>> becomes IAsyncEnumerable<ConversionResult>
* Upgraded target framework from netstandard 1.3 to netstandard 2.0
* Introduced cancellation token

### Vsix


### VB -> C#

* Convert "Handles" in some previously missed cases [#530](https://github.com/icsharpcode/CodeConverter/pull/530)
* Wrap event handlers in lambda where needed [#474](https://github.com/icsharpcode/CodeConverter/pull/474)
* Streamline trailing else if [810de96](https://github.com/icsharpcode/CodeConverter/commit/810de9677e8f8406c512e292297540e35d6c51d9)
* Use less error-prone, more performant null/default comparisons [4de1978](https://github.com/icsharpcode/CodeConverter/commit/4de19782ef43d8404f6065516d1380a2e00eafdc)
* Convert "{}" to "Array.Empty<T>()" [#495](https://github.com/icsharpcode/CodeConverter/issues/495)
* Convert inferred anonymous member names without duplicating name [#480](https://github.com/icsharpcode/CodeConverter/issues/480)
* Convert "!" operator to element access [#479](https://github.com/icsharpcode/CodeConverter/issues/479)
* Fix async method default return statements [#478](https://github.com/icsharpcode/CodeConverter/issues/478)
* Convert multi-dimensional array initializers correctly [#539](https://github.com/icsharpcode/CodeConverter/pull/539)

### C# -> VB

* Convert PrefixUnaryExpression (Not, minus, etc.) [#533](https://github.com/icsharpcode/CodeConverter/pull/533)
* Rename more members with case conflicts (e.g. interface members) [#533](https://github.com/icsharpcode/CodeConverter/pull/533)
* String equality conversion now calls "Equals" to match the C# logic [#533](https://github.com/icsharpcode/CodeConverter/pull/533)
* Deduplicate imports [#533](https://github.com/icsharpcode/CodeConverter/pull/533)
* Converts "default" keyword to "Nothing" keyword [#428](https://github.com/icsharpcode/CodeConverter/issues/428)

## [7.9.0] - 2020-02-27


### Vsix

* Exclude project file from conversion result if it hasn't changed
* Further efforts to stop the roslyn library crashing Visual Studio
* Conversion tasks are now cancellable

### VB -> C#

* More consistently add constructor for DesignerGenerated attribute

### C# -> VB

* Avoid incorrectly renaming symbols
* Prevent "SyntaxTree is not part of the compilation" error [#527](https://github.com/icsharpcode/CodeConverter/issues/527)
* Avoid incorrectly renaming symbols [#524](https://github.com/icsharpcode/CodeConverter/issues/524)

## [7.8.0] - 2020-02-15

### Vsix

* Stop Roslyn from silently crashing Visual Studio during conversion [#521](https://github.com/icsharpcode/CodeConverter/issues/521)

### VB -> C#
* Performance improvement on big solutions

### C# -> VB

* Partial class/method improvements
* Avoid ambiguity in some generated method calls

## [7.7.0] - 2020-02-11


### Vsix


### VB -> C#

* Cast foreach collection to IEnumerable if needed or unknown
* Fix ordering bug converting redim bounds without preserve for 2d arrays
* Exit Property should become return [#497](https://github.com/icsharpcode/CodeConverter/issues/497)
* First effort converting some Xml Member Access
* Avoid adding new keyword when not allowed/required [#504](https://github.com/icsharpcode/CodeConverter/issues/504)
* Avoid evaluating Select Case expression multiple times in some cases where it may be non-deterministic or have side effects [#323](https://github.com/icsharpcode/CodeConverter/issues/323)
* Avoid repeated redundant break statement caused by explicit Exit Select [#433](https://github.com/icsharpcode/CodeConverter/issues/433)
* Convert return expression to match return type [#496](https://github.com/icsharpcode/CodeConverter/issues/496)
* Fix conversion for hex values ending in "C" [#483](https://github.com/icsharpcode/CodeConverter/issues/483)
* Converted multi line if blocks now always contain braces [#466](https://github.com/icsharpcode/CodeConverter/issues/466)
* Add conversions to allow arithmetic on enums
* Add omitted argument lists on conditional expressions
* Winforms initialization improvements
* Convert VbMyResourcesResXFileCodeGenerator resource generator type in project file
* Improved comment conversion [#518](https://github.com/icsharpcode/CodeConverter/pull/518)

### C# -> VB

* Rename local symbols differing only in case [#80](https://github.com/icsharpcode/CodeConverter/issues/80)
* Remove erroneous event modifier from delegates
* Add Implements clause for event fields
* Add Overloads modifier where needed in VB
* Add ReadOnly/WriteOnly modifier for interface properties that need it
* Fix bug which led to nested modules
* Fix accessibility of converted public static classes
* Fix overqualifying with "Global" in some cases
* Fix parenthesized TryCast which caused compile error
* Fix erroneous rename of value argument in events' accessors and properties' setter
* Add call AscW method to convert Char to numeric types
* Remove erroneous backing fields' generation in root classes
* Add TryCast expression for convering to generic types
* Comments are now converted
* Some preprocessor directives are partially converted [#517](https://github.com/icsharpcode/CodeConverter/pull/517)

## [7.6.0] - 2020-01-11


### Vsix

* Fix Visual Studio crash when converting some structures

### VB -> C#


### C# -> VB


## [7.5.0] - 2020-01-11

* Improve ambiguous name resolution

### Vsix


### VB -> C#
* Simplify compound assignment conversion
* Avoid some case conflicts

### C# -> VB

* Remove extra parentheses around CType expression
* Convert var declaration patterns with binary operators in switch statements (part of #222)
* Keep empty argument lists

## [7.4.0] - 2019-12-17

* Several C# API tweaks wrapping conversion options into a type

### Vsix

* Fixed error caused when converting with "Copy to clipboard" option enabled

### VB -> C#
* Add modifier for nested types
* Remove body for converted extern methods
* Convert Call with missing argument list and semantic information

### C# -> VB
* Convert "is" expression ([#427](https://github.com/icsharpcode/CodeConverter/pull/427))
* Convert "*=" operator

## [7.3.0] - 2019-11-25

* Fixes for nullrefs

### Vsix
* Load extension only when menu item clicked (multi-project conversion menu not present until project loaded)

### VB -> C#
* Convert implicit object->string cast correctly ([#365](https://github.com/icsharpcode/CodeConverter/pull/365))
* Convert trivia (e.g. comments) at start of file ([#333](https://github.com/icsharpcode/CodeConverter/pull/333))
* Improvements to redim conversion ([#403](https://github.com/icsharpcode/CodeConverter/pull/403), [#393](https://github.com/icsharpcode/CodeConverter/pull/393))
* Convert array of arrays initializer ([#364](https://github.com/icsharpcode/CodeConverter/pull/364))
* Improvements to implicit enum -> int conversion ([#361](https://github.com/icsharpcode/CodeConverter/pull/361))
* Convert expressions in constants ([#329](https://github.com/icsharpcode/CodeConverter/pull/329))
* Convert implicit `ElementAtOrDefault` ([#362](https://github.com/icsharpcode/CodeConverter/pull/362))
* Convert types in ternary expressions ([#363](https://github.com/icsharpcode/CodeConverter/pull/363))
* Support for converting dot net standard VB projects ([#398](https://github.com/icsharpcode/CodeConverter/pull/398))
* Avoid compilation error for duplicate cases ([#374](https://github.com/icsharpcode/CodeConverter/pull/374))
* Correctly handle type promoted module symbols ([#375](https://github.com/icsharpcode/CodeConverter/pull/375))
* Prefer renamed imports for name resolution ([#401](https://github.com/icsharpcode/CodeConverter/pull/401))
* Correctly convert ambiguous names ([#332](https://github.com/icsharpcode/CodeConverter/pull/332))
* Ensure correct visibility for constructors ([#422](https://github.com/icsharpcode/CodeConverter/pull/422))
* Ensure casing is correct for namespaces ([#421](https://github.com/icsharpcode/CodeConverter/pull/421))
* Convert CType from a non numeric type to an enum
* Convert Exit Function
* Convert object initializers requiring type casts
* Convert async keyword on lambdas
* Convert nullable if statement conditions

### C# -> VB
* Convert property accessors with visiblity modifiers ([#92](https://github.com/icsharpcode/CodeConverter/pull/92))
* For loop with decrement (i--) results in missing 'Step -1' ([#411](https://github.com/icsharpcode/CodeConverter/pull/411))
* Improve escaping for variables of predefined types
* Add Implements keyword for explicitly implemented members
* Property/indexer conversion improvements
* Convert private default members
* Convert property accessors with visiblity modifiers (#92)
* For loop with decrement (i--) results in missing 'Step -1' (#411)
* Improve custom event conversion ([#442](https://github.com/icsharpcode/CodeConverter/pull/442))

## [7.2.0] - 2019-10-13
* Parallelize multi-file conversion
* Make snippet conversion (ConvertText method) make classes partial by default since context isn't known ([#379](https://github.com/icsharpcode/CodeConverter/pull/379))
* Web converter requires .NET Core 3.0
* Visual Studio built-in simplification applied post-conversion ([#386](https://github.com/icsharpcode/CodeConverter/pull/386))

### Vsix
* Improve UI responsiveness and output window details while converting

### VB -> C#
* Implicitly typed local multi-variable declarations type converted correctly ([#373](https://github.com/icsharpcode/CodeConverter/pull/373))
* "My" namespace - first attempt at conversion ([#169](https://github.com/icsharpcode/CodeConverter/pull/169))

### C# -> VB
* Converts extern functions correctly ([#352](https://github.com/icsharpcode/CodeConverter/pull/352))
* Converts invoke on non-events correctly ([#377](https://github.com/icsharpcode/CodeConverter/pull/377))

## [7.1.0] - 2019-09-12

No longer restricts converted files to solution directory

### Vsix
* Improve UI feedback during conversion

### VB -> C#
* Improve conversion for inline functions
* Use visual basic "Conversions" functions to match VB logic
* Simplify output code by shortening some names
* Improve out parameter conversion
* Improve iterator conversion
* Improve with block conversion for value types
* Improve for loop conversion initialization/bounds
* No longer duplicates containing namespaces
* Improve some enum handling
* Avoid VB type appearing within `default(...)` expression

### C# -> VB
* Improve conversion of collection initializers

### API

`ProjectConversion` methods now require an IProgress parameter, deprecated overloads without it:
* `ProjectConversion.ConvertProject`
* `ProjectConversion.ConvertProjectContents`
* `ProjectConversion.ConvertProjectFile`

Deprecated overload of `ProjectConversion.ConvertSingle` in favour of one requiring a `Document`


Please instead use `ConvertSingle(Document document, TextSpan selected, ILanguageConversion languageConversion)`
See the implementation of the deprecated method for help in migrating.

## [7.0.0] - 2019-08-01
* Compatible with Visual Studio ~15.5+

### VB -> C#
* Improve conversion for WithEvents/Handles
* Improve detection of enum related casts
* Convert parameterized properties
* Convert plain XML literals

### C# -> VB
* Convert more binary operators

## [6.9.0] - 2019-06-09

### VB -> C#
* String comparison conversion now often avoids referencing VB library in output (when TextCompare is set to Binary)
* Convert WithEvents/Handles correctly for partial classes
* Convert Like operator
* Convert VB indexer to ElementAtOrDefault to make behaviour consistent
* Improve accuracy of choosing square brackets or parentheses
* Avoid nullref when converting for loop

### C# -> VB
* Enable OptionInfer on converted projects
* Convert global namespace correctly

## [6.8.0] - 2019-05-13

### VB -> C#
* Assignment return now converted
* Enum implicit casts now converted
* Access to shared variables through instance now converted
* MyClass references now converted
* Variables explicitly initialised to their default (as is implicit in VB)
* Adds project reference to Microsoft.VisualBasic
  * Uses Operators.CompareString for string equality comparison to match VB logic
  * Uses DateAndTime for built-in date functions
  * CDate() now converted to "Conversions.ToDate"
* Improvements to parenthesization
* Select Case with non-constant strings now converted correctly
* Interface readonly properties now converted correctly

## [6.7.0] - 2019-04-09

* Downgrade Roslyn requirement in attempt to work with VS2017 15.3+

### VB -> C#
* Ensure "new()" generic constraint is last
* Do not convert MyBase.Finalize, it's implicit
* Standardize case of identifiers

## [6.6.0] - 2019-03-29

* Ask people to upgrade VS if missing languageservices

### C# -> VB
* Improve event identifier conversion

### VB -> C#
* Improve conversion of interpolated strings (format, alignment, escaping)

## [6.5.0] - 2019-03-03
* Avoid fatal error converting a project in a solution containing a website project ([#243](https://github.com/icsharpcode/CodeConverter/pull/243))
* Improve best-effort conversion in the presence of errors
* Improved nuget package and web converter's snippet detection
* Exclude conversion-source-language files from converted project
* Improve conversion of type casts
* Web UI tweaks

### C# -> VB
* Fix for interpolated strings and switch statements in VS2019 Preview

## [6.4.0] - 2019-02-07
Fix initialization bug in VS2017

### C# -> VB
* Tuples now converted
* All known operator overloads now converted

## [6.3.0] - 2019-02-05
* VS 2019 support
* Breaking API change: Most library API names and return types are now async
* Improve VS startup time by making package load async
* Added SourceLink

### VB -> C#
* Private setter added to conversion of ReadOnly properties to cater for backing field usage
* Usage of compiler generated event variable name converted correctly
* Access modifiers no longer added erroneously to static constructor 
* "Do Until" construct multi-part conditions are correctly converted
* Tuple conversion support added
* VB -> C#: Error (lot of comments about the issue) when define an array with number of elements
* Decimal division conversion bugfix

### C# -> VB
* GoTo Case with dot in name converted correctly

## [6.2.0] - 2018-11-19

### VB -> C#

* Fix array indexing from outside a class
* Escape double quotes with string interpolation
* Converts date literals
* Converts ChrW and Chr
* Converts type promoted module members
* Converts EntryPoint and Charset for native functions
* Converts WriteOnly interface properties
* Converts "Integer?" to int?
* Converts property setters with parameters not named "value"
* Fixed bug causing duplicate namespace qualification
* Parenthesizes ternary conditional when necessary to preserve logic

### C# -> VB

* Convert enums with explicit base type

## [6.1.0] - 2018-10-12

### VB -> C#
* Parenthesize "as" cast if necessary
* Convert properties using "Item" syntax
* Extract variable for "To" expression
* Convert base constructor call missing parentheses
* Fix Nullref in SyntaxFactory.MethodDeclaration


## [6.0.0] - 2018-08-21
* Performance improvement for large solutions
* Fix solution/project level context menu item not appearing when projects are within folders

### VB -> C#
* Fix solution level conversion issues for projects other than the first one
* Improve query syntax support (some forms of group now supported)

## [5.9.0] - 2018-08-01

* Note: This release downgrades the library to net standard 1.3 for compatibility reasons - this should fix "could not load file or assembly netstandard, Culture=neutral'" error

### VB -> C#
* Type inferred const - convert to explicit type
* Improve name qualification
* Add parentheses around conditional expression in string interpolation
* Add parentheses where needed when convert Not, CType and TypeOf
* Fix namespace conversion issues
* Conversion error for indexer on property value of unresolved type
* Single-line lambda with statement body not implemented
* Array literals not always converted to implicit C# array

## [5.8.0] - 2018-06-26
* Move options lower down in the context menus so they aren't in the way 

### VB -> C#
* Handle WithEvents fields without initializers
* In lambda, use parentheses around single explicitly typed parameter
* Convert LocalDeclarationStatementSyntax
* Default parameter "value" now has the correct case
* Multiline xml doc comment conversion bugfix

### C# -> VB
* Interfaces "implements" clause now converted
* Shared no longer appears on Module members
* Fixed .Name bug with anonymous object creation
* Use Is and IsNot for reference type comparison

## [5.7.0] - 2018-05-08
* Update to .NET Standard 2.0
* Convert solution and project files
* Added convert and copy to clipboard into options
 
### VB -> C#
* Convert WithEvents/Handles
* Convert `Handles` and `WithEvents` similarly to the VB compiler
* Handle expressions in Select Case
* Ensure all parts of a partial class have the partial modifier
* Handle missing optional arguments
* Increase number of default global imports for web conversion
* Default properties now converted

### C# -> VB
* Fix DeclarationExpressions without a type throwing exception
* Convert Throw Expressions to multi-line lambda function
* Convert properties with no accessors
* Fix error converting ObjectCollectionInitializerSyntax within object initializer
* Escape predefined if they are used for the name of declaration

## [5.6.3] - 2018-04-09

* Improve support for sub-class snippets through the website
* Best effort conversion with errors as comments inline
* Tidy up duplicate import/using statements

### VB -> C#
* Using statements and array initialization improvements
* Convert select case expressions
* Fix for multi-parameter extension methods
* Convert single line void delegates 
* Fix array initialization incorrect conversion bugs
* Convert operator overloads
* Overestimate when a method should be invoked with no parameters
* Remove global namespace in conversion
* Add Imports System.Runtime.InteropServices when convering an out parameter
* Support Erase and Redim Preserve
* Convert select case expressions
* Extend support for converting with blocks

### C# -> VB
* Convert C# 7.0 features: "is pattern", throw and declaration expressions
* Convert expression bodies
* Fix to ensure Case Else always comes last in a Select statement
* Convert base class constructor call
* Fix convert char from integer cast
* Convert hex literals to hex literals (rather than just integers)
* Fix to avoid using "_" as a parameter name
* Fix for loop variable missing declaration and initialization
* Convert declare to extern
* Fix to improve accuracy of adding AddressOf
* Convert block syntax
* Convert empty statement
* Fix to avoid single line if-else statement's conversion causing compilation error
* Fix to avoid delegate with no parameters throwing NullReferenceException

## [5.6.2] - 2018-03-16

### VB -> C#
* Fix for change in logic when converting nested one-line if blocks
* Fix for loss of precision in arithmetic when using `/` operator
* Integer division and bit shifting now converted
* `Is` and `IsNot` operators now converted
* `Narrowing ` and `Widening` conversion operators now converted
* Linq query syntax with no select clause now converted
* `DirectCast` operator now converted
* `Shadows` modifier now converted (to `new`)

### C# -> VB
* Newline now appears before each attribute list
* Fix for `new` converting to `Shadows` - it now maps to `Overloads`

## [5.6.1] - 2018-03-06
* VSIX: Gets conversion off the UI thread to avoid it freezing
* VB -> C#: VB projects referencing other VB projects no longer error
* VB -> C#: XmlDoc comments now are correctly newline terminated

## [5.6.0] - 2018-03-05

### Visual studio extension
* New commands added to convert whole solution/project at once
* Works reliably in VS2017
* Instead of copy to clipboard, opens result as Visual Studio window for quick viewing
* Writes summary to output window to explain what's happened
* Project context used for all conversions

### VB -> C#
* Query expression syntax converted in simple cases
* Anonymous object creation converted
* Object initializer syntax converted
* Converts comments in the majority of cases (though with some formatting oddities)
* Now works on sub-method snippets
* Names now fully qualified where necessary
* Out parameters correctly converted

## [5.5.0] - 2017-12-29

* Move from Refactoring Essentials to a repository of its own
* Separate NuGet
* Separate VSIX
* Improvements on the VB side
