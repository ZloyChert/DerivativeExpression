using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionDerivative
{
    public class CustomDerivativeExpressionTreeVisitor
    {
        public Expression<Func<double, double>> Visit(Expression<Func<double, double>> function)
        {
            //CallSite
            return Expression.Lambda<Func<double, double>>(Visitor.CreateFromExpression(function.Body).Visit(), function.Parameters);
        }
    }

    public abstract class Visitor
    {
        protected static readonly MethodInfo Pow = typeof(Math).GetMethod(nameof(Math.Pow));
        protected static readonly MethodInfo Log = typeof(Math).GetMethod(nameof(Math.Log), new[] { typeof(double) });
        protected readonly ConstantExpression Zero = Expression.Constant(0d, typeof(double));
        protected readonly ConstantExpression One = Expression.Constant(1d, typeof(double));
        public abstract Expression Visit();
        public static Visitor CreateFromExpression(Expression node)
            => node switch
            {
                BinaryExpression be => new BinaryVisitor(be),
                MethodCallExpression mce when mce.Method == Pow => new PowMethodCallVisitor(mce),
                _ => new SimpleVisitor(node),
            };
        
    }

    public class BinaryVisitor : Visitor
    {
        private readonly BinaryExpression _node;
        
        public BinaryVisitor(BinaryExpression node)
        {
            _node = node;
        }

        public override Expression Visit()
            => _node.NodeType switch
            {
                ExpressionType.Add => Expression.Add(CreateFromExpression(_node.Left).Visit(), CreateFromExpression(_node.Right).Visit()),
                ExpressionType.Subtract => Expression.Subtract(CreateFromExpression(_node.Left).Visit(), CreateFromExpression(_node.Right).Visit()),

                ExpressionType.Multiply when _node.Left is ConstantExpression && _node.Right is ConstantExpression => Zero,
                ExpressionType.Multiply when _node.Left is ConstantExpression && _node.Right is ParameterExpression => _node.Left,
                ExpressionType.Multiply when _node.Left is ParameterExpression && _node.Right is ConstantExpression => _node.Right,
                ExpressionType.Multiply => Expression.Add(Expression.Multiply(CreateFromExpression(_node.Left).Visit(), _node.Right), Expression.Multiply(_node.Left, CreateFromExpression(_node.Right).Visit())),

                ExpressionType.Divide when _node.Left is ConstantExpression && _node.Right is ConstantExpression => Zero,
                ExpressionType.Divide when _node.Left is ConstantExpression && _node.Right is ParameterExpression => Expression.Divide(_node.Left, Expression.Multiply(_node.Right, _node.Right)),
                ExpressionType.Divide when _node.Left is ParameterExpression && _node.Right is ConstantExpression => Expression.Divide(One, _node.Right),
                ExpressionType.Divide => Expression.Divide(Expression.Subtract(Expression.Multiply(CreateFromExpression(_node.Left).Visit(), _node.Right), Expression.Multiply(_node.Left, CreateFromExpression(_node.Right).Visit())), Expression.Multiply(_node.Right, _node.Right)),
            };
    }

    public class PowMethodCallVisitor : Visitor
    {
        private readonly MethodCallExpression _node;

        public PowMethodCallVisitor(MethodCallExpression node)
        {
            _node = node;
        }

        public override Expression Visit()
            => (_node.Arguments[0], _node.Arguments[1]) switch
            {
                (ConstantExpression constant, ParameterExpression _) => Expression.Multiply(_node, Expression.Call(null, Log, constant)),
                (ParameterExpression param, ConstantExpression constant) => Expression.Multiply(constant, Expression.Call(null, Pow, param, Expression.Constant((double)constant.Value - 1, typeof(double)))),
                (ConstantExpression constant, Expression expression) => Expression.Multiply(Expression.Multiply(CreateFromExpression(expression).Visit(), _node), Expression.Call(null, Log, constant)),
            };
    }

    public class SimpleVisitor : Visitor
    {
        private readonly Expression _node;

        public SimpleVisitor(Expression node)
        {
            _node = node;
        }

        public override Expression Visit()
            => _node.NodeType switch
            {
                ExpressionType.Constant => Zero,
                ExpressionType.Parameter => One,
            };
    }
}
