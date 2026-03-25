using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class MethodStatementTests_803 : ConverterTestBase
{
    [Fact]
    public async Task Issue803_SelectCaseWithRelationalPatternAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal Breite As Integer)
        Dim Rollo_FederUmdrehungen_Berechnen As Integer
        Select Case Breite
          Case Is < 1000
            Rollo_FederUmdrehungen_Berechnen = 12
          Case Is < 1200
            Rollo_FederUmdrehungen_Berechnen = 15
          Case Is < 1600
            Rollo_FederUmdrehungen_Berechnen = 19
          Case Is < 1800
            Rollo_FederUmdrehungen_Berechnen = 25
          Case Else
            Rollo_FederUmdrehungen_Berechnen = 28
        End Select
    End Sub
End Class", @"internal partial class TestClass
{
    private void TestMethod(int Breite)
    {
        int Rollo_FederUmdrehungen_Berechnen;
        switch (Breite)
        {
            case < 1000:
                {
                    Rollo_FederUmdrehungen_Berechnen = 12;
                    break;
                }

            case < 1200:
                {
                    Rollo_FederUmdrehungen_Berechnen = 15;
                    break;
                }

            case < 1600:
                {
                    Rollo_FederUmdrehungen_Berechnen = 19;
                    break;
                }

            case < 1800:
                {
                    Rollo_FederUmdrehungen_Berechnen = 25;
                    break;
                }

            default:
                {
                    Rollo_FederUmdrehungen_Berechnen = 28;
                    break;
                }
        }
    }
}");
    }
}
