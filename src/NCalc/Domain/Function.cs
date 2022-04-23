using System;

namespace NCalc.Domain
{
    [Serializable]
    public class Function : LogicalExpression
    {
        public Function(Identifier identifier, LogicalExpression[] expressions)
        {
            Identifier = identifier;
            Expressions = expressions;
        }

        public Identifier Identifier { get; set; }

        public LogicalExpression[] Expressions { get; set; }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        [NonSerialized]
        internal Action<FunctionArgs> DelegateFunction;

        [NonSerialized]
        internal Func<Function, object> InternalDelegateFunction;

        [NonSerialized]
        internal bool Resolved;
    }
}