Imports System

Public Class VisualBasicClass
        Dim SomeDate = ""
        Dim SomeDateDateNothing As Date? = If(String.IsNullOrEmpty(SomeDate), Nothing, DateTime.Parse(SomeDate))
        Dim isNotNothing = SomeDateDateNothing IsNot Nothing
        Dim isSomething = SomeDateDateNothing = New Date()
End Class