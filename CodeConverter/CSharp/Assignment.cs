using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal struct Assignment
    {
        public ExpressionSyntax Field;
        public SyntaxKind AssignmentKind;
        public ExpressionSyntax Initializer;
        public bool PostAssignment;

        public Assignment(ExpressionSyntax field, SyntaxKind assignmentKind, ExpressionSyntax initializer, bool postAssignment = false)
        {
            Field = field;
            AssignmentKind = assignmentKind;
            Initializer = initializer;
            PostAssignment = postAssignment;
        }

        public override bool Equals(object obj) => obj is Assignment other &&
                   (other.Field, other.AssignmentKind, other.Initializer, other.PostAssignment).Equals((Field, AssignmentKind, Initializer, PostAssignment));


        public override int GetHashCode() => (Field, AssignmentKind, Initializer, PostAssignment).GetHashCode();

        public void Deconstruct(out ExpressionSyntax field, out SyntaxKind assignmentKind, out ExpressionSyntax initializer)
        {
            field = Field;
            assignmentKind = AssignmentKind;
            initializer = Initializer;
        }

        public static implicit operator Assignment((ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer) value)
        {
            return new Assignment(value.Field, value.AssignmentKind, value.Initializer);
        }
    }
}