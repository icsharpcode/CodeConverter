using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalLocals
    {
        public static SyntaxAnnotation Annotation = new SyntaxAnnotation("CodeconverterAdditionalLocal");

        private readonly Stack<List<IHoistedNode>> _hoistedNodesPerScope;

        public AdditionalLocals()
        {
            _hoistedNodesPerScope = new Stack<List<IHoistedNode>>();
        }

        public void PushScope()
        {
            _hoistedNodesPerScope.Push(new List<IHoistedNode>());
        }

        public void PopScope()
        {
            _hoistedNodesPerScope.Pop();
        }

        public T Hoist<T>(T additionalLocal) where T: IHoistedNode
        {
            _hoistedNodesPerScope.Peek().Add(additionalLocal);
            return additionalLocal;
        }

        public IReadOnlyCollection<AdditionalDeclaration> GetDeclarations()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalDeclaration>().ToArray();
        }

        public IReadOnlyCollection<AdditionalAssignment> GetPostAssignments()
        {
            return _hoistedNodesPerScope.Peek().OfType<AdditionalAssignment>().ToArray();
        }
    }
}
