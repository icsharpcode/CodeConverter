using System.Collections.Generic;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal struct KnownMethod
    {
        public string Import;
        public string TypeName;
        public string MethodName;

        public KnownMethod(string import, string typeName, string methodName)
        {
            this.Import = import;
            this.TypeName = typeName;
            this.MethodName = methodName;
        }

        public override bool Equals(object obj) =>
            obj is KnownMethod other && Import == other.Import && TypeName == other.TypeName && MethodName == other.MethodName;

        public override int GetHashCode() =>
            (Import, TypeName, MethodName).GetHashCode();

        public static implicit operator KnownMethod((string import, string typeName, string methodName) value) =>
            new KnownMethod(value.import, value.typeName, value.methodName);

        public ExpressionSyntax Invoke(HashSet<string> extraUsingDirectives, params ExpressionSyntax[] args)
        {
            extraUsingDirectives.Add(Import);
            return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(TypeName, MethodName), ExpressionSyntaxExtensions.CreateArgList(args));
        }
    }
}
