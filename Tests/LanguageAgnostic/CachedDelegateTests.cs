using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Xunit;
using VbCompilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class CachedDelegateTests
{
    [Fact]
    public async Task GetEmbeddedSyntaxTree_ExtensionMethodResolves()
    {
        // Arrange
        var compilation = await CreateSimpleClassCompilationAsync();
        var location = compilation.SourceModule.GlobalNamespace.Locations.FirstOrDefault();

        // Act - Should not throw when resolving the delegate
        var syntaxTree = location.EmbeddedSyntaxTree;

        // Assert
        Assert.NotNull(syntaxTree);
    }

    [Fact]
    public async Task IsMyGroupCollectionProperty_ExtensionMethodResolves()
    {
        // Arrange
        var (compilation, propertySymbol) = await CreateCompilationWithPropertyAsync();

        // Act - Should not throw when resolving the delegate
        var isMyGroupCollection = propertySymbol.IsMyGroupCollectionProperty;

        // Assert
        Assert.False(isMyGroupCollection); // Regular properties are not MyGroup collection properties
    }

    [Fact]
    public async Task GetAssociatedField_ExtensionMethodResolves()
    {
        // Arrange
        var (compilation, propertySymbol) = await CreateCompilationWithPropertyAsync();

        // Act - Should not throw when resolving the delegate
        var associatedField = propertySymbol.AssociatedField;

        // Assert
        // Auto-properties should have an associated backing field
        Assert.NotNull(associatedField);
    }

    [Fact]
    public async Task GetAssociatedField_ReturnsNullForManualProperty()
    {
        // Arrange
        var code = @"
Namespace TestNamespace
    Public Class TestClass
        Private _field As String
        
        Public Property TestProperty As String
            Get
                Return _field
            End Get
            Set(value As String)
                _field = value
            End Set
        End Property
    End Class
End Namespace";
        var compilation = await CreateVbCompilationAsync(code);
        var propertySymbol = GetPropertySymbol(compilation, "TestProperty");

        // Act
        var associatedField = propertySymbol.AssociatedField;

        // Assert
        // Properties with explicit backing fields should not have an associated field
        Assert.Null(associatedField);
    }

    [Fact]
    public async Task GetVbUnassignedVariables_ExtensionMethodResolves()
    {
        // Arrange
        var (compilation, dataFlowAnalysis) = await CreateCompilationWithUnassignedVariableAsync();

        // Act
        var unassignedVars = dataFlowAnalysis.VbUnassignedVariables;

        // Assert
        Assert.NotNull(unassignedVars);

        if (unassignedVars.Any())
        {
            Assert.Contains(unassignedVars, s => s.Name == "x");
        }
    }

    [Fact]
    public async Task GetVbUnassignedVariables_EmptyForFullyAssignedVariables()
    {
        // Arrange
        var code = @"
Namespace TestNamespace
    Public Class TestClass
        Public Function TestMethod() As Integer
            Dim x As Integer = 10
            Dim y As Integer = 5
            Return x + y
        End Function
    End Class
End Namespace";
        var compilation = await CreateVbCompilationAsync(code);
        var dataFlowAnalysis = GetDataFlowAnalysis(compilation, "TestMethod");

        // Act
        var unassignedVars = dataFlowAnalysis.VbUnassignedVariables;

        // Assert
        Assert.NotNull(unassignedVars);
        // Both variables are assigned, so there should be no unassigned variables
        Assert.DoesNotContain(unassignedVars, s => s.Name == "x" || s.Name == "y");
    }

    private static Task<VbCompilation> CreateSimpleClassCompilationAsync()
    {
        var code = @"
Namespace TestNamespace
    Public Class TestClass
    End Class
End Namespace";
        return CreateVbCompilationAsync(code);
    }

    private static async Task<(VbCompilation compilation, IPropertySymbol property)> CreateCompilationWithPropertyAsync()
    {
        var code = @"
Namespace TestNamespace
    Public Class TestClass
        Public Property TestProperty As String
    End Class
End Namespace";
        var compilation = await CreateVbCompilationAsync(code);
        var propertySymbol = GetPropertySymbol(compilation, "TestProperty");
        return (compilation, propertySymbol);
    }

    private static async Task<(VbCompilation compilation, DataFlowAnalysis dataFlow)> CreateCompilationWithUnassignedVariableAsync()
    {
        var code = @"
Namespace TestNamespace
    Public Class TestClass
        Public Function TestMethod() As Integer
            Dim x As Integer
            Dim y As Integer = 5
            Return y
        End Function
    End Class
End Namespace";
        var compilation = await CreateVbCompilationAsync(code);
        var dataFlowAnalysis = GetDataFlowAnalysis(compilation, "TestMethod");
        return (compilation, dataFlowAnalysis);
    }

    private static async Task<VbCompilation> CreateVbCompilationAsync(string code)
    {
        var compiler = new VisualBasicCompiler();
        var syntaxTree = compiler.CreateTree(code);
        var references = DefaultReferences.NetStandard2;
        var compilation = (VbCompilation)compiler.CreateCompilationFromTree(syntaxTree, references);
        
        // Ensure compilation succeeds
        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diagnostics);
        
        return compilation;
    }

    private static IPropertySymbol GetPropertySymbol(VbCompilation compilation, string propertyName)
    {
        var tree = compilation.SyntaxTrees.First();
        var root = tree.GetRoot();
        var propertyNode = root.DescendantNodes()
            .OfType<PropertyStatementSyntax>()
            .First(p => p.Identifier.Text == propertyName);
        
        var semanticModel = compilation.GetSemanticModel(tree);
        return semanticModel.GetDeclaredSymbol(propertyNode) as IPropertySymbol;
    }

    private static DataFlowAnalysis GetDataFlowAnalysis(VbCompilation compilation, string methodName)
    {
        var tree = compilation.SyntaxTrees.First();
        var root = tree.GetRoot();
        var method = root.DescendantNodes()
            .OfType<MethodBlockSyntax>()
            .First(m => m.SubOrFunctionStatement.Identifier.Text == methodName);
        
        var semanticModel = compilation.GetSemanticModel(tree);
        
        // Analyze the entire method body
        var statements = method.Statements;
        if (statements.Any())
        {
            return semanticModel.AnalyzeDataFlow(statements.First(), statements.Last());
        }
        
        throw new System.Exception($"Method '{methodName}' has no statements to analyze");
    }
}
