using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class SelectObjectCaseIntegerTest
{
    public void S()
    {
        object o;
        int j;
        o = 2.0d;
        switch (o)
        {
            case var @case when Operators.ConditionalCompareObjectEqual(@case, 1, false):
                {
                    j = 1;
                    break;
                }
            case var case1 when Operators.ConditionalCompareObjectEqual(case1, 2, false):
                {
                    j = 2;
                    break;
                }
            case var case2 when Operators.ConditionalCompareObjectLessEqual(3, case2, false) && Operators.ConditionalCompareObjectLessEqual(case2, 4, false):
                {
                    j = 3;
                    break;
                }
            case var case3 when Operators.ConditionalCompareObjectGreater(case3, 4, false):
                {
                    j = 4;
                    break;
                }

            default:
                {
                    j = -1;
                    break;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code