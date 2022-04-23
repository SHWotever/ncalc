using System;

namespace NCalc.Domain
{
    [Serializable]
    public class Identifier : LogicalExpression
    {
        public Identifier(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [NonSerialized]
        public Func<object> ValueDelegate;

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}