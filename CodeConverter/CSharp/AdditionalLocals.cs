using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalLocals : IEnumerable<KeyValuePair<string, AdditionalLocal>>
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

        public AdditionalLocal AddAdditionalLocal(AdditionalLocal additionalLocal)
        {
            _additionalLocals.Peek().Add(additionalLocal.Id, additionalLocal);
            return additionalLocal;
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
    }
}
