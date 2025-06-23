Imports System

Public Class VisualBasicClass
    Dim SomeDate As String = "2022-01-01"
    Dim SomeDateDateParsed As Date? = If(String.IsNullOrEmpty(SomeDate), CType(Nothing, Date?), DateTime.Parse(SomeDate))
End Class