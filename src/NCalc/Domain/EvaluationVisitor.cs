﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace NCalc.Domain
{
    public class EvaluationVisitor : LogicalExpressionVisitor
    {
        private readonly EvaluateOptions _options = EvaluateOptions.None;
        private readonly CultureInfo _cultureInfo;

        private bool IgnoreCase
        { get { return (_options & EvaluateOptions.IgnoreCase) == EvaluateOptions.IgnoreCase; } }

        public EvaluationVisitor(EvaluateOptions options) : this(options, CultureInfo.CurrentCulture)
        {
        }

        public EvaluationVisitor(EvaluateOptions options, CultureInfo cultureInfo)
        {
            _options = options;
            _cultureInfo = cultureInfo;
        }

        public object Result { get; private set; }

        private object Evaluate(LogicalExpression expression)
        {
            expression.Accept(this);
            return Result;
        }

        public override void Visit(LogicalExpression expression)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private static Type[] CommonTypes = new[] { typeof(Int64), typeof(Double), typeof(Boolean), typeof(String), typeof(Decimal) };

        /// <summary>
        /// Gets the the most precise type.
        /// </summary>
        /// <param name="a">Type a.</param>
        /// <param name="b">Type b.</param>
        /// <returns></returns>
        private static Type GetMostPreciseType(Type a, Type b)
        {
            foreach (Type t in CommonTypes)
            {
                if (a == t || b == t)
                {
                    return t;
                }
            }

            return a;
        }

        public int CompareUsingMostPreciseType(object a, object b)
        {
            Type mpt;
            if (a == null)
            {
                if (b == null)
                    return 0;
                mpt = GetMostPreciseType(null, b.GetType());
            }
            else
            {
                mpt = GetMostPreciseType(a.GetType(), b?.GetType());
            }

            return Comparer.Default.Compare(Convert.ChangeType(a, mpt, _cultureInfo), Convert.ChangeType(b, mpt));
        }

        public override void Visit(TernaryExpression expression)
        {
            // Evaluates the left expression and saves the value
            expression.LeftExpression.Accept(this);
            bool left = Convert.ToBoolean(Result, _cultureInfo);

            if (left)
            {
                expression.MiddleExpression.Accept(this);
            }
            else
            {
                expression.RightExpression.Accept(this);
            }
        }

        private static bool IsReal(object value)
        {
            var typeCode = Type.GetTypeCode(value.GetType());

            return typeCode == TypeCode.Decimal || typeCode == TypeCode.Double || typeCode == TypeCode.Single;
        }

        public override void Visit(BinaryExpression expression)
        {
            // simulate Lazy<Func<>> behavior for late evaluation
            object leftValue = null;
            Func<object> left = () =>
            {
                if (leftValue == null)
                {
                    expression.LeftExpression.Accept(this);
                    leftValue = Result;
                }
                return leftValue;
            };

            // simulate Lazy<Func<>> behavior for late evaluations
            object rightValue = null;
            Func<object> right = () =>
            {
                if (rightValue == null)
                {
                    expression.RightExpression.Accept(this);
                    rightValue = Result;
                }
                return rightValue;
            };

            if (expression.BinaryDelegate == null)
            {
                expression.BinaryDelegate = GetBinaryDelegate(expression);
            }

            Result = expression.BinaryDelegate(left, right);
        }

        private System.Func<Func<object>, Func<object>, object> GetBinaryDelegate(BinaryExpression expression)
        {
            switch (expression.Type)
            {
                case BinaryExpressionType.And:
                    return (left, right) => Convert.ToBoolean(left(), _cultureInfo) && Convert.ToBoolean(right(), _cultureInfo);
                    break;

                case BinaryExpressionType.Or:
                    return (left, right) => Convert.ToBoolean(left(), _cultureInfo) || Convert.ToBoolean(right(), _cultureInfo);
                    break;

                case BinaryExpressionType.Div:
                    return (left, right) => IsReal(left()) || IsReal(right())
                                 ? Numbers.Divide(left(), right())
                                 : Numbers.Divide(Convert.ToDouble(left(), _cultureInfo), right());
                    break;

                case BinaryExpressionType.Equal:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) == 0;
                    break;

                case BinaryExpressionType.Greater:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) > 0;
                    break;

                case BinaryExpressionType.GreaterOrEqual:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) >= 0;
                    break;

                case BinaryExpressionType.Lesser:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) < 0;
                    break;

                case BinaryExpressionType.LesserOrEqual:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) <= 0;
                    break;

                case BinaryExpressionType.Minus:
                    return (left, right) => Numbers.Soustract(left(), right());
                    break;

                case BinaryExpressionType.Modulo:
                    return (left, right) => Numbers.Modulo(left(), right());
                    break;

                case BinaryExpressionType.NotEqual:
                    // Use the type of the left operand to make the comparison
                    return (left, right) => CompareUsingMostPreciseType(left(), right()) != 0;
                    break;

                case BinaryExpressionType.Plus:
                    return (left, right) =>
                    {
                        var l = left();
                        if (l is string)
                        {
                            return String.Concat(l, right());
                        }
                        else
                        {
                            return Numbers.Add(l, right());
                        }
                    };

                    break;

                case BinaryExpressionType.Times:
                    return (left, right) => Numbers.Multiply(left(), right());
                    break;

                case BinaryExpressionType.BitwiseAnd:
                    return (left, right) => Convert.ToUInt16(left(), _cultureInfo) & Convert.ToUInt16(right());
                    break;

                case BinaryExpressionType.BitwiseOr:
                    return (left, right) => Convert.ToUInt16(left(), _cultureInfo) | Convert.ToUInt16(right());
                    break;

                case BinaryExpressionType.BitwiseXOr:
                    return (left, right) => Convert.ToUInt16(left(), _cultureInfo) ^ Convert.ToUInt16(right());
                    break;

                case BinaryExpressionType.LeftShift:
                    return (left, right) => Convert.ToUInt16(left(), _cultureInfo) << Convert.ToUInt16(right());
                    break;

                case BinaryExpressionType.RightShift:
                    return (left, right) => Convert.ToUInt16(left(), _cultureInfo) >> Convert.ToUInt16(right());
                    break;

                case BinaryExpressionType.Exponentiation:
                    return (left, right) => Math.Pow(Convert.ToDouble(left(), _cultureInfo), Convert.ToDouble(right()));
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public override void Visit(UnaryExpression expression)
        {
            // Recursively evaluates the underlying expression
            expression.Expression.Accept(this);

            switch (expression.Type)
            {
                case UnaryExpressionType.Not:
                    Result = !Convert.ToBoolean(Result, _cultureInfo);
                    break;

                case UnaryExpressionType.Negate:
                    Result = Numbers.Soustract(0, Result);
                    break;

                case UnaryExpressionType.BitwiseNot:
                    Result = ~Convert.ToUInt16(Result, _cultureInfo);
                    break;

                case UnaryExpressionType.Positive:
                    // No-op
                    break;
            }
        }

        public override void Visit(ValueExpression expression)
        {
            Result = expression.Value;
        }

        public override void Visit(Function function)
        {
            // Quickly reuse previous found definition
            if (function.InternalDelegateFunction != null)
            {
                Result = function.InternalDelegateFunction(function);
                return;
            }

            var args = new FunctionArgs
            {
                Parameters = new Expression[function.Expressions.Length]
            };

            // Don't call parameters right now, instead let the function do it as needed.
            // Some parameters shouldn't be called, for instance, in a if(), the "not" value might be a division by zero
            // Evaluating every value could produce unexpected behaviour
            for (int i = 0; i < function.Expressions.Length; i++)
            {
                args.Parameters[i] = new Expression(function.Expressions[i], _options, _cultureInfo);
                args.Parameters[i].EvaluateFunction += EvaluateFunction;
                args.Parameters[i].EvaluateParameter += EvaluateParameter;
                args.Parameters[i].ResolveFunction += ResolveFunction;
                args.Parameters[i].ResolveParameter += ResolveParameter;

                // Assign the parameters of the Expression to the arguments so that custom Functions and Parameters can use them
                args.Parameters[i].Parameters = Parameters;
            }

            if (!function.Resolved)
            {
                var resolveDelegateArg = new FunctionResolveArgs();
                OnResolveFunction(IgnoreCase ? function.Identifier.Name.ToLower() : function.Identifier.Name, args.Parameters.Length, resolveDelegateArg);
                function.DelegateFunction = resolveDelegateArg.Delegate;
                function.Resolved = true;
            }

            if (function.DelegateFunction != null)
            {
                function.DelegateFunction(args);
                Result = args.Result;
                return;
            }

            // Calls external implementation
            OnEvaluateFunction(IgnoreCase ? function.Identifier.Name.ToLower() : function.Identifier.Name, args);

            // If an external implementation was found get the result back
            if (args.HasResult)
            {
                Result = args.Result;
                return;
            }

            // Try to find an internal function
            var resolveInternalDelegateArg = new InternalFunctionResolveArgs();

            if (function.InternalDelegateFunction == null)
            {
                ResolveInternalFunction(function, resolveInternalDelegateArg);
                function.InternalDelegateFunction = resolveInternalDelegateArg.Delegate;
            }

            Result = function.InternalDelegateFunction(function);

            return;
        }

        private void ResolveInternalFunction(Function func, InternalFunctionResolveArgs fargs)
        {
            switch (func.Identifier.Name.ToLower())
            {
                #region Abs

                case "abs":

                    CheckCase("Abs", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Abs() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Abs(Convert.ToDecimal(
                            Evaluate(function.Expressions[0]), _cultureInfo)
                    );

                    break;

                #endregion Abs

                #region Acos

                case "acos":

                    CheckCase("Acos", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Acos() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Acos(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Acos

                #region Asin

                case "asin":

                    CheckCase("Asin", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Asin() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Asin(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Asin

                #region Atan

                case "atan":

                    CheckCase("Atan", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Atan() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Atan(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Atan

                #region Ceiling

                case "ceiling":

                    CheckCase("Ceiling", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Ceiling() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Ceiling(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Ceiling

                #region Cos

                case "cos":

                    CheckCase("Cos", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Cos() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Cos(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Cos

                #region Exp

                case "exp":

                    CheckCase("Exp", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Exp() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Exp(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Exp

                #region Floor

                case "floor":

                    CheckCase("Floor", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Floor() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Floor(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Floor

                #region IEEERemainder

                case "ieeeremainder":

                    CheckCase("IEEERemainder", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("IEEERemainder() takes exactly 2 arguments");

                    fargs.Delegate = (function) => Math.IEEERemainder(Convert.ToDouble(Evaluate(function.Expressions[0])), Convert.ToDouble(Evaluate(function.Expressions[1]), _cultureInfo));

                    break;

                #endregion IEEERemainder

                #region Ln

                case "ln":

                    CheckCase("Ln", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Ln() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Log(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Ln

                #region Log

                case "log":

                    CheckCase("Log", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("Log() takes exactly 2 arguments");

                    fargs.Delegate = (function) => Math.Log(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo), Convert.ToDouble(Evaluate(function.Expressions[1]), _cultureInfo));

                    break;

                #endregion Log

                #region Log10

                case "log10":

                    CheckCase("Log10", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Log10() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Log10(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Log10

                #region Pow

                case "pow":

                    CheckCase("Pow", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("Pow() takes exactly 2 arguments");

                    fargs.Delegate = (function) => Math.Pow(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo), Convert.ToDouble(Evaluate(function.Expressions[1]), _cultureInfo));

                    break;

                #endregion Pow

                #region Round

                case "round":

                    CheckCase("Round", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("Round() takes exactly 2 arguments");

                    MidpointRounding rounding = (_options & EvaluateOptions.RoundAwayFromZero) == EvaluateOptions.RoundAwayFromZero ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven;

                    fargs.Delegate = (function) => Math.Round(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo), Convert.ToInt16(Evaluate(function.Expressions[1]), _cultureInfo), rounding);

                    break;

                #endregion Round

                #region Sign

                case "sign":

                    CheckCase("Sign", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Sign() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Sign(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Sign

                #region Sin

                case "sin":

                    CheckCase("Sin", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Sin() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Sin(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Sin

                #region Sqrt

                case "sqrt":

                    CheckCase("Sqrt", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Sqrt() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Sqrt(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Sqrt

                #region Tan

                case "tan":

                    CheckCase("Tan", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Tan() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Tan(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Tan

                #region Truncate

                case "truncate":

                    CheckCase("Truncate", func.Identifier.Name);

                    if (func.Expressions.Length != 1)
                        throw new ArgumentException("Truncate() takes exactly 1 argument");

                    fargs.Delegate = (function) => Math.Truncate(Convert.ToDouble(Evaluate(function.Expressions[0]), _cultureInfo));

                    break;

                #endregion Truncate

                #region Max

                case "max":

                    CheckCase("Max", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("Max() takes exactly 2 arguments");

                    fargs.Delegate = (function) =>
                    {
                        object maxleft = Evaluate(function.Expressions[0]);
                        object maxright = Evaluate(function.Expressions[1]);

                        return Numbers.Max(maxleft, maxright);
                    };
                    break;

                #endregion Max

                #region Min

                case "min":

                    CheckCase("Min", func.Identifier.Name);

                    if (func.Expressions.Length != 2)
                        throw new ArgumentException("Min() takes exactly 2 arguments");

                    fargs.Delegate = (function) =>
                    {
                        object minleft = Evaluate(function.Expressions[0]);
                        object minright = Evaluate(function.Expressions[1]);

                        return Numbers.Min(minleft, minright);
                    };
                    break;

                #endregion Min

                #region if

                case "if":

                    CheckCase("if", func.Identifier.Name);

                    if (func.Expressions.Length != 3)
                        throw new ArgumentException("if() takes exactly 3 arguments");

                    fargs.Delegate = (function) =>
                    {
                        bool cond = Convert.ToBoolean(Evaluate(function.Expressions[0]), _cultureInfo);
                        return cond ? Evaluate(function.Expressions[1]) : Evaluate(function.Expressions[2]);
                    };
                    break;

                #endregion if

                #region in

                case "in":

                    CheckCase("in", func.Identifier.Name);

                    if (func.Expressions.Length < 2)
                        throw new ArgumentException("in() takes at least 2 arguments");
                    fargs.Delegate = (function) =>
                    {
                        object parameter = Evaluate(function.Expressions[0]);

                        bool evaluation = false;

                        // Goes through any values, and stop whe one is found
                        for (int i = 1; i < func.Expressions.Length; i++)
                        {
                            object argument = Evaluate(function.Expressions[i]);
                            if (CompareUsingMostPreciseType(parameter, argument) == 0)
                            {
                                evaluation = true;
                                break;
                            }
                        }

                        return evaluation;
                    };
                    break;

                #endregion in

                default:
                    throw new ArgumentException("Function not found",
                        func.Identifier.Name);
            }
        }

        private void CheckCase(string function, string called)
        {
            if (IgnoreCase)
            {
                if (function.ToLower() == called.ToLower())
                {
                    return;
                }

                throw new ArgumentException("Function not found", called);
            }

            if (function != called)
            {
                throw new ArgumentException(String.Format("Function not found {0}. Try {1} instead.", called, function));
            }
        }

        public event EvaluateFunctionHandler EvaluateFunction;

        public event ResolveFunctionHandler ResolveFunction;

        private void OnEvaluateFunction(string name, FunctionArgs args)
        {
            if (EvaluateFunction != null)
                EvaluateFunction(name, args);
        }

        private void OnResolveFunction(string name, int parameterCount, FunctionResolveArgs args)
        {
            if (ResolveFunction != null)
                ResolveFunction(name, parameterCount, args);
        }

        public override void Visit(Identifier parameter)
        {
            if (Parameters.ContainsKey(parameter.Name))
            {
                // The parameter is defined in the hashtable
                if (Parameters[parameter.Name] is Expression)
                {
                    // The parameter is itself another Expression
                    var expression = (Expression)Parameters[parameter.Name];

                    // Overloads parameters
                    foreach (var p in Parameters)
                    {
                        expression.Parameters[p.Key] = p.Value;
                    }

                    expression.EvaluateFunction += EvaluateFunction;
                    expression.EvaluateParameter += EvaluateParameter;

                    expression.ResolveFunction += ResolveFunction;
                    expression.ResolveParameter += ResolveParameter;

                    Result = ((Expression)Parameters[parameter.Name]).Evaluate();
                }
                else
                    Result = Parameters[parameter.Name];
            }
            else
            {
                if (parameter.ValueDelegate == null && ResolveParameter != null)
                {
                    var resolveArgs = new ParameterResolveArgs();

                    OnResolveParameter(parameter.Name, resolveArgs);
                    parameter.ValueDelegate = resolveArgs.Result;
                }
                if (parameter.ValueDelegate != null)
                {
                    Result = parameter.ValueDelegate();
                    if (Result != null)
                    {
                        return;
                    }
                }

                // The parameter should be defined in a call back method
                var args = new ParameterArgs();

                // Calls external implementation
                OnEvaluateParameter(parameter.Name, args);

                if (!args.HasResult)
                    throw new ArgumentException("Parameter was not defined", parameter.Name);

                Result = args.Result;
            }
        }

        public event EvaluateParameterHandler EvaluateParameter;
        public event ResolveParameterHandler ResolveParameter;

        private void OnEvaluateParameter(string name, ParameterArgs args)
        {
            if (EvaluateParameter != null)
                EvaluateParameter(name, args);
        }

        private void OnResolveParameter(string name, ParameterResolveArgs args)
        {
            if (ResolveParameter != null)
                ResolveParameter(name, args);
        }

        public Dictionary<string, object> Parameters { get; set; }
    }
}