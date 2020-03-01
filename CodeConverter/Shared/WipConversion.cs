using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    internal struct WipFileConversion
    {
        public static WipFileConversion<TWip> Create<TWip>(string path, TWip wip, string[] errors)
        {
            return new WipFileConversion<TWip>(path, wip, errors);
        }
    }

    public struct WipFileConversion<TWip>
    {
        public string Path;
        public TWip Wip;
        public string[] Errors;

        internal WipFileConversion(string path, TWip wip, string[] errors)
        {
            Path = path;
            Wip = wip;
            Errors = errors;
        }

        public override bool Equals(object obj)
        {
            return obj is WipFileConversion<TWip> other &&
                   Path == other.Path &&
                   EqualityComparer<TWip>.Default.Equals(Wip, other.Wip) &&
                   EqualityComparer<string[]>.Default.Equals(Errors, other.Errors);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public void Deconstruct(out string path, out TWip node, out string[] errors)
        {
            path = Path;
            node = Wip;
            errors = Errors;
        }

        public static implicit operator (string Path, TWip Node, string[] Errors)(WipFileConversion<TWip> value)
        {
            return (value.Path, value.Wip, value.Errors);
        }

        public static implicit operator WipFileConversion<TWip>((string Path, TWip Wip, string[] Errors) value)
        {
            return new WipFileConversion<TWip>(value.Path, value.Wip, value.Errors);
        }
    }
}