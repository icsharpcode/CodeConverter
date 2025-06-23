using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class StringExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task MultilineStringAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        Dim x = ""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
        Dim y = $""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string x = @""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
        string y = $@""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task QuoteCharacterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class C
    Public Sub s
        Dim x As String = Chr(34)
        x = Chr(92)
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
public partial class C
{
    public void s()
    {
        string x = ""\"""";
        x = @""\"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task QuotesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Shared Function GetTextFeedInput(pStream As String, pTitle As String, pText As String) As String
        Return ""{"" & AccessKey() & "",""""streamName"""": """""" & pStream & """""",""""point"""": ["" & GetTitleTextPair(pTitle, pText) & ""]}""
    End Function

    Shared Function AccessKey() As String
        Return """"""accessKey"""": """"8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke""""""
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As Integer) As String
        Return (""{""""name"""": """""" & pName & """""", """"value"""": """""" & pValue & """"""}"")
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As String) As String
        Return (""{""""name"""": """""" & pName & """""", """"value"""": """""" & pValue & """"""}"")
    End Function

    Shared Function GetTitleTextPair(pName As String, pValue As String) As String
        Return (""{""""title"""": """""" & pName & """""", """"msg"""": """""" & pValue & """"""}"")
    End Function
    Shared Function GetDeltaPoint(pDelta As Integer) As String
        Return (""{""""delta"""": """""" & pDelta & """"""}"")
    End Function
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class TestClass
{
    public static string GetTextFeedInput(string pStream, string pTitle, string pText)
    {
        return ""{"" + AccessKey() + "",\""streamName\"": \"""" + pStream + ""\"",\""point\"": ["" + GetTitleTextPair(pTitle, pText) + ""]}"";
    }

    public static string AccessKey()
    {
        return ""\""accessKey\"": \""8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke\"""";
    }

    public static string GetNameValuePair(string pName, int pValue)
    {
        return ""{\""name\"": \"""" + pName + ""\"", \""value\"": \"""" + pValue + ""\""}"";
    }

    public static string GetNameValuePair(string pName, string pValue)
    {
        return ""{\""name\"": \"""" + pName + ""\"", \""value\"": \"""" + pValue + ""\""}"";
    }

    public static string GetTitleTextPair(string pName, string pValue)
    {
        return ""{\""title\"": \"""" + pName + ""\"", \""msg\"": \"""" + pValue + ""\""}"";
    }
    public static string GetDeltaPoint(int pDelta)
    {
        return ""{\""delta\"": \"""" + pDelta + ""\""}"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringCompareAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Class1
    Sub Foo()
        Dim s1 As String = Nothing
        Dim s2 As String = """"
        If s1 <> s2 Then
            Throw New Exception()
        End If
        If s1 = ""something"" Then
            Throw New Exception()
        End If
        If ""something"" = s1 Then
            Throw New Exception()
        End If
        If s1 = Nothing Then
            '
        End If
        If s1 = """" Then
            '
        End If
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

public partial class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = """";
        if ((s1 ?? """") != (s2 ?? """"))
        {
            throw new Exception();
        }
        if (s1 == ""something"")
        {
            throw new Exception();
        }
        if (""something"" == s1)
        {
            throw new Exception();
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringCompareTextAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Option Compare Text
Public Class Class1
    Sub Foo()
        Dim s1 As String = Nothing
        Dim s2 As String = """"
        If s1 <> s2 Then
            Throw New Exception()
        End If
        If s1 = ""something"" Then
            Throw New Exception()
        End If
        If ""something"" = s1 Then
            Throw New Exception()
        End If
        If s1 = Nothing Then
            '
        End If
        If s1 = """" Then
            '
        End If
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;
using System.Globalization;

public partial class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = """";
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1 ?? """", s2 ?? """", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) != 0)
        {
            throw new Exception();
        }
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1, ""something"", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
        {
            throw new Exception();
        }
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(""something"", s1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
        {
            throw new Exception();
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringCompareDefaultInstrAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Imports Microsoft.VisualBasic

Class Issue655
    Dim s1 = InStr(1, ""obj"", ""object '"")
    Dim s2 = InStrRev(1, ""obj"", ""object '"")
    Dim s3 = Replace(1, ""obj"", ""object '"")
    Dim s4 = Split(1, ""obj"", ""object '"")
    Dim s5 = Filter(New String() { 1, 2}, ""obj"")
    Dim s6 = StrComp(1, ""obj"")
    Dim s7 = OtherFunction()
    
    Function OtherFunction(Optional c As CompareMethod = CompareMethod.Binary) As Boolean
        Return c = CompareMethod.Binary
    End Function
End Class", extension: "vb"),
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Issue655
{
    private object s1 = Strings.InStr(1, ""obj"", ""object '"");
    private object s2 = Strings.InStrRev(1.ToString(), ""obj"", Conversions.ToInteger(""object '""));
    private object s3 = Strings.Replace(1.ToString(), ""obj"", ""object '"");
    private object s4 = Strings.Split(1.ToString(), ""obj"", Conversions.ToInteger(""object '""));
    private object s5 = Strings.Filter(new string[] { 1.ToString(), 2.ToString() }, ""obj"");
    private object s6 = Strings.StrComp(1.ToString(), ""obj"");
    private object s7;

    public Issue655()
    {
        s7 = OtherFunction();
    }

    public bool OtherFunction(CompareMethod c = CompareMethod.Binary)
    {
        return c == CompareMethod.Binary;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringCompareTextInstrAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Option Compare Text ' Comment omitted since line has no conversion
Imports Microsoft.VisualBasic

Class Issue655
    Dim s1 = InStr(1, ""obj"", ""object '"")
    Dim s2 = InStrRev(1, ""obj"", ""object '"")
    Dim s3 = Replace(1, ""obj"", ""object '"")
    Dim s4 = Split(1, ""obj"", ""object '"")
    Dim s5 = Filter(New String() { 1, 2}, ""obj"")
    Dim s6 = StrComp(1, ""obj"")
    Dim s7 = OtherFunction()
    
    Function OtherFunction(Optional c As CompareMethod = CompareMethod.Binary) As Boolean
        Return c = CompareMethod.Binary
    End Function
End Class", extension: "vb"),
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Issue655
{
    private object s1 = Strings.InStr(1, ""obj"", ""object '"", Compare: CompareMethod.Text);
    private object s2 = Strings.InStrRev(1.ToString(), ""obj"", Conversions.ToInteger(""object '""), Compare: CompareMethod.Text);
    private object s3 = Strings.Replace(1.ToString(), ""obj"", ""object '"", Compare: CompareMethod.Text);
    private object s4 = Strings.Split(1.ToString(), ""obj"", Conversions.ToInteger(""object '""), Compare: CompareMethod.Text);
    private object s5 = Strings.Filter(new string[] { 1.ToString(), 2.ToString() }, ""obj"");
    private object s6 = Strings.StrComp(1.ToString(), ""obj"", Compare: CompareMethod.Text);
    private object s7;

    public Issue655()
    {
        s7 = OtherFunction();
    }

    public bool OtherFunction(CompareMethod c = CompareMethod.Binary)
    {
        return c == CompareMethod.Binary;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringConcatPrecedenceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Class1
    Sub Foo()
        Dim x = ""x "" & 5 - 4 & "" y""
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
public partial class Class1
{
    public void Foo()
    {
        string x = ""x "" + (5 - 4) + "" y"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringConcatenationAssignmentAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        Dim str = ""Hello, ""
        str &= ""World""
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string str = ""Hello, "";
        str += ""World"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringInterpolationWithConditionalOperatorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Function GetString(yourBoolean as Boolean) As String
    Return $""You {if (yourBoolean, ""do"", ""do not"")} have a true value""
End Function", extension: "vb"),
                Verifier.Verify(@"public string GetString(bool yourBoolean)
{
    return $""You {(yourBoolean ? ""do"" : ""do not"")} have a true value"";
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringInterpolationWithDoubleQuotesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Imports System

Namespace Global.InnerNamespace
    Public Class Test
        Public Function StringInter(t As String, dt As DateTime) As String
            Dim a = $""pre{t} t""
            Dim b = $""pre{t} """" t""
            Dim c = $""pre{t} """"\ t""
            Dim d = $""pre{t & """"""""} """" t""
            Dim e = $""pre{t & """"""""} """"\ t""
            Dim f = $""pre{{escapedBraces}}{dt,4:hh}""
            Return a & b & c & d & e & f
        End Function
    End Class
End Namespace", extension: "vb"),
                Verifier.Verify(@"using System;

namespace InnerNamespace
{
    public partial class Test
    {
        public string StringInter(string t, DateTime dt)
        {
            string a = $""pre{t} t"";
            string b = $""pre{t} \"" t"";
            string c = $@""pre{t} """"\ t"";
            string d = $""pre{t + ""\""""} \"" t"";
            string e = $@""pre{t + ""\""""} """"\ t"";
            string f = $""pre{{escapedBraces}}{dt,4:hh}"";
            return a + b + c + d + e + f;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringInterpolationWithDateFormatAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Imports System

Namespace Global.InnerNamespace
    Public Class Test
           public function InterStringDateFormat(dt As DateTime) As String
            Dim a As String = $""Soak: {dt: d\.h\:mm\:ss\.f}""
            return a 
            End function
    End Class
End Namespace", extension: "vb"),
                Verifier.Verify(@"using System;

namespace InnerNamespace
{
    public partial class Test
    {
        public string InterStringDateFormat(DateTime dt)
        {
            string a = $""Soak: {dt: d\\.h\\:mm\\:ss\\.f}"";
            return a;
        }
    }
}", extension: "cs")
            );
        }
    }
    [Fact]
    public async Task NoConversionRequiredWithinConcatenationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Issue508
    Sub Foo()
        Dim x = ""x"" & 4 & ""y""
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
public partial class Issue508
{
    public void Foo()
    {
        string x = ""x"" + 4 + ""y"";
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EmptyStringCoalesceSkippedForLiteralComparisonAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class VisualBasicClass

    Sub Foo()
        Dim x = """"
        Dim y = x = ""something""
    End Sub

End Class", extension: "vb"),
                Verifier.Verify(@"
public partial class VisualBasicClass
{

    public void Foo()
    {
        string x = """";
        bool y = x == ""something"";
    }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue396ComparisonOperatorForStringsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Issue396ComparisonOperatorForStringsAsync
    Private str = 1.ToString()
    Private b = str > """"
End Class", extension: "vb"),
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue396ComparisonOperatorForStringsAsync
{
    private object str = 1.ToString();
    private object b;

    public Issue396ComparisonOperatorForStringsAsync()
    {
        b = Operators.ConditionalCompareObjectGreater(str, """", false);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue590EnumConvertsToNumericStringAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class EnumTests
    Private Enum RankEnum As SByte
        First = 1
        Second = 2
    End Enum

    Public Sub TestEnumConcat()
        Console.Write(RankEnum.First & RankEnum.Second)
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

public partial class EnumTests
{
    private enum RankEnum : sbyte
    {
        First = 1,
        Second = 2
    }

    public void TestEnumConcat()
    {
        Console.Write(RankEnum.First + RankEnum.Second);
    }
}
1 target compilation errors:
CS0019: Operator '+' cannot be applied to operands of type 'EnumTests.RankEnum' and 'EnumTests.RankEnum'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue806DateTimeConvertsToStringWithinConcatenationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Issue806
    Sub Foo()
        Dim x = #2022-01-01# & "" 15:00""
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue806
{
    public void Foo()
    {
        string x = Conversions.ToString(DateTime.Parse(""2022-01-01"")) + "" 15:00"";
    }
}", extension: "cs")
            );
        }
    }
}