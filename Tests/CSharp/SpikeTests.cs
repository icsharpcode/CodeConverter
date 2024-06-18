using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Xunit;


namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class SpikeTests
{

    [Fact]
    public async Task ConvertRazorFileAsync()
    {
        var codeDocument = GetCodeDocument("""
        @Code
            ViewData("Title") = "About" & CStr(1)
        End Code

        <main aria-labelledby="title">
            <h2 id="title">@ViewData("Title").</h2><h3 id="title">@ViewData("Title2").</h3>
            <h3>@ViewData("Message")</h3>

            <p>Use this area to provide additional information.</p>
        </main>@Code
            ViewData("Title") = "About" & CStr(1)
        End Code

        <main aria-labelledby="title">
            <h2 id="title">@ViewData("Title").</h2><h3 id="title">@ViewData("Title2").</h3>
            <h3>@ViewData("Message")</h3>

            <p>Use this area to provide additional information.</p>
        </main>

        """, "anything.vbhtml");
        
        var syntaxTree = codeDocument.GetSyntaxTree();
        var getRoot = syntaxTree.GetType().GetProperty("Root", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;
        var root = getRoot.Invoke(syntaxTree, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);
        var getDocument = root.GetType().GetProperty("Document", BindingFlags.Instance | BindingFlags.Public).GetMethod;
        var document = getDocument.Invoke(root, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);
        var getChildren = document.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public).GetMethod;
        var children = getChildren.Invoke(document, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);


    }

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    private static extern object get_Root(RazorSyntaxTree codeDocument);


    private RazorCodeDocument GetCodeDocument(string source, string filePath)
    {
        var taghelper = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
            //.BoundAttributeDescriptor(attr => attr.Name("show").TypeName("System.Boolean"))
            //.BoundAttributeDescriptor(attr => attr.Name("id").TypeName("System.Int32"))
            //.TagMatchingRuleDescriptor(rule => rule.RequireTagName("taghelper"))
            //.Metadata(TypeName("TestTagHelper"))
            .Build();

        var engine = CreateProjectEngine(_ => {
                //return builder.Features.Add(new VisualStudioEnableTagHelpersFeature());
            }
        );

        var sourceDocument = TestRazorSourceDocument.Create(source, normalizeNewLines: true);
        var importDocument = TestRazorSourceDocument.Create("@addTagHelper *, TestAssembly", filePath: filePath, relativePath: filePath);

        var codeDocument = engine.ProcessDesignTime(sourceDocument, FileKinds.Legacy, importSources: ImmutableArray.Create(importDocument), new[] { taghelper });
        return codeDocument;
        //return RazorWrapperFactory.WrapCodeDocument(codeDocument);
    }
    protected RazorProjectEngine CreateProjectEngine(Action<RazorProjectEngineBuilder> configure)
    {
        return RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create("c:\\tmp"), configure);
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable


public static class TestRazorSourceDocument
{

    public static MemoryStream CreateStreamContent(string content = "Hello, World!", Encoding encoding = null, bool normalizeNewLines = false)
    {
        var stream = new MemoryStream();
        encoding ??= Encoding.UTF8;
        using (var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true)) {
            if (normalizeNewLines) {
                content = NormalizeNewLines(content);
            }

            writer.Write(content);
        }

        stream.Seek(0L, SeekOrigin.Begin);

        return stream;
    }

    public static RazorSourceDocument Create(
        string content = "Hello, world!",
        Encoding encoding = null,
        bool normalizeNewLines = false,
        string filePath = "test.cshtml",
        string relativePath = "test.cshtml")
    {
        if (normalizeNewLines) {
            content = NormalizeNewLines(content);
        }

        var properties = new RazorSourceDocumentProperties(filePath, relativePath);
        return RazorSourceDocument.Create(content, encoding ?? Encoding.UTF8, properties);
    }

    public static RazorSourceDocument Create(
        string content,
        RazorSourceDocumentProperties properties,
        Encoding encoding = null,
        bool normalizeNewLines = false)
    {
        if (normalizeNewLines) {
            content = NormalizeNewLines(content);
        }

        return RazorSourceDocument.Create(content, encoding ?? Encoding.UTF8, properties);
    }

    private static string NormalizeNewLines(string content)
    {
        return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
    }
}