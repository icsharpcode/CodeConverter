using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class StringExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task MultilineStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = ""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
        Dim y = $""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string x = @""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
        string y = $@""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
    }
}");
        }

        [Fact]
        public async Task QuotesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
End Class", @"
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
}");
        }

        [Fact]
        public async Task StringCompareAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
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
End Class", @"using System;

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

        if (s1 == null)
        {
            // 
        }

        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
    }
}");
        }

        [Fact]
        public async Task StringCompareTextAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Option Compare Text
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
End Class", @"using System;
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

        if (s1 == null)
        {
            // 
        }

        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
    }
}");
        }

        [Fact]
        public async Task StringConcatPrecedenceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim x = ""x "" & 5 - 4 & "" y""
    End Sub
End Class", @"
public partial class Class1
{
    public void Foo()
    {
        string x = ""x "" + (5 - 4) + "" y"";
    }
}");
        }

        [Fact]
        public async Task StringConcatenationAssignmentAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim str = ""Hello, ""
        str &= ""World""
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string str = ""Hello, "";
        str += ""World"";
    }
}");
        }

        [Fact]
        public async Task StringInterpolationWithConditionalOperatorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Function GetString(yourBoolean as Boolean) As String
    Return $""You {if (yourBoolean, ""do"", ""do not"")} have a true value""
End Function",
                @"public string GetString(bool yourBoolean)
{
    return $""You {(yourBoolean ? ""do"" : ""do not"")} have a true value"";
}");
        }

        [Fact]
        public async Task StringInterpolationWithDoubleQuotesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Imports System

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
End Namespace",
@"using System;

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
}");
        }

        [Fact]
        public async Task NoConversionRequiredWithinConcatenationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class Issue508
    Sub Foo()
        Dim x = ""x"" & 4 & ""y""
    End Sub
End Class",
@"
public partial class Issue508
{
    public void Foo()
    {
        string x = ""x"" + 4 + ""y"";
    }
}");
        }

        [Fact]
        public async Task EmptyStringCoalesceSkippedForLiteralComparisonAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class VisualBasicClass

    Sub Foo()
        Dim x = """"
        Dim y = x = ""something""
    End Sub

End Class",
@"
public partial class VisualBasicClass
{
    public void Foo()
    {
        string x = """";
        bool y = x == ""something"";
    }
}");
        }

        [Fact]
        public async Task Issue396ComparisonOperatorForStringsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class Issue396ComparisonOperatorForStringsAsync
    Private str = 1.ToString()
    Private b = str > """"
End Class",
@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue396ComparisonOperatorForStringsAsync
{
    public Issue396ComparisonOperatorForStringsAsync()
    {
        b = Operators.ConditionalCompareObjectGreater(str, """", false);
    }

    private object str = 1.ToString();
    private object b;
}");
        }

        [Fact]
        public async Task Issue590EnumConvertsToNumericStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class EnumTests
    Private Enum RankEnum As SByte
        First = 1
        Second = 2
    End Enum

    Public Sub TestEnumConcat()
        Console.Write(RankEnum.First & RankEnum.Second)
    End Sub
End Class",
@"using System;

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
CS0019: Operator '+' cannot be applied to operands of type 'EnumTests.RankEnum' and 'EnumTests.RankEnum'");
        }
    }
}
