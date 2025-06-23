Option Compare Text ' Comment omitted since line has no conversion
Imports Microsoft.VisualBasic

Class Issue655
    Dim s1 = InStr(1, "obj", "object '")
    Dim s2 = InStrRev(1, "obj", "object '")
    Dim s3 = Replace(1, "obj", "object '")
    Dim s4 = Split(1, "obj", "object '")
    Dim s5 = Filter(New String() { 1, 2}, "obj")
    Dim s6 = StrComp(1, "obj")
    Dim s7 = OtherFunction()
    
    Function OtherFunction(Optional c As CompareMethod = CompareMethod.Binary) As Boolean
        Return c = CompareMethod.Binary
    End Function
End Class