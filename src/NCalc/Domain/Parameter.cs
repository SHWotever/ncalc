using System;

namespace NCalc.Domain
{
    public class Identifier : LogicalExpression
    {
        public Identifier(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public Func<object> ValueDelegate { get; set; }
        public bool Resolved { get; internal set; }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
