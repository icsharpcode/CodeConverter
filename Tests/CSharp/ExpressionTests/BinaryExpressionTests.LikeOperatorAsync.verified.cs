using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        bool x = LikeOperator.LikeString("", "*x*", CompareMethod.Binary);
    }
}