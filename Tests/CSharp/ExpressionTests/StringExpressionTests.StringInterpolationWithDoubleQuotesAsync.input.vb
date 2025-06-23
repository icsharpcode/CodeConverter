Imports System

Namespace Global.InnerNamespace
    Public Class Test
        Public Function StringInter(t As String, dt As DateTime) As String
            Dim a = $"pre{t} t"
            Dim b = $"pre{t} "" t"
            Dim c = $"pre{t} ""\ t"
            Dim d = $"pre{t & """"} "" t"
            Dim e = $"pre{t & """"} ""\ t"
            Dim f = $"pre{{escapedBraces}}{dt,4:hh}"
            Return a & b & c & d & e & f
        End Function
    End Class
End Namespace