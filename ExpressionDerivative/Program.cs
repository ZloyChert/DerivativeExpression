using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace ExpressionDerivative
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<double, double>> function = x => (x * x + 3 + 8 * x) / (5 * x * x - Math.Pow(5, x));
            //var a = new PatterntMatchingDerivate().ParseDerivative(function);
            //var a = new CustomDerivativeExpressionTreeVisitor().Visit(function);
            var a = new BuildinExpressionTreeVisitor().GetDerivative(function);
            Func<double, double> compiledExpression = a.Compile();
            double result = compiledExpression(2);
            Console.WriteLine(result);
        }
    }
}
