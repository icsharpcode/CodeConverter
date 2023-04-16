using System.Web.UI.WebControls;
using System.Xml.Linq;
using System;
using ICSharpCode.CodeConverter.Common;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class ProjectFileTextEditorTests
{
    [Fact]
    public void TogglesExistingValue()
    {
        var convertedProjFile = WithUpdatedDefaultItemExcludes(
            @"<Project>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <DefaultItemExcludes>$(DefaultItemExcludes);$(SpaRoot)node_modules\**;$(ProjectDir)**\*.cs</DefaultItemExcludes>
    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
  </PropertyGroup>
</Project>", "cs", "vb");

        Assert.Equal(Utils.HomogenizeEol(@"<Project>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <DefaultItemExcludes>$(DefaultItemExcludes);$(SpaRoot)node_modules\**;$(ProjectDir)**\*.vb</DefaultItemExcludes>
    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
  </PropertyGroup>
</Project>"),
            Utils.HomogenizeEol(convertedProjFile));
    }

    [Fact]
    public void InsertsIfNotPresent()
    {
        var convertedProjFile = WithUpdatedDefaultItemExcludes(
            @"<Project>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.2</TargetFramework>
  </PropertyGroup>
  <PropertyGroup>
    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
  </PropertyGroup>
</Project>", "cs", "vb");

        Assert.Equal(Utils.HomogenizeEol(@"<Project>
  <PropertyGroup>
    <TargetFramework>netcoreapp2.2</TargetFramework>
    <DefaultItemExcludes>$(DefaultItemExcludes);$(ProjectDir)**\*.vb</DefaultItemExcludes>
  </PropertyGroup>
  <PropertyGroup>
    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
  </PropertyGroup>
</Project>"),
            Utils.HomogenizeEol(convertedProjFile));
    }

    private static string WithUpdatedDefaultItemExcludes(string inputProjFile, string extensionNotToExclude, string extensionToExclude)
    {
        var xmlDoc = XDocument.Parse(inputProjFile);
        XNamespace xmlNs = xmlDoc.Root.GetDefaultNamespace();

        ProjectFileTextEditor.WithUpdatedDefaultItemExcludes(xmlDoc, xmlNs, extensionNotToExclude, extensionToExclude);

        return xmlDoc.Declaration != null ? xmlDoc.Declaration + Environment.NewLine + xmlDoc : xmlDoc.ToString();
    }
}