using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionDerivative
{
    public class BuildinExpressionTreeVisitor : ExpressionVisitor
    {
        private readonly MethodInfo _pow = typeof(Math).GetMethod(nameof(Math.Pow));
        private readonly MethodInfo _log = typeof(Math).GetMethod(nameof(Math.Log), new[] { typeof(double) });
        private readonly ConstantExpression _zero = Expression.Constant(0d, typeof(double));
        private readonly ConstantExpression _one = Expression.Constant(1d, typeof(double));

        public Expression<Func<double, double>> GetDerivative(Expression<Func<double, double>> function)
        {
            return Expression.Lambda<Func<double, double>>(Visit(function.Body), function.Parameters);
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpr)
            => binaryExpr.NodeType switch
            {
                ExpressionType.Add => Expression.Add(Visit(binaryExpr.Left), Visit(binaryExpr.Right)),
                ExpressionType.Subtract => Expression.Subtract(Visit(binaryExpr.Left), Visit(binaryExpr.Right)),

                ExpressionType.Multiply => (binaryExpr.Left, binaryExpr.Right) switch
                {
                    (ConstantExpression _, ConstantExpression _) => _zero,
                    (ConstantExpression constant, ParameterExpression _) => constant,
                    (ParameterExpression _, ConstantExpression constant) => constant,
                    _ => Expression.Add(Expression.Multiply(Visit(binaryExpr.Left), binaryExpr.Right), Expression.Multiply(binaryExpr.Left, Visit(binaryExpr.Right)))
                },

                ExpressionType.Divide => (binaryExpr.Left, binaryExpr.Right) switch
                {
                    (ConstantExpression _, ConstantExpression _) => _zero,
                    (ConstantExpression constant, ParameterExpression parameter) => Expression.Divide(constant, Expression.Multiply(parameter, parameter)),
                    (ParameterExpression _, ConstantExpression constant) => Expression.Divide(_one, constant),
                    _ => Expression.Divide(Expression.Subtract(Expression.Multiply(Visit(binaryExpr.Left), binaryExpr.Right), Expression.Multiply(binaryExpr.Left, Visit(binaryExpr.Right))), Expression.Multiply(binaryExpr.Right, binaryExpr.Right))
                },
            };

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
            => (methodCall.Arguments[0], methodCall.Arguments[1]) switch
            {
                (ConstantExpression constant, ParameterExpression _) => Expression.Multiply(methodCall, Expression.Call(null, _log, constant)),
                (ParameterExpression param, ConstantExpression constant) => Expression.Multiply(constant, Expression.Call(null, _pow, param, Expression.Constant((double)constant.Value - 1, typeof(double)))),
                (ConstantExpression constant, Expression expression) => Expression.Multiply(Expression.Multiply(Visit(expression), methodCall), Expression.Call(null, _log, constant)),
            };

        protected override Expression VisitConstant(ConstantExpression _) => _zero;

        protected override Expression VisitParameter(ParameterExpression b) => _one;
    }
}
