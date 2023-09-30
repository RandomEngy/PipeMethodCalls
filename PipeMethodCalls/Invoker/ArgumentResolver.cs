using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;
using System.Linq;

namespace PipeMethodCalls
{
	public static class ArgumentResolver
	{
		public static object GetValue(Expression expression)
		{
			switch (expression)
			{
				case null:
					return null;
				case ConstantExpression constantExpression:
					return constantExpression.Value;
				case MemberExpression memberExpression:
					return GetValue(memberExpression);
				case MethodCallExpression methodCallExpression:
					return GetValue(methodCallExpression);
			}

			var lambdaExpression = Expression.Lambda(expression);
			var @delegate = lambdaExpression.Compile();
			return @delegate.DynamicInvoke();
		}

		private static object GetValue(MemberExpression memberExpression)
		{
			var value = GetValue(memberExpression.Expression);

			var member = memberExpression.Member;
			switch (member)
			{
				case FieldInfo fieldInfo:
					return fieldInfo.GetValue(value);
				case PropertyInfo propertyInfo:
					try
					{
						return propertyInfo.GetValue(value);
					}
					catch (TargetInvocationException e)
					{
						throw e.InnerException;
					}
				default:
					throw new Exception("Unknown member type: " + member.GetType());
			}
		}

		private static object GetValue(MethodCallExpression methodCallExpression)
		{
			var paras = GetArray(methodCallExpression.Arguments);
			var obj = GetValue(methodCallExpression.Object);

			try
			{
				return methodCallExpression.Method.Invoke(obj, paras);
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
		}

		private static object[] GetArray(IEnumerable<Expression> expressions)
		{
			return expressions.Select(GetValue).ToArray();
		}
	}
}