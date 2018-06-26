# Change Log
All notable changes to the code converter will be documented here.

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
