using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public bool IsPointWithinBoundaryBox(double dblLat, double dblLon, object boundbox)
    {
        if (boundbox is not null)
        {
            bool boolInLatBounds = Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectLessEqual(dblLat, ((dynamic)boundbox).north, false), Operators.ConditionalCompareObjectGreaterEqual(dblLat, ((dynamic)boundbox).south, false))); // Less then highest (northmost) lat, AND more than lowest (southmost) lat
            bool boolInLonBounds = Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectGreaterEqual(dblLon, ((dynamic)boundbox).west, false), Operators.ConditionalCompareObjectLessEqual(dblLon, ((dynamic)boundbox).east, false))); // More than lowest (westmost) lat, AND less than highest (eastmost) lon
            return boolInLatBounds & boolInLonBounds;
        }
        else
        {
            // Throw New Exception("boundbox is null.")
        }
        return false;
    }
}