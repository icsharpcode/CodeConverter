using System.Drawing;

public partial class AShape
{
    private RectangleF PaneArea;
    private int _OuterGap;
    public void SetSize(Rectangle clientRectangle)
    {
        var area = (RectangleF)clientRectangle;
        area.Inflate(-_OuterGap, -_OuterGap);
        PaneArea = area;
    }
}