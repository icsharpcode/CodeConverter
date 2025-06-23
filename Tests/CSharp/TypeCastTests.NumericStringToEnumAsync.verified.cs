using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class NumericStringToEnum
{
    public static void Main()
    {
        Interaction.MsgBox(nameof(Main), (MsgBoxStyle)Conversions.ToInteger("1"), true);
    }
}
