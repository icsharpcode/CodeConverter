# Change Log
All notable changes to the code converter will be documented here.

# 7.1.0 12/09/2019

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

# 7.0.0 01/08/2019
* Compatible with Visual Studio ~15.5+

### VB -> C#
* Improve conversion for WithEvents/Handles
* Improve detection of enum related casts
* Convert parameterized properties
* Convert plain XML literals

### C# -> VB
* Convert more binary operators

# 6.9.0 09/06/2019

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

# 6.8.0 13/05/2019

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

# 6.7.0 09/04/2019

* Downgrade Roslyn requirement in attempt to work with VS2017 15.3+

### VB -> C#
* Ensure "new()" generic constraint is last
* Do not convert MyBase.Finalize, it's implicit
* Standardize case of identifiers

# 6.6.0 29/03/2019

* Ask people to upgrade VS if missing languageservices

### C# -> VB
* Improve event identifier conversion

### VB -> C#
* Improve conversion of interpolated strings (format, alignment, escaping)

# 6.5.0 03/03/2019
* Avoid fatal error converting a project in a solution containing a website project (#243)
* Improve best-effort conversion in the presence of errors
* Improved nuget package and web converter's snippet detection
* Exclude conversion-source-language files from converted project
* Improve conversion of type casts
* Web UI tweaks

### C# -> VB
* Fix for interpolated strings and switch statements in VS2019 Preview

# 6.4.0 07/02/2019
Fix initialization bug in VS2017

### C# -> VB
* Tuples now converted
* All known operator overloads now converted

# 6.3.0 05/02/2019
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

# 6.2.0 19/11/2018

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

# 6.1.0 - 12/10/2018

### VB -> C#
* Parenthesize "as" cast if necessary
* Convert properties using "Item" syntax
* Extract variable for "To" expression
* Convert base constructor call missing parentheses
* Fix Nullref in SyntaxFactory.MethodDeclaration


# 6.0.0 - 21/08/2018
* Performance improvement for large solutions
* Fix solution/project level context menu item not appearing when projects are within folders

### VB -> C#
* Fix solution level conversion issues for projects other than the first one
* Improve query syntax support (some forms of group now supported)

# 5.9.0 - 01/08/2018

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

# 5.8.0 - 26/06/2018
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

# 5.7.0 - 08/05/2018
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

# 5.6.3 - 09/04/2018

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

## 5.6.2 - 16/03/2018

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

## 5.6.1 - 06/03/2018
* VSIX: Gets conversion off the UI thread to avoid it freezing
* VB -> C#: VB projects referencing other VB projects no longer error
* VB -> C#: XmlDoc comments now are correctly newline terminated

## 5.6 - 05/03/2018

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

## 5.5 - 12/29/2017

* Move from Refactoring Essentials to a repository of its own
* Separate NuGet
* Separate VSIX
* Improvements on the VB side
