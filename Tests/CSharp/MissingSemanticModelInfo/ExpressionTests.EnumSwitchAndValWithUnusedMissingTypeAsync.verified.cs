using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class EnumAndValTest
{
    public enum PositionEnum : int
    {
        None = 0,
        LeftTop = 1
    }

    public PositionEnum TitlePosition = PositionEnum.LeftTop;
    public PositionEnum TitleAlign = (PositionEnum)2;
    public float Ratio = 0f;

    public PositionEnum PositionEnumFromString(string pS, MissingType missing)
    {
        var tPos = default(PositionEnum);
        switch (pS.ToUpper() ?? "")
        {
            case "NONE":
            case "0":
                {
                    tPos = 0;
                    break;
                }
            case "LEFTTOP":
            case "1":
                {
                    tPos = (PositionEnum)1;
                    break;
                }

            default:
                {
                    Ratio = (float)Conversion.Val(pS);
                    break;
                }
        }
        return tPos;
    }
    public string PositionEnumStringFromConstant(PositionEnum pS)
    {
        string tS;
        switch (pS)
        {
            case 0:
                {
                    tS = "NONE";
                    break;
                }
            case (PositionEnum)1:
                {
                    tS = "LEFTTOP";
                    break;
                }

            default:
                {
                    tS = ((int)pS).ToString();
                    break;
                }
        }
        return tS;
    }
}
1 source compilation errors:
BC30002: Type 'MissingType' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)