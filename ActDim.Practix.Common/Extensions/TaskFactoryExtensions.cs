// see also ParallelExtensionsExtras project
// see also https://gist.github.com/dgrunwald/1961087

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
	public static partial class TaskFactoryExtensions
	{
		public static TResult RunSync<TResult>(this TaskFactory factory, Func<Task<TResult>> func)
		{

			return factory
			  .StartNew<Task<TResult>>(func)
			  .Unwrap<TResult>()
			  //.GetAwaiter()
			  //.GetResult()
			  .Result
			  ;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="func"></param>
		public static void RunSync(this TaskFactory factory, Func<Task> func)
		{
			factory
			  .StartNew<Task>(func)
			  .Unwrap()
			  //.GetAwaiter()
			  //.GetResult()
			  .Wait()
			  ;
		}

		/// <summary>
		/// Creates a continuation Task that will compplete upon
		/// the completion of a set of provided Tasks.
		/// </summary>
		/// <param name="factory">The TaskFactory to use to create the continuation task.</param>
		/// <param name="tasks">The array of tasks from which to continue.</param>
		/// <returns>A task that, when completed, will return the array of completed tasks.</returns>
		public static Task<Task[]> WhenAll(
			this TaskFactory factory, params Task[] tasks)
		{
			return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
		}

		/// <summary>
		/// Creates a continuation Task that will compplete upon
		/// the completion of a set of provided Tasks.
		/// </summary>
		/// <param name="factory">The TaskFactory to use to create the continuation task.</param>
		/// <param name="tasks">The array of tasks from which to continue.</param>
		/// <returns>A task that, when completed, will return the array of completed tasks.</returns>
		public static Task<Task<TAntecedentResult>[]> WhenAll<TAntecedentResult>(
			this TaskFactory factory, params Task<TAntecedentResult>[] tasks)
		{
			return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
		}

		/// <summary>
		/// Creates a continuation Task that will complete upon
		/// the completion of any one of a set of provided Tasks.
		/// </summary>
		/// <param name="factory">The TaskFactory to use to create the continuation task.</param>
		/// <param name="tasks">The array of tasks from which to continue.</param>
		/// <returns>A task that, when completed, will return the completed task.</returns>
		public static Task<Task> WhenAny(
			this TaskFactory factory, params Task[] tasks)
		{
			return factory.ContinueWhenAny(tasks, completedTask => completedTask);
		}

		/// <summary>
		/// Creates a continuation Task that will complete upon
		/// the completion of any one of a set of provided Tasks.
		/// </summary>
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
