Imports System.Data

Class TestConstCharacterConversions
    Function GetItem(dr As DataRow) As Object
        Const a As String = Chr(7)
        Const b As String = ChrW(8)
        Const t As String = Chr(9)
        Const n As String = ChrW(10)
        Const v As String = Chr(11)
        Const f As String = ChrW(12)
        Const r As String = Chr(13)
        Const x As String = Chr(14)
        Const 字 As String = ChrW(&H5B57)
   End Function
End Class