
Public Class C
    Public Function IsPointWithinBoundaryBox(ByVal dblLat As Double, dblLon As Double, ByVal boundbox As Object) As Boolean
        If boundbox IsNot Nothing Then
            Dim boolInLatBounds As Boolean = (dblLat <= boundbox.north) And (dblLat >= boundbox.south) 'Less then highest (northmost) lat, AND more than lowest (southmost) lat
            Dim boolInLonBounds As Boolean = (dblLon >= boundbox.west) And (dblLon <= boundbox.east) 'More than lowest (westmost) lat, AND less than highest (eastmost) lon
            Return boolInLatBounds And boolInLonBounds
        Else
            'Throw New Exception("boundbox is null.")
        End If
        Return False
    End Function
End Class
