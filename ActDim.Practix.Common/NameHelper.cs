using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ActDim.Practix.Common
    {
        /// <summary>
        /// Provides utilities for extracting names or full property paths from Expression trees.
        /// </summary>
    public static class NameHelper
    {
        /// <summary>
        /// Extracts the name or path of a property from a lambda expression (e.g., x => x.User.Name -> "User.Name").
        /// </summary>
        public static string NameOf<T>(Expression<Func<T, object>> expression)
        {
            return GetMemberPath(expression.Body);
        }

        /// <summary>
        /// Extracts the name or path of a property from a lambda expression (e.g., x => x.Id -> "Id").
        /// </summary>
        public static string NameOf<TParameter, TResult>(Expression<Func<TParameter, TResult>> expression)
        {
            return GetMemberPath(expression.Body);
        }

        /// <summary>
        /// Main entry point for extracting the name or path from any expression.
        /// </summary>
        public static string NameOf(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            return GetMemberPath(expression);
        }

        private static string GetMemberPath(Expression expression)
        {
            switch (expression)
            {
                case LambdaExpression lambda:
                    return GetMemberPath(lambda.Body);

                case UnaryExpression unary:
                    // Handle type casting or boxing to object
                    return GetMemberPath(unary.Operand);

                case MemberExpression member:
                    return ResolveFullMemberName(member);

                case MethodCallExpression call:
                    return call.Method.Name;

                case NewExpression @new:
                    if (@new.Constructor != null)
                    {
                        return @new.Constructor.Name;
                    }
                    else
                    {
                        return "Unknown";
                    }

                default:
                    throw new NotSupportedException($"The expression type {expression.NodeType} is not supported for name extraction.");
            }
        }

        private static string ResolveFullMemberName(MemberExpression member)
        {
            // Recursively traverse up the tree to build the full path (e.g., User.Address.City)
            if (member.Expression is MemberExpression nestedMember)
            {
                return $"{ResolveFullMemberName(nestedMember)}.{member.Member.Name}";
            }
            else
            {
                // If we reached the root, return the current member's name
                return member.Member.Name;
            }
        }
    }
}
