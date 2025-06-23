Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer, [step] As Integer)
      For i As Integer = startIndex To endIndex Step [step]
        Debug.WriteLine(i)
  Next
End Sub
End Class