using System;
using System.Linq.Expressions;

namespace ActDim.Reflectron
{
    /// <summary>
    /// Provides fast reflection-based member access and mutation for an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public interface IReflectron<T> where T : class
    {
        /// <summary>
        /// Gets or sets a member value by its string name.
        /// </summary>
        /// <param name="name">The property or field name.</param>
        /// <returns>The member value.</returns>
        object this[string name] { get; set; }

        /// <summary>
        /// Gets the value of the member (property or field) specified by its string name.
        /// </summary>
        /// <typeparam name="TMember">The expected member return type.</typeparam>
        /// <param name="name">The property or field name.</param>
        /// <returns>The member value.</returns>
        TMember Get<TMember>(string name);

        /// <summary>
        /// Gets the value of the member (property or field) specified by an expression.
        /// </summary>
        /// <typeparam name="TMember">The expected member return type.</typeparam>
        /// <param name="memberExpr">The expression selecting the property or field.</param>
        /// <returns>The member value.</returns>
        TMember Get<TMember>(Expression<Func<T, TMember>> memberExpr);

        /// <summary>
        /// Sets the value of the member (property or field) specified by its string name and returns the assigned value.
        /// </summary>
        /// <typeparam name="TMember">The member value type.</typeparam>
        /// <param name="name">The property or field name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The assigned value.</returns>
        TMember Set<TMember>(string name, TMember value);

        /// <summary>
        /// Sets the value of the member (property or field) specified by an expression and returns the assigned value.
        /// </summary>
        /// <typeparam name="TMember">The member value type.</typeparam>
        /// <param name="memberExpr">The expression selecting the property or field.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The assigned value.</returns>
        TMember Set<TMember>(Expression<Func<T, TMember>> memberExpr, TMember value);

        /// <summary>
        /// Gets a compiled method invoker delegate for the specified method name.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method signature.</typeparam>
        /// <param name="name">The method name.</param>
        /// <returns>A compiled method caller delegate.</returns>
        TDelegate GetMethod<TDelegate>(string name);

        /// <summary>
        /// Gets a compiled method invoker delegate for the void method specified by an expression.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method signature.</typeparam>
        /// <param name="methodExpr">The expression selecting or calling the method.</param>
        /// <returns>A compiled method caller delegate.</returns>
        TDelegate GetMethod<TDelegate>(Expression<Action<T>> methodExpr);

        /// <summary>
        /// Gets a compiled method invoker delegate for the method returning a value specified by an expression.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method signature.</typeparam>
        /// <typeparam name="TResult">The method return type.</typeparam>
        /// <param name="methodExpr">The expression selecting or calling the method.</param>
        /// <returns>A compiled method caller delegate.</returns>
        TDelegate GetMethod<TDelegate, TResult>(Expression<Func<T, TResult>> methodExpr);

        /// <summary>
        /// Gets a compiled method invoker delegate for the method specified by a lambda expression.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type matching the method signature.</typeparam>
        /// <param name="methodExpr">The lambda expression selecting or calling the method.</param>
        /// <returns>A compiled method caller delegate.</returns>
        TDelegate GetMethod<TDelegate>(LambdaExpression methodExpr);
    }
}
