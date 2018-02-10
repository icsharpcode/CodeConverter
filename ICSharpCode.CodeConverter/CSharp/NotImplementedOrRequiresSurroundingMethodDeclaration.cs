using System;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class NotImplementedOrRequiresSurroundingMethodDeclaration : NotImplementedException
    {
        public NotImplementedOrRequiresSurroundingMethodDeclaration(string message) : base(message)
        {
        }
    }
}