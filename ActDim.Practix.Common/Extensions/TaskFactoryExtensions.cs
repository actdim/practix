using System;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="TaskFactory"/> and <see cref="TaskFactory{TResult}"/>.
    /// </summary>
    /// <remarks>
    /// Reference implementations based on ParallelExtensionsExtras project and https://gist.github.com/dgrunwald/1961087
    /// </remarks>
    public static partial class TaskFactoryExtensions
    {
        /// <summary>
        /// Synchronously executes an asynchronous function on the specified <see cref="TaskFactory"/> and returns its result.
        /// </summary>
        /// <typeparam name="TResult">The result type of the asynchronous function.</typeparam>
        /// <param name="factory">The task factory.</param>
        /// <param name="func">The asynchronous function delegate.</param>
        /// <returns>The result produced by the asynchronous function.</returns>
        public static TResult RunSync<TResult>(this TaskFactory factory, Func<Task<TResult>> func)
        {
            return factory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Synchronously executes an asynchronous void function on the specified <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="factory">The task factory.</param>
        /// <param name="func">The asynchronous function delegate.</param>
        public static void RunSync(this TaskFactory factory, Func<Task> func)
        {
            factory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Creates a continuation Task that will complete upon the completion of all provided Tasks.
        /// </summary>
        /// <param name="factory">The TaskFactory to use to create the continuation task.</param>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <returns>A task that, when completed, will return the array of completed tasks.</returns>
        public static Task<Task[]> WhenAll(this TaskFactory factory, params Task[] tasks)
        {
            return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
        }

        /// <summary>
        /// Creates a continuation Task that will complete upon the completion of all provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The result type of the antecedent tasks.</typeparam>
        /// <param name="factory">The TaskFactory to use to create the continuation task.</param>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <returns>A task that, when completed, will return the array of completed tasks.</returns>
        public static Task<Task<TAntecedentResult>[]> WhenAll<TAntecedentResult>(
            this TaskFactory factory, params Task<TAntecedentResult>[] tasks)
        {
            return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
        }

        /// <summary>
        /// Creates a continuation Task that will complete upon the completion of any one of a set of provided Tasks.
        /// </summary>
        /// <param name="factory">The TaskFactory to use to create the continuation task.</param>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <returns>A task that, when completed, will return the completed task.</returns>
        public static Task<Task> WhenAny(this TaskFactory factory, params Task[] tasks)
        {
            return factory.ContinueWhenAny(tasks, completedTask => completedTask);
        }

        /// <summary>
        /// Creates a continuation Task that will complete upon the completion of any one of a set of provided Tasks.
        /// </summary>
        /// <typeparam name="TAntecedentResult">The result type of the antecedent tasks.</typeparam>
        /// <param name="factory">The TaskFactory to use to create the continuation task.</param>
        /// <param name="tasks">The array of tasks from which to continue.</param>
        /// <returns>A task that, when completed, will return the completed task.</returns>
        public static Task<Task<TAntecedentResult>> WhenAny<TAntecedentResult>(
            this TaskFactory factory, params Task<TAntecedentResult>[] tasks)
        {
            return factory.ContinueWhenAny(tasks, completedTask => completedTask);
        }
    }
}
