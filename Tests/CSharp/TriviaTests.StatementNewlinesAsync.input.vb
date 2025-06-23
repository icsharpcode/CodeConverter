Imports System

Public Class X
    <Display(Name:="Reinsurance Year")> _
    Public SelectedReinsuranceYear As Int16

    
    <Display(Name:="Record Type")> _
    Public SelectedRecordType As String

    <Display(Name:="Release Date")> _
    Public ReleaseDate As Nullable(Of Date)

End Class

Friend Class DisplayAttribute
    Inherits Attribute
    Property Name As String
End Class
