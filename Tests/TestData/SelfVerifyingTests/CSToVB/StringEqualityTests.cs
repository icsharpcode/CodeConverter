using Microsoft.VisualBasic.CompilerServices;
using Xunit;

public class StringEqualityTests
{
    private object nullObject = null;
    private string nullString = null;
    private object emptyStringObject = "";
    private string emptyString = "";
    private char[] emptyCharArray = new char[0];

    [Fact]
    public void NonInternStringsEqualsOperator() {
        string a = "10";
        string b = 10.ToString(); //strings created in runtime are not interned
        Assert.True(a == b);
    }

    [Fact]
    public void NullEmptyStringEqualsOperator() => Assert.False((emptyString != null && nullString != null) && emptyString == nullString); //null == string.Empty in VB

    [Fact]
    public void NullEmptyStringNotEqualsOperator() => Assert.True((emptyString == null && nullString != null) || (emptyString != null && nullString == null)); //null == string.Empty in VB

    [Fact]
    public void NullEmptyObjectEqualsOperator() => Assert.False(nullObject == emptyString);

    [Fact]
    public void NullEmptyObjectNotEqualsOperator() => Assert.True(nullObject != emptyString);

    [Fact]
    public void StringCharArrayEqualsOperator() => Assert.False(emptyStringObject == emptyCharArray);

    [Fact]
    public void StringCharArrayNotEqualsOperator() => Assert.True(emptyStringObject != emptyCharArray);

    [Fact]
    public void NullObjectEqualsOperator() => Assert.True(nullObject == nullString);

    [Fact]
    public void NullObjectNotEqualsOperator() => Assert.False(nullObject != nullString);

    [Fact]
    public void VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
    {
        object a1 = 3;
        object a2 = 3;
        object b = 4;
        Assert.False(a1 == a2, "Identical values stored in different objects should not be equal");
        Assert.False(a1 == b, "Different values stored in different objects should not be equal");
    }
}