using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Common;
using Microsoft.CodeAnalysis;
using Moq;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class SolutionFileTextEditorTests : IDisposable
{
    private Solution _sln;
    private const string SlnName = "MySolution";
    private const string SlnFilePath = @"C:\" + SlnName + @"\" + SlnName + ".sln";
    private Mock<IFileSystem> _fsMock;

    public SolutionFileTextEditorTests()
    {
        _sln = CreateTestSolution();
        _fsMock = new Mock<IFileSystem>();
    }

    public void Dispose()
    {
        _sln.Workspace.Dispose();
        _fsMock.Reset();
    }

    [Fact]
    public void ConvertSolutionFile_WhenInSolutionBaseDirThenUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", "VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", null);
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", "VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSlnxSolutionFile_ProjectElementsWithForwardSlashesAreUpdated()
    {
        //Arrange
        var slnxContents = "<Solution>\r\n  <Project Path=\"ConsoleApp1/ConsoleApp1.vbproj\" />\r\n</Solution>";
        var slnxSln = CreateTestSolution(@"C:\MySolution\MySolution.slnx");
        var projectId = ProjectId.CreateNewId();
        var projInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "ConsoleApp1", "ConsoleApp1",
            LanguageNames.VisualBasic, @"C:\MySolution\ConsoleApp1\ConsoleApp1.vbproj");
        slnxSln = slnxSln.AddProject(projInfo);
        var testProject = slnxSln.GetProject(projectId);

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> { testProject },
            fileSystem: _fsMock.Object, solutionContents: slnxContents);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = "<Solution>\r\n  <Project Path=\"ConsoleApp1/ConsoleApp1.csproj\" />\r\n</Solution>";
        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_WhenInProjectFolderThenUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary\VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_GivenDifferentLibraryWhenInDifferentProjectFolderThenNotUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary\Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary\Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary\VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_GivenDifferentLibraryWhenInSameProjectFolderThenNotUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary\VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_GivenDifferentLibraryWhenSameFileNameThenNotUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary", @"Prefix.VbLibrary\VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary", @"Prefix.VbLibrary\VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary", @"VbLibrary\VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_GivenDifferentLibraryWhenSameProjectNameThenNotUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary", @"Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary", @"VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", null);
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary", @"Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary", @"VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSolutionFile_GivenDifferentLibraryWhenInSolutionBaseDirThenNotUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", null);
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", @"VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public async Task Convert_WhenReferencedByProjFileInDifferentProjFolderThenUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibraryReferencer\VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        var referencingProject = AddTestProject("VbLibraryReferencer", "VbLibraryReferencer");
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] {new ProjectReference(testProject.Id)});
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new []
        {
            @"..\VbLibrary\VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new []
        {
            @"..\VbLibrary\VbLibrary.csproj"
        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public async Task Convert_WhenReferencedByProjFileInSameFolderThenUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", null);
        var referencingProject = AddTestProject("VbLibraryReferencer", null);
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] {new ProjectReference(testProject.Id)});
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new []
        {
            @"VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new []
        {
            @"VbLibrary.csproj"
        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public async Task Convert_WhenReferencedByProjFileInSlnFolderThenUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        var referencingProject = AddTestProject("VbLibraryReferencer", null);
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] {new ProjectReference(testProject.Id)});
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new []
        {
            @"VbLibrary\VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new []
        {
            @"VbLibrary\VbLibrary.csproj"
        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public async Task Convert_WhenReferencedProjectInSlnFolderThenUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibraryReferencer\VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", null);
        var referencingProject = AddTestProject("VbLibraryReferencer", "VbLibraryReferencer");
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] {new ProjectReference(testProject.Id)});
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new []
        {
            @"..\VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new []
        {
            @"..\VbLibrary.csproj"
        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public async Task Convert_GivenDifferentReferencedLibraryWhenSameProjFolderThenNotUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\Prefix.VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibraryReferencer\VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        var otherTestProject = AddTestProject("Prefix.VbLibrary", "VbLibrary");
        var referencingProject = AddTestProject("VbLibraryReferencer", "VbLibraryReferencer");
        var testProjReference = new ProjectReference(testProject.Id);
        var otherTestProjReference = new ProjectReference(otherTestProject.Id);
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] {testProjReference, otherTestProjReference});
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new []
        {
            @"..\VbLibrary\Prefix.VbLibrary.vbproj",
            @"..\VbLibrary\VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new []
        {
            @"..\VbLibrary\Prefix.VbLibrary.vbproj",
            @"..\VbLibrary\VbLibrary.csproj"

        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public async Task Convert_GivenDifferentReferencedLibraryWhenSameProjFileNameThenNotUpdatedAsync()
    {
        //Arrange
        var solutionProjectReference = GetSolutionProjectReference(new[]
        {
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", "Prefix.VbLibrary", @"Prefix.VbLibrary\VbLibrary.vbproj", "CFAB82CD-BA17-4F08-99E2-403FADB0C46A"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", "VbLibraryReferencer", @"VbLibraryReferencer\VbLibraryReferencer.vbproj", "5E05E1E2-4063-4941-831D-E5BA3B3C5F5C"),
            ("F184B08F-C81C-45F6-A57F-5ABD9991F28F", "VbLibrary", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary");
        var otherTestProject = AddTestProject("VbLibrary", "Prefix.VbLibrary");
        var referencingProject = AddTestProject("VbLibraryReferencer", "VbLibraryReferencer");
        var testProjReference = new ProjectReference(testProject.Id);
        var otherTestProjReference = new ProjectReference(otherTestProject.Id);
        _sln = _sln.WithProjectReferences(referencingProject.Id,
            new[] { testProjReference, otherTestProjReference });
        testProject = _sln.GetProject(testProject.Id);

        var projReference = GetProjectProjectReference(new[]
        {
            @"..\Prefix.VbLibrary\VbLibrary.vbproj",
            @"..\VbLibrary\VbLibrary.vbproj"
        });

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns(projReference);

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> { testProject },
            fileSystem: _fsMock.Object, solutionContents: solutionProjectReference);

        //Act
        var convertedProjFile = await GetConvertedCodeAsync(slnConverter, referencingProject);

        //Assert
        var expectedProjFile = GetProjectProjectReference(new[]
        {
            @"..\Prefix.VbLibrary\VbLibrary.vbproj",
            @"..\VbLibrary\VbLibrary.csproj"
        });

        Assert.Equal(expectedProjFile, Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public void ConvertSolutionFile_WhenProjectIsMultiTargetThenUpdated()
    {
        //Arrange
        var solutionFileProjectReference = GetSolutionProjectReference(new[]
        {
            ("778DAE3C-4631-46EA-AA77-85C1314464D9", @"VbLibrary\VbLibrary.vbproj", "23195658-FBE7-4A3E-B79D-91AAC2D428E7")
        });

        var testProject = AddTestProject("VbLibrary", "VbLibrary", "VbLibrary (net48)");
        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> {testProject},
            fileSystem: _fsMock.Object, solutionContents: solutionFileProjectReference);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = GetSolutionProjectReference(new[] {
            ("9A19103F-16F7-4668-BE54-9A1E7A4F7556", @"VbLibrary\VbLibrary.csproj", "913DD733-37BF-05CF-35C5-5BD4A0431C47")
        });

        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    private static string GetSolutionProjectReference(IEnumerable<(string ProjTypeGuid, string RelativeProjPath, string ProjRefGuid)> projRefTuples)
    {
        return GetSolutionProjectReference(projRefTuples
            .Select(tuple => (tuple.ProjTypeGuid, ProjName: Path.GetFileNameWithoutExtension(tuple.RelativeProjPath),
                tuple.RelativeProjPath, tuple.ProjRefGuid))
            .ToList());
    }

    private static string GetSolutionProjectReference(IReadOnlyCollection<(string ProjTypeGuid, string ProjName, string RelativeProjPath,
        string ProjRefGuid)> projRefTuples)
    {
        var referenceBuilder = new StringBuilder();
        var builderAppendMethod = projRefTuples.Count > 1
            ? (Action<string>) (stringToAppend => referenceBuilder.AppendLine(stringToAppend))
            : stringToAppend => referenceBuilder.Append(stringToAppend);

        builderAppendMethod("<Project>");

        foreach ((string projTypeGuid, string projName, string relativeProjPath, string projRefGuid) in projRefTuples)
        {
            var referenceStringToAppend = $@"Project(""{{{projTypeGuid}}}"") = ""{projName}"","
                                          + $@" ""{relativeProjPath}"", ""{{{projRefGuid}}}""
EndProject";
            builderAppendMethod(referenceStringToAppend);
        }

        foreach (var projRefTuple in projRefTuples)
        {
            var referenceStringToAppend = $@"{{{projRefTuple.ProjRefGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU";
            builderAppendMethod(referenceStringToAppend);
        }

        builderAppendMethod("</Project>");

        var referenceString = referenceBuilder.ToString();

        return Utils.HomogenizeEol(referenceString);
    }

    private static string GetProjectProjectReference(IReadOnlyCollection<string> relProjPaths)
    {
        var referenceBuilder = new StringBuilder();
        var builderAppendMethod = relProjPaths.Count > 1
            ? (Action<string>) (stringToAppend => referenceBuilder.AppendLine(stringToAppend))
            : stringToAppend => referenceBuilder.Append(stringToAppend);

        builderAppendMethod("<Project>");

        foreach (var relativeProjPath in relProjPaths)
        {
            var referenceStringToAppend = $@"<ProjectReference Include=""{relativeProjPath}"" />";
            builderAppendMethod(referenceStringToAppend);
        }

        builderAppendMethod("</Project>");

        var referenceString = referenceBuilder.ToString();

        return Utils.HomogenizeEol(referenceString);
    }

    private static Solution CreateTestSolution(string filePath = SlnFilePath)
    {
        var ws = Task.Run(() => ThreadSafeWorkspaceHelper.CreateAdhocWorkspace.GetValueAsync())
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - the tests don't deadlock
            .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        var solutionId = SolutionId.CreateNewId(SlnName);
        var versionStamp = VersionStamp.Create();
        var solutionInfo = SolutionInfo.Create(solutionId, versionStamp, filePath);

        return ws.AddSolution(solutionInfo);
    }

    private Project AddTestProject(string projFileName, string projDirName, string projName = null, string projExtension = ".vbproj")
    {
        var projectId = ProjectId.CreateNewId(debugName: projName);
        var versionStamp = VersionStamp.Create();
        var slnDirectoryName = Path.GetDirectoryName(_sln.FilePath) ?? "";
        var projDirPath = Path.Combine(slnDirectoryName, projDirName ?? string.Empty);
        var name = projName ?? projFileName;

        var projectInfo = ProjectInfo.Create(projectId, versionStamp, name, name, LanguageNames.VisualBasic,
            Path.Combine(projDirPath, projFileName + projExtension));

        _sln = _sln.AddProject(projectInfo);
        var project = _sln.GetProject(projectId);

        return project;
    }

    private static async Task<string> GetConvertedCodeAsync(SolutionConverter slnConverter, Project referencingProject)
    {
        var conversionResult = await slnConverter.ConvertAsync()
            .SingleAsync(result => result.SourcePathOrNull == referencingProject.FilePath);

        return conversionResult.ConvertedCode;
    }

    [Fact]
    public void ConvertSlnxSolutionFile_WhenInSolutionBaseDirThenUpdated()
    {
        //Arrange
        var slnxContents = "<Solution>\r\n  <Project Path=\"VbLibrary.vbproj\" />\r\n</Solution>";
        var slnxSln = CreateTestSolution(@"C:\MySolution\MySolution.slnx");
        var projectId = ProjectId.CreateNewId();
        var projInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "VbLibrary", "VbLibrary",
            LanguageNames.VisualBasic, @"C:\MySolution\VbLibrary.vbproj");
        slnxSln = slnxSln.AddProject(projInfo);
        var testProject = slnxSln.GetProject(projectId);

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> { testProject },
            fileSystem: _fsMock.Object, solutionContents: slnxContents);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = "<Solution>\r\n  <Project Path=\"VbLibrary.csproj\" />\r\n</Solution>";
        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }

    [Fact]
    public void ConvertSlnxSolutionFile_WhenInProjectFolderThenUpdated()
    {
        //Arrange
        var slnxContents = "<Solution>\r\n  <Project Path=\"VbLibrary\\VbLibrary.vbproj\" />\r\n</Solution>";
        var slnxSln = CreateTestSolution(@"C:\MySolution\MySolution.slnx");
        var projectId = ProjectId.CreateNewId();
        var projInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "VbLibrary", "VbLibrary",
            LanguageNames.VisualBasic, @"C:\MySolution\VbLibrary\VbLibrary.vbproj");
        slnxSln = slnxSln.AddProject(projInfo);
        var testProject = slnxSln.GetProject(projectId);

        _fsMock.Setup(mock => mock.File.ReadAllText(It.IsAny<string>())).Returns("");

        var slnConverter = SolutionConverter.CreateFor<VBToCSConversion>(new List<Project> { testProject },
            fileSystem: _fsMock.Object, solutionContents: slnxContents);

        //Act
        var convertedSlnFile = slnConverter.ConvertSolutionFile().ConvertedCode;

        //Assert
        var expectedSlnFile = "<Solution>\r\n  <Project Path=\"VbLibrary\\VbLibrary.csproj\" />\r\n</Solution>";
        Assert.Equal(expectedSlnFile, Utils.HomogenizeEol(convertedSlnFile));
    }
}