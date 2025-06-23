MustInherit Class ClassA
    Implements System.IDisposable

    Protected MustOverride Sub Test()
    Public MustOverride Sub Dispose() Implements IDisposable.Dispose
End Class