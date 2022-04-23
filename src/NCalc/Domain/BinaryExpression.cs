using System;

namespace NCalc.Domain
{
    [System.Serializable]
    public class BinaryExpression : LogicalExpression
    {
        public BinaryExpression(BinaryExpressionType type, LogicalExpression leftExpression, LogicalExpression rightExpression)
        {
            Type = type;
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }

        public LogicalExpression LeftExpression { get; set; }

        public LogicalExpression RightExpression { get; set; }

        public BinaryExpressionType Type { get; set; }

        [NonSerialized]
        internal Func<Func<object>, Func<object>, object> BinaryDelegate;

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public enum BinaryExpressionType
    {
        And,
        Or,
        NotEqual,
        LesserOrEqual,
        GreaterOrEqual,
        Lesser,
        Greater,
        Equal,
        Minus,
        Plus,
        Modulo,
        Div,
        Times,
        BitwiseOr,
        BitwiseAnd,
        BitwiseXOr,
        LeftShift,
        RightShift,
        Unknown,
        Exponentiation
    }
}
