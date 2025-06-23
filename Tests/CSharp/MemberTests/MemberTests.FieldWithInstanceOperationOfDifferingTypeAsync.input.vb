Public Class DoesNotNeedConstructor
    Private ReadOnly ClassVariable1 As New ParallelOptions With {.MaxDegreeOfParallelism = 5}
End Class