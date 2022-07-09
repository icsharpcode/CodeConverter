using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class TriviaTests : ConverterTestBase
{
    [Fact]
    public async Task Issue506_IfStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System

Public Class TestClass506
    Public Sub Deposit(Item As Integer, ColaOnly As Boolean, MonteCarloLogActive As Boolean, InDevEnv As Func(Of Boolean))

        If ColaOnly Then 'just log the Cola value
            Console.WriteLine(1)
        ElseIf (Item = 8 Or Item = 9) Then 'this is an indexing rate for inflation adjustment
            Console.WriteLine(2)
        Else 'this for a Roi rate from an assets parameters
            Console.WriteLine(3)
        End If
        If MonteCarloLogActive AndAlso InDevEnv() Then 'Special logging for dev debugging
            Console.WriteLine(4)
            'WriteErrorLog() 'write a blank line
        End If
    End Sub
End Class", @"using System;

public partial class TestClass506
{
    public void Deposit(int Item, bool ColaOnly, bool MonteCarloLogActive, Func<bool> InDevEnv)
    {

        if (ColaOnly) // just log the Cola value
        {
            Console.WriteLine(1);
        }
        else if (Item == 8 | Item == 9) // this is an indexing rate for inflation adjustment
        {
            Console.WriteLine(2);
        }
        else // this for a Roi rate from an assets parameters
        {
            Console.WriteLine(3);
        }
        if (MonteCarloLogActive && InDevEnv()) // Special logging for dev debugging
        {
            Console.WriteLine(4);
            // WriteErrorLog() 'write a blank line
        }
    }
}");
    }

    [Fact]
    public async Task Issue15_NestedRegionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"#Region ""Whole File""
#Region ""Nested""
Imports System

#Region ""Class""
Module Program
#Region ""Inside Class""
    Sub Main(args As String())
#Region ""Inside Method""
        Console.WriteLine(""Hello World!"")
#End Region
    End Sub
#End Region
End Module
#End Region
#End Region
#End Region
",
            @"#region Whole File
#region Nested
using System;

#region Class
internal static partial class Program
{
    #region Inside Class
    public static void Main(string[] args)
    {
        #region Inside Method
        Console.WriteLine(""Hello World!"");
        #endregion
    }
    #endregion
}
#endregion
#endregion
#endregion");
    }

    [Fact]
    public async Task RegionsWithEventsIssue772Async()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class VisualBasicClass
    Inherits System.Windows.Forms.Form

    #Region "" Members ""

        Private _Member As String = String.Empty

    #End Region

    #Region "" Construction ""

        Public Sub New()
        
        End Sub

    #End Region

    #Region "" Methods ""

        Public Sub Eventhandler_Load(sender As Object, e As EventArgs) Handles Me.Load
            'Do something
        End Sub

    #End Region

End Class",
            @"using System;

public partial class VisualBasicClass : System.Windows.Forms.Form
{

    #region  Members 

    private string _Member = string.Empty;

    #endregion

    #region  Construction 

    public VisualBasicClass()
    {
        Load += Eventhandler_Load;

    }

    #endregion

    #region  Methods 

    public void Eventhandler_Load(object sender, EventArgs e)
    {
        // Do something
    }

    #endregion

}");
    }

    [Fact]
    public async Task Issue15_IfTrueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class AClass
    #If TRUE
    Private Sub AMethod()
    End Sub
    #End If
End Class",
            @"
public partial class AClass
{
    /* TODO ERROR: Skipped IfDirectiveTrivia
#If TRUE
*/
    private void AMethod()
    {
    }
    /* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/
}
");
    }

    [Fact]
    public async Task Issue15_IfFalseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class AClass
    #If FALSE
    Private Sub AMethod()
    End Sub
    #End If
End Class",
            @"
public partial class AClass
{
    /* TODO ERROR: Skipped IfDirectiveTrivia
#If FALSE
*//* TODO ERROR: Skipped DisabledTextTrivia
    Private Sub AMethod()
    End Sub
*/    /* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/
}");
    }

    [Fact]
    public async Task Issue771_DoNotTrimLineCommentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
'>> Thomas  16.03.2021
'                       bei BearbeitungsTyp = ""SP__unten""
Public Class AClass
End Class",
            @"

// >> Thomas  16.03.2021
// bei BearbeitungsTyp = ""SP__unten""
public partial class AClass
{
}");
    }

    [Fact]
    public async Task Issue771_DoNotTrimBlockCommentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
''' >> Thomas  16.03.2021
'''                       bei BearbeitungsTyp = ""SP__unten""
Public Class AClass
End Class",
            @"

/// >> Thomas  16.03.2021
///                       bei BearbeitungsTyp = ""SP__unten""
public partial class AClass
{
}");
    }

    [Fact]
    public async Task TestMethodXmlDocAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    ''' <summary>Xml doc</summary>
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"
internal partial class TestClass
{
    /// <summary>Xml doc</summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}");
    }

    [Fact]
    public async Task TestGeneratedMethodXmlDocAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    '''<summary>
    '''  Returns the cached ResourceManager instance used by this class.
    '''</summary>
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"
internal partial class TestClass
{
    /// <summary>
    /// Returns the cached ResourceManager instance used by this class.
    /// </summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}");
    }

    [Fact]
    public async Task StatementNewlinesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System

Public Class X
    <Display(Name:=""Reinsurance Year"")> _
    Public SelectedReinsuranceYear As Int16

    
    <Display(Name:=""Record Type"")> _
    Public SelectedRecordType As String

    <Display(Name:=""Release Date"")> _
    Public ReleaseDate As Nullable(Of Date)

End Class

Friend Class DisplayAttribute
    Inherits Attribute
    Property Name As String
End Class
", @"using System;

public partial class X
{
    [Display(Name = ""Reinsurance Year"")]
    public short SelectedReinsuranceYear;


    [Display(Name = ""Record Type"")]
    public string SelectedRecordType;

    [Display(Name = ""Release Date"")]
    public DateTime? ReleaseDate;

}

internal partial class DisplayAttribute : Attribute
{
    public string Name { get; set; }
}");
    }

    [Fact]
    public async Task TestCarryingOverErrorTriviaAddedByConverterAsync()
    {
        var vbCode = @"Public Class VisualBasicClass
    Public Class TestClass
        Public Property A As Integer
        Public Property B As integer
    End class
   Public Sub New()
        Dim y As  New TestClass() With { .A = 1, .B = .A 
        }
   End Sub
End Class";

        var options = new TextConversionOptions(DefaultReferences.NetStandard2);
        var conversionResult = await ProjectConversion.ConvertTextAsync<VBToCSConversion>(vbCode, options);

        var regex = new Regex(@"#error Cannot convert \w+ - see comment for details\s+ \/\* Cannot convert.*?\*\/", RegexOptions.Singleline);
        Assert.Equal(1, conversionResult.Exceptions.Count);
        Assert.Matches(regex, conversionResult.ConvertedCode);
    }
}