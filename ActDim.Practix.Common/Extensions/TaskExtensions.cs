using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Task"/> and <see cref="Task{TResult}"/> operations, continuations, and timeouts.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Executes a continuation action only if the predecessor task completes successfully, propagating Faulted or Canceled states.
        /// </summary>
        /// <param name="predecessor">The predecessor task.</param>
        /// <param name="continuationAction">The action to execute on successful completion.</param>
        /// <returns>A task representing the state-propagated operation.</returns>
        public static Task StatePropagatingContinueWith(this Task predecessor, Action<Task> continuationAction)
        {
            var taskSource = new TaskCompletionSource<object>();

            predecessor.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    taskSource.SetCanceled();
                    return;
                }

                if (t.IsFaulted)
                {
                    taskSource.SetException(t.Exception.InnerExceptions);
                    return;
                }

                try
                {
                    continuationAction(predecessor);
                    taskSource.SetResult(null);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            });

            return taskSource.Task;
        }

        /// <summary>
        /// Executes a typed continuation action only if the predecessor task completes successfully, propagating Faulted or Canceled states.
        /// </summary>
        /// <typeparam name="T">The result type of the predecessor task.</typeparam>
        /// <param name="predecessor">The predecessor task.</param>
        /// <param name="continuationAction">The action to execute on successful completion.</param>
        /// <returns>A task representing the state-propagated operation.</returns>
        public static Task StatePropagatingContinueWith<T>(this Task<T> predecessor, Action<Task<T>> continuationAction)
        {
            var taskSource = new TaskCompletionSource<T>();

            predecessor.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    taskSource.SetCanceled();
                    return;
                }

                if (t.IsFaulted)
                {
                    taskSource.SetException(t.Exception.InnerExceptions);
                    return;
                }

                try
                {
                    continuationAction(predecessor);
                    taskSource.SetResult(t.Result);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            });

            return taskSource.Task;
        }

        /// <summary>
        /// Converts an iterator sequence of tasks into a single task that evaluates the sequence asynchronously step-by-step.
        /// </summary>
        /// <typeparam name="TResult">The expected final result type.</typeparam>
        /// <param name="tasks">The sequence of tasks.</param>
        /// <returns>A task representing the completed iterator sequence.</returns>
        public static Task<TResult> ToTask<TResult>(this IEnumerable<Task> tasks)
        {
            var taskScheduler = SynchronizationContext.Current == null
                ? TaskScheduler.Default
                : TaskScheduler.FromCurrentSynchronizationContext();
            var taskEnumerator = tasks.GetEnumerator();
            var completionSource = new TaskCompletionSource<TResult>();

            ToTaskDoOneStep(taskEnumerator, taskScheduler, completionSource, null);
            return completionSource.Task;
        }

        private static void ToTaskDoOneStep<TResult>(
            IEnumerator<Task> taskEnumerator,
            TaskScheduler taskScheduler,
            TaskCompletionSource<TResult> completionSource,
            Task completedTask)
        {
            try
            {
                TaskStatus status;
                if (completedTask == null)
                {
                    // This is the first task from the iterator; skip status check.
                }
                else if ((status = completedTask.Status) == TaskStatus.Canceled)
                {
                    taskEnumerator.Dispose();
                    completionSource.SetCanceled();
                    return;
                }
                else if (status == TaskStatus.Faulted)
                {
                    taskEnumerator.Dispose();
                    completionSource.SetException(completedTask.Exception.InnerExceptions);
                    return;
                }
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
                return;
            }

            bool haveMore;
            try
            {
                haveMore = taskEnumerator.MoveNext();
            }
            catch (OperationCanceledException)
            {
                completionSource.SetCanceled();
                return;
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
                return;
            }

            if (!haveMore)
            {
                if (typeof(TResult) == typeof(VoidResult))
                {
                    completionSource.SetResult(default(TResult));
                }
                else if (!(completedTask is Task<TResult>))
                {
                    completionSource.SetException(new InvalidOperationException(
                        "Asynchronous iterator " + taskEnumerator +
                        " requires a final result task of type " + typeof(Task<TResult>).FullName +
                        (completedTask == null ? ", but none was provided." :
                            "; the actual task type was " + completedTask.GetType().FullName)));
                }
                else
                {
                    completionSource.SetResult(((Task<TResult>)completedTask).Result);
                }
            }
            else
            {
                taskEnumerator.Current.ContinueWith(
                    nextTask => ToTaskDoOneStep(taskEnumerator, taskScheduler, completionSource, nextTask),
                    taskScheduler);
            }
        }

        private abstract class VoidResult { }

        internal struct VoidTypeStruct { }

        /// <summary>
        /// Converts a sequence of untyped tasks into a single completion task.
        /// </summary>
        /// <param name="tasks">The task sequence.</param>
        /// <returns>A task representing the iterator sequence completion.</returns>
        public static Task ToTask(this IEnumerable<Task> tasks)
        {
            return ToTask<VoidResult>(tasks);
        }

        /// <summary>
        /// Returns a task that fails with a <see cref="TimeoutException"/> if the target task does not complete within the specified timeout.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="task">The target task.</param>
        /// <param name="millisecondsTimeout">The timeout duration in milliseconds.</param>
        /// <returns>A task that completes with the target result or throws <see cref="TimeoutException"/>.</returns>
        public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout)
        {
            return TimeoutAfter<TResult>((Task)task, millisecondsTimeout);
        }

        /// <summary>
        /// Returns a task that fails with a <see cref="TimeoutException"/> if the target task does not complete within the specified timeout.
        /// </summary>
        /// <param name="task">The target task.</param>
        /// <param name="millisecondsTimeout">The timeout duration in milliseconds.</param>
        /// <returns>A task that completes or throws <see cref="TimeoutException"/>.</returns>
        public static Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            return TimeoutAfter<VoidTypeStruct>(task, millisecondsTimeout);
        }

        private static Task<TResult> TimeoutAfter<TResult>(Task task, int millisecondsTimeout)
        {
            var tcs = new TaskCompletionSource<TResult>();

            if (task.IsCompleted || (millisecondsTimeout == Timeout.Infinite))
            {
                MarshalTaskResults(task, tcs);
                return tcs.Task;
            }

            if (millisecondsTimeout == 0)
            {
                tcs.SetException(new TimeoutException());
                return tcs.Task;
            }

            var timer = new Timer(state =>
            {
                var taskCompletionSource = (TaskCompletionSource<TResult>)state;
                taskCompletionSource.TrySetException(new TimeoutException());
            }, tcs, millisecondsTimeout, Timeout.Infinite);

            task.ContinueWith(antecedent =>
            {
                timer.Dispose();
                MarshalTaskResults(antecedent, tcs);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

            return tcs.Task;
        }

        internal static void MarshalTaskResults<TResult>(Task source, TaskCompletionSource<TResult> proxy)
        {
            switch (source.Status)
            {
                case TaskStatus.Faulted:
                    proxy.TrySetException(source.Exception);
                    break;
                case TaskStatus.Canceled:
                    proxy.TrySetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    var castedSource = source as Task<TResult>;
                    proxy.TrySetResult(
                        castedSource == null ? default(TResult) : castedSource.Result);
                    break;
            }
        }
    }
}
