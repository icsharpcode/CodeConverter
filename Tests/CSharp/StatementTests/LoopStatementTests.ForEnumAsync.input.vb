Friend Enum MyEnum
    Zero
    One
End Enum

Friend Class ForEnumAsync
    Sub PrintLoop(startIndex As MyEnum, endIndex As MyEnum, [step] As MyEnum)
      For i = startIndex To endIndex Step [step]
        Debug.WriteLine(i)
      Next
      For i2 As MyEnum = startIndex To endIndex Step [step]
        Debug.WriteLine(i2)
      Next
      For i3 As MyEnum = startIndex To endIndex Step 3
        Debug.WriteLine(i3)
      Next
      For i4 As MyEnum = startIndex To 4
        Debug.WriteLine(i4)
      Next
    End Sub
End Class