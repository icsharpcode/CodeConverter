Imports System

Public Structure SomeStruct
    Public FieldA As Integer
    Public FieldB As Integer
End Structure

Module Module1
   Sub Main()
      Dim myArray(0) As SomeStruct

      With myArray(0)
         .FieldA = 3
         .FieldB = 4
      End With

      'Outputs: FieldA was changed to New FieldA value 
      Console.WriteLine($"FieldA was changed to {myArray(0).FieldA}")
      Console.WriteLine($"FieldB was changed to {myArray(0).FieldB}")
      Console.ReadLine
   End Sub
End Module