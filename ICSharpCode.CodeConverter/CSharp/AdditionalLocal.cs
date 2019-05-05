using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalLocals : IEnumerable<KeyValuePair<string, AdditionalLocals.AdditionalLocal>>
    {
        public static SyntaxAnnotation Annotation = new SyntaxAnnotation("CodeconverterAdditionalLocal");

        private readonly Stack<Dictionary<string, AdditionalLocal>> _additionalLocals;

        public AdditionalLocals()
        {
            _additionalLocals = new Stack<Dictionary<string, AdditionalLocal>>();
        }

        public void PushScope()
        {
            _additionalLocals.Push(new Dictionary<string, AdditionalLocal>());
        }

        public void PopScope()
        {
            _additionalLocals.Pop();
        }

        public AdditionalLocal AddAdditionalLocal(string prefix, ExpressionSyntax initializer)
        {
            var local = new AdditionalLocal(prefix, initializer);
            _additionalLocals.Peek().Add(local.ID, local);
            return local;
        }

        public IEnumerator<KeyValuePair<string, AdditionalLocal>> GetEnumerator()
        {
            return _additionalLocals.Peek().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _additionalLocals.Peek().GetEnumerator();
        }

        public AdditionalLocal this[string id] {
            get {
                return _additionalLocals.Peek()[id];
            }
        }

        public class AdditionalLocal
        {
            public string Prefix { get; private set; }
            public string ID { get; private set; }
            public ExpressionSyntax Initializer { get; private set; }

            public AdditionalLocal(string prefix, ExpressionSyntax initializer)
            {
                Prefix = prefix;
                ID = $"ph{Guid.NewGuid().ToString("N")}";
                Initializer = initializer;
            }
        }
    }

}
