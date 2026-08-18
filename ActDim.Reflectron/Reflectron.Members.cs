using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Reflectron
{
    public static partial class Reflectron
    {
        private static readonly Type BaseDelegateType = typeof(Delegate);
        private static readonly Type DelegateType = typeof(Delegate);
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type VoidType = typeof(void);

        /// <summary>
        /// Creates a generic <see cref="Func{TResult}"/> delegate type for the specified argument and return types.
        /// </summary>
        /// <param name="typeArgs">The type arguments for the Func delegate.</param>
        /// <returns>The constructed Func delegate type.</returns>
        public static Type GetFuncType(params Type[] typeArgs)
        {
            return Expression.GetFuncType(typeArgs);
        }

        /// <summary>
        /// Creates a generic <see cref="Action"/> delegate type for the specified argument types.
        /// </summary>
        /// <param name="typeArgs">The type arguments for the Action delegate.</param>
        /// <returns>The constructed Action delegate type.</returns>
        public static Type GetActionType(params Type[] typeArgs)
        {
            return Expression.GetActionType(typeArgs);
        }

        /// <summary>
        /// Gets a member by its expression usage.
        /// For example, <c>GetMemberInfo(() => obj.GetType())</c> will return the <see cref="MethodInfo"/> for <c>GetType</c>.
        /// </summary>
        /// <param name="expr">The lambda expression selecting a member.</param>
        /// <returns>The extracted <see cref="MemberInfo"/>.</returns>
        public static MemberInfo GetMemberInfo(LambdaExpression expr)
        {
            Guard.Against.Null(expr, nameof(expr));

            var bodyExpr = expr.Body;

            while (true)
            {
                switch (bodyExpr.NodeType)
                {
                    case ExpressionType.Convert:
                        var convertExpr = (UnaryExpression)bodyExpr;
                        bodyExpr = convertExpr.Operand;
                        continue;

                    case ExpressionType.MemberAccess:
                        var memberExpr = (MemberExpression)bodyExpr;
                        return memberExpr.Member;

                    case ExpressionType.Call:
                        var callExpr = (MethodCallExpression)bodyExpr;
                        return callExpr.Method;

                    case ExpressionType.New:
                        var newExpr = (NewExpression)bodyExpr;
                        return newExpr.Constructor;
                }

                throw new ArgumentException($"{nameof(expr)}.Body must be a member, call, or constructor expression.", nameof(expr));
            }
        }

        /// <summary>
        /// Gets the constructor info from a construction call expression.
        /// </summary>
        /// <typeparam name="T">The type being constructed.</typeparam>
        /// <param name="expr">The construction expression.</param>
        /// <returns>The extracted <see cref="ConstructorInfo"/>.</returns>
        public static ConstructorInfo GetConstructorInfo<T>(Expression<Func<T>> expr)
        {
            return (ConstructorInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a field from a sample usage expression.
        /// </summary>
        /// <typeparam name="T">The field value type.</typeparam>
        /// <param name="expr">The field access expression.</param>
        /// <returns>The extracted <see cref="FieldInfo"/>.</returns>
        public static FieldInfo GetFieldInfo<T>(Expression<Func<T>> expr)
        {
            return (FieldInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a field from a typed instance member expression.
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <typeparam name="TField">The field value type.</typeparam>
        /// <param name="expr">The field access expression.</param>
        /// <returns>The extracted <see cref="FieldInfo"/>.</returns>
        public static FieldInfo GetFieldInfo<TInstance, TField>(Expression<Func<TInstance, TField>> expr)
        {
            return (FieldInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a property from a sample usage expression.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="expr">The property access expression.</param>
        /// <returns>The extracted <see cref="PropertyInfo"/>.</returns>
        public static PropertyInfo GetPropertyInfo<T>(Expression<Func<T>> expr)
        {
            return (PropertyInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a property from a typed instance member expression.
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="expr">The property access expression.</param>
        /// <returns>The extracted <see cref="PropertyInfo"/>.</returns>
        public static PropertyInfo GetPropertyInfo<TInstance, T>(Expression<Func<TInstance, T>> expr)
        {
            return (PropertyInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a method info of a void method expression.
        /// </summary>
        /// <param name="expr">The void method call expression.</param>
        /// <returns>The extracted <see cref="MethodInfo"/>.</returns>
        public static MethodInfo GetMethodInfo(Expression<Action> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets a method info of a typed instance void method expression.
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <param name="expr">The void method call expression.</param>
        /// <returns>The extracted <see cref="MethodInfo"/>.</returns>
        public static MethodInfo GetMethodInfo<TInstance>(Expression<Action<TInstance>> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets the MethodInfo of a method that returns a value.
        /// </summary>
        /// <typeparam name="T">The return value type.</typeparam>
        /// <param name="expr">The method call expression.</param>
        /// <returns>The extracted <see cref="MethodInfo"/>.</returns>
        public static MethodInfo GetMethodInfo<T>(Expression<Func<T>> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }

        /// <summary>
        /// Gets the MethodInfo of a typed instance method that returns a value.
        /// </summary>
        /// <typeparam name="TInstance">The declaring instance type.</typeparam>
        /// <typeparam name="TOutput">The return value type.</typeparam>
        /// <param name="expr">The method call expression.</param>
        /// <returns>The extracted <see cref="MethodInfo"/>.</returns>
        public static MethodInfo GetMethodInfo<TInstance, TOutput>(Expression<Func<TInstance, TOutput>> expr)
        {
            return (MethodInfo)GetMemberInfo(expr);
        }

        private static Expression GetArgumentExpression(int index, IList<Type> methodParameterTypes, Type[] invokeParameterTypes, ParameterExpression[] paramExprs)
        {
            var invokeParameterType = invokeParameterTypes[index];
            var methodParameterType = methodParameterTypes[index];

            var paramExpr = Expression.Parameter(invokeParameterType, "P" + index);
            paramExprs[index] = paramExpr;
            if (methodParameterType == invokeParameterType)
            {
                return paramExpr;
            }

            var convertExpr = Expression.Convert(paramExpr, methodParameterType);
            return convertExpr;
        }
    }
}
