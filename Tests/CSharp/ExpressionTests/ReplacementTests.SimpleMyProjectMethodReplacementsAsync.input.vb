
Imports System

Public Class SimpleMyProjectMethodReplacementsWork
    Sub TestMethod()
        Dim str1 As String
        Dim str2 As String
        Dim x As Object
        Dim dt As DateTime
        Dim Computer As New Microsoft.VisualBasic.Devices.Computer
        x = Computer.FileSystem.CurrentDirectory()
        x = Computer.FileSystem.GetTempFileName()
        x = Computer.FileSystem.CombinePath(str1, str2)
        x = Computer.FileSystem.GetDirectoryInfo(str1)
        x = Computer.FileSystem.GetDriveInfo(str1)
        x = Computer.FileSystem.GetFileInfo(str1)
        x = Computer.FileSystem.GetName(str1)
        x = Computer.FileSystem.ReadAllBytes(str1)
        x = Computer.FileSystem.ReadAllText(str1)
        x = Computer.FileSystem.DirectoryExists(str1)
        x = Computer.FileSystem.FileExists(str1)
        Computer.FileSystem.DeleteFile(str1)
        x = Computer.FileSystem.SpecialDirectories.Temp
        x = Computer.Info.InstalledUICulture
        x = Computer.Info.OSFullName
        x = Computer.Info.OSPlatform
        x = Computer.Info.OSVersion
    End Sub
End Class