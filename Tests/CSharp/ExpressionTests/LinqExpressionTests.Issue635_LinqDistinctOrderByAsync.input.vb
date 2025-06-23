
Imports System.Collections.Generic
Imports System.Linq

Public Class Issue635
    Dim l As List(Of Integer)
    Dim listSortedDistinct = From x In l Order By x Distinct
End Class