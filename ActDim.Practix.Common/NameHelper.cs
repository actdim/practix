using System;
using System.Linq.Expressions;

namespace ActDim.Practix
{
	public class NameHelper
	{
		// MethodInfo.GetCurrentMethod().Name.Substring(4);
		// TypeManager.GetProperty<T>(x => x.Name).Name;

		// GetName/GetMemberName        
		// expression - propertyExpression/memberExpression/propertySelector/memberSelector
        public static string NameOf(Expression<Func<object>> expression)
        {
            var body = expression.Body;
            return NameOf(body);
        }

        public static string NameOf<T>(Expression<Func<T, object>> expression)
        {
            var body = expression.Body;
            return NameOf(body);
        }

        public static string NameOf<TParameter, TResult>(Expression<Func<TParameter, TResult>> expression)
		{
            var body = expression.Body;
            return NameOf(body);
        }

		public static string NameOf(Expression expression)
		{
			MemberExpression memberExpression = null;
			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess: // is MemberExpression
					memberExpression = (MemberExpression)expression; // as MemberExpression
					break;
				case ExpressionType.Lambda: // expression is LambdaExpression
					memberExpression = (MemberExpression)((LambdaExpression)expression).Body; // as LambdaExpression, as MemberExpression 
					break;
				// ExpressionType.ConvertChecked?
				case ExpressionType.Convert:
					// expression.Body is UnaryExpression?
					memberExpression = (MemberExpression)((UnaryExpression)expression).Operand; // as UnaryExpression, as MemberExpression
					break;
				case ExpressionType.Call:
					var callExpression = (MethodCallExpression)expression;
					return callExpression.Method.Name;
				case ExpressionType.New:
					var newExpression = (NewExpression)expression;
					return newExpression.Constructor.Name;
			}			

			// throw new ArgumentException("expression.Body must be a member or call expression.", "expression");

			if (memberExpression == null)
			{
				throw new ArgumentException("Invalid expression type.", nameof(expression)); //cannot get member expression																							 
			}
			return memberExpression.Member.Name;
			// return NameOf(memberExpression.Expression) + "." + memberExpression.Member.Name;
			// exclude (ignore) parents
			// exclude (ignore) "this"?
			// ConstantExpression constantExpression = (ConstantExpression)memberExpression.Expression; // as ConstantExpression            
			// LambdaExpression lambda = Expression.Lambda(constantExpression);
			// lambda.Compile().DynamicInvoke();
			// constantExpression.Value;
		}
	}
}
