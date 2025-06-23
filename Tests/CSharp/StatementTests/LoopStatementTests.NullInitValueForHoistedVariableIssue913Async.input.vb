Public Class VisualBasicClass
Private Shared Sub ProblemsWithPullingVariablesOut()
      ' example 1
      For Each a In New List(Of String)
          Dim b As Long
          If a = "" Then
              b = 1
          End If
          DoSomeImportantStuff(b)
      Next

      ' example 2
      Dim c As String
      Do While True
          Dim d As Long
          If c = "" Then
              d = 1
          End If

         DoSomeImportantStuff(d)
         Exit Do
     Loop
 End Sub

Private Shared Sub ProblemsWithPullingVariablesOut_AlwaysWriteBeforeRead()
      ' example 1
      For Each a In New List(Of String)
          Dim b As Long
          If a = "" Then
              b = 1
          End If
          DoSomeImportantStuff()
      Next

      ' example 2
      Dim c As String
      Do While True
          Dim d As Long
          If c = "" Then
              d = 1
          End If

         DoSomeImportantStuff()
         Exit Do
     Loop
 End Sub
 Private Shared Sub DoSomeImportantStuff()
     Debug.Print("very important")
 End Sub
 Private Shared Sub DoSomeImportantStuff(b as Long)
     Debug.Print("very important")
 End Sub
End Class