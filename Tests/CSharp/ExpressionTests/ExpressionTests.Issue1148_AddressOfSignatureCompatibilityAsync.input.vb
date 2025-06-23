
Imports System

Public Class Issue1148
    Public Shared FuncClass As Func(Of TestObjClass) = AddressOf FunctionReturningClass
    Public Shared FuncBaseClass As Func(Of TestBaseObjClass) = AddressOf FunctionReturningClass
    Public Shared FuncInterface As Func(Of ITestObj) = AddressOf FunctionReturningClass
    Public Shared FuncInterfaceParam As Func(Of ITestObj, ITestObj) = AddressOf CastObj
    Public Shared FuncClassParam As Func(Of TestObjClass, ITestObj) = AddressOf CastObj

    Public Shared Function FunctionReturningClass() As TestObjClass
        Return New TestObjClass()
    End Function

    Public Shared Function CastObj(obj As ITestObj) As TestObjClass
        Return CType(obj, TestObjClass)
    End Function

End Class

Public Class TestObjClass
    Inherits TestBaseObjClass
    Implements ITestObj
End Class

Public Class TestBaseObjClass
End Class

Public Interface ITestObj
End Interface
