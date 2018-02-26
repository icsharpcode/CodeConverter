using System;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class NotImplementedOrRequiresSurroundingMethodDeclaration : NotImplementedException
    {
        public NotImplementedOrRequiresSurroundingMethodDeclaration(string message) : base(message)
        {
        }
    }
}