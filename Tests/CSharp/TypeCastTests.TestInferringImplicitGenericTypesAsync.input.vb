
Imports System
Imports System.Linq

Public Class TestClass
    Public Sub GenerateFromConstants
        Dim floatArr = Enumerable.Repeat(1.0F, 5).ToArray()        
        Dim doubleArr = Enumerable.Repeat(2.0, 5).ToArray()        
        Dim decimalArr = Enumerable.Repeat(3.0D, 5).ToArray()    
        Dim boolArr = Enumerable.Repeat(true, 5).ToArray()
        Dim intArr = Enumerable.Repeat(1, 5).ToArray()        
        Dim uintArr = Enumerable.Repeat(1ui, 5).ToArray()        
        Dim longArr = Enumerable.Repeat(1l, 5).ToArray()        
        Dim ulongArr = Enumerable.Repeat(1ul, 5).ToArray()
        Dim charArr = Enumerable.Repeat("a"c, 5).ToArray()        
        Dim strArr = Enumerable.Repeat("a", 5).ToArray()
        Dim objArr = Enumerable.Repeat(new object(), 5).ToArray()
    End Sub

    Public Sub GenerateFromCasts
        Dim floatArr = Enumerable.Repeat(CSng(1), 5).ToArray()        
        Dim doubleArr = Enumerable.Repeat(CDbl(2), 5).ToArray()        
        Dim decimalArr = Enumerable.Repeat(CDec(3), 5).ToArray()  
        Dim boolArr = Enumerable.Repeat(CBool(1), 5).ToArray()
        Dim intArr = Enumerable.Repeat(CInt(1.0), 5).ToArray()        
        Dim uintArr = Enumerable.Repeat(CUInt(1.0), 5).ToArray()        
        Dim longArr = Enumerable.Repeat(CLng(1.0), 5).ToArray()        
        Dim ulongArr = Enumerable.Repeat(CULng(1.0), 5).ToArray()
        Dim charArr = Enumerable.Repeat(CChar("a"), 5).ToArray()        
        Dim strArr = Enumerable.Repeat(CStr("a"c), 5).ToArray()
        Dim objArr1 = Enumerable.Repeat(CObj("a"), 5).ToArray()        
        Dim objArr2 = Enumerable.Repeat(CType("a", object), 5).ToArray()
    End Sub
End Class
