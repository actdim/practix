using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    // target - source/value/instance
    public static class ObjectExtensions
    {
        #region IsNull

        /// <summary>
        /// Determines if the object is null
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if it is null, false otherwise</returns>
        public static bool IsNull(this object obj)
        {
            // obj == null
            return ReferenceEquals(obj, null) || Convert.IsDBNull(obj);
            // obj.GetType() == typeof(DBNull)
            // obj == DBNull.Value
        }

        #endregion

        /// <summary>
        /// Turns a single item into an enumerable
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="item">Item</param>
        /// <returns>Enumerable containing the single item</returns>
        internal static IEnumerable<T> Enumerate<T>(this T item) //EnumerateOne
        {
            yield return item;
        }

        /// <summary>
        /// Gets the safe string representation of an object which is the ToString() result for non-null objects and String.Empty otherwise
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public static string ToStringSafe(this object obj)
        {

            return (obj != null && obj != DBNull.Value ? obj.ToString() : string.Empty);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string NameOf(this object target, Expression<Func<object>> expression) //source/instance
        {
            return NameHelper.NameOf(expression);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string NameOf<T>(this object target, Expression<Func<T>> expression) //source/instance
        {
            return NameHelper.NameOf(expression);
        }

        // QUOTENAME
        // http://msdn.microsoft.com/en-us/library/ms176114.aspx
        // SqlCeCommand.Parameters Property
        // http://msdn.microsoft.com/en-us/library/system.data.sqlserverce.sqlcecommand.parameters.aspx
        // SQLify
        // Embedded single-quotes and backslashes are not properly escaped
        // Nullable?
        // AsSqlLiteral
        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToSqlLiteral(this object value) //for SQL statements
        {
            if (value == null)
            {
                return "NULL";
            }
            else
            {
                if (value is Int16 || value is Int32 || value is Int64 || value is Double || value is Single || value is Byte || value is Decimal) //|| value is Char
                {
                    return value.ToString();
                }
                else
                {
                    // quote
                    return string.Format("'{0}'", value.ToString().Replace("'", "''"));
                }

            }
        }

        // AsSqlIdentifier
        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToSqlIdentifier(this object value) //for SQL statements
        {
            return string.Format("[{0}]", value); //"\"{0}\""
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MethodBase GetCurrentMethod(this object value)
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            var method = sf.GetMethod();
            if ("Void MoveNext()".Equals(method.ToString()) &&
                method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            {
                // var baseType = value.GetType();
                // var wrappedMethod = st.GetFrames().Skip(1).Select(x => x.GetMethod()).FirstOrDefault(x => x.DeclaringType == baseType);
                // if (wrappedMethod != null)
                // {
                // 	return wrappedMethod;
                // }
                return method.GetRealMethodFromAsyncMethod();
            }
            return method;
        }


    }
}
