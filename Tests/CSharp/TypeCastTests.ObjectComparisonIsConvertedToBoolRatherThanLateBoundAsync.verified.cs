using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class CopiedFromTheSelfVerifyingBooleanTests
{
    public void VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
    {
        object a1 = 3;
        object a2 = 3;
        AssertTrue(Operators.ConditionalCompareObjectEqual(a1, a2, false), "Identical values stored in objects should be equal");
    }

    private void AssertTrue(bool? v1, string v2)
    {
    }

    private void AssertTrue(bool v1, string v2)
    {
    }
}