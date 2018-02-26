# Change Log

All notable changes to the code converter will be documented here.

## 5.6 - 27/02/2018

### Visual studio extension
* Works reliably in VS2017
* Instead of copy to clipboard, opens result as Visual Studio window for quick viewing
* Opens conversion summary window to explain what's happened
* Project context used for all conversions

### VB -> C# specific
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