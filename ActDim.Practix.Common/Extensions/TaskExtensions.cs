using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Will only execute the given continuationAction, if the given predecessor task completes successfully.
        /// The states Faulted and Canceled are propagated to the returned task.
        /// </summary>
        /// <param name="predecessor">Predecessor task.</param>
        /// <param name="continuationAction">Action to Execute, if predecessor completes successfully</param>
        /// <returns></returns>
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

        // http://www.codeproject.com/Articles/504197/Await-Tasks-in-Csharp-using-Iterators

        public static Task<TResult> ToTask<TResult>(this IEnumerable<Task> tasks)
        {
            var taskScheduler =
                SynchronizationContext.Current == null
                    ? TaskScheduler.Default : TaskScheduler.FromCurrentSynchronizationContext();
            var taskEnumerator = tasks.GetEnumerator();
            var completionSource = new TaskCompletionSource<TResult>();

            ToTaskDoOneStep(taskEnumerator, taskScheduler, completionSource, null);
            return completionSource.Task;
        }

        private static void ToTaskDoOneStep<TResult>(
            IEnumerator<Task> taskEnumerator, TaskScheduler taskScheduler,
            TaskCompletionSource<TResult> completionSource, Task completedTask)
        {
            try
            {
                // Check status of previous nested task (if any), and stop if Canceled or Faulted.
                // In these cases, we are abandoning the enumerator, so we must dispose it.
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
                // Return exception from disposing the enumerator.
                completionSource.SetException(ex);
                return;
            }

            // Find the next Task in the iterator; handle cancellation and other exceptions.
            Boolean haveMore;
            try
            {
                // Enumerator disposes itself if it throws an exception or completes (returns false).
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
                // No more tasks; set the result (if any) from the last completed task (if any).
                // We know it's not Canceled or Faulted because we checked at the start of this method.
                if (typeof(TResult) == typeof(VoidResult))
                {
                    // No result
                    completionSource.SetResult(default(TResult));

                }
                else if (!(completedTask is Task<TResult>))
                {     // Wrong result
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
                // When the nested task completes, continue by performing this function again.
                taskEnumerator.Current.ContinueWith(
                    nextTask => ToTaskDoOneStep(taskEnumerator, taskScheduler, completionSource, nextTask),
                    taskScheduler);
            }
        }

        private abstract class VoidResult { }

        public static Task ToTask(this IEnumerable<Task> tasks)
        {
            return ToTask<VoidResult>(tasks);
        }

        // public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        // {
        //     var tcs = new TaskCompletionSource<bool>();
        //     using (cancellationToken.Register(
        //                 s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        //         if (task != await Task.WhenAny(task, tcs.Task))
        //         {
        //             throw new OperationCanceledException(cancellationToken);
        //         }
        //     return await task;
        // }

        // public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        // {
        //     // var completedTask = Task.Factory.WhenAny(task, Delay(timeout));
        //     var completedTask = Task.Factory.ContinueWhenAny(new [] { task, Delay(timeout) },  _ => _);           
        //     return completedTask.ContinueWith(_ =>
        //     {
        //         if (_ != task)
        //         {
        //             throw new TimeoutException("The operation has timed out.");
        //         }
        //         return ((Task<TResult>)_).Result;
        //     });
        // }

        // public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        // {
        //     if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
        //     {
        //         await task;
        //     }
        //     else
        //     {
        //         throw new TimeoutException();
        //     }
        // }

        // The following based on http://blogs.msdn.com/b/pfxteam/archive/2011/11/10/10235834.aspx

        internal struct VoidTypeStruct { }  // See Footnote #1

        public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout)
        {
            return TimeoutAfter<TResult>((Task)task, millisecondsTimeout);
        }

        public static Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            return TimeoutAfter<VoidTypeStruct>(task, millisecondsTimeout);
        }

        private static Task<TResult> TimeoutAfter<TResult>(Task task, int millisecondsTimeout)
        {
            // tcs.Task will be returned as a proxy to the caller
            var tcs = new TaskCompletionSource<TResult>();

            // Short-circuit #1: infinite timeout or task already completed
            if (task.IsCompleted || (millisecondsTimeout == Timeout.Infinite))
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                MarshalTaskResults(task, tcs);
                return tcs.Task;
            }

            // Short-circuit #2: zero timeout
            if (millisecondsTimeout == 0)
            {
                // We've already timed out.
                tcs.SetException(new TimeoutException());
                return tcs.Task;
            }

            // Set up a timer to complete after the specified timeout period
            var timer = new Timer(state =>
            {
                // Recover your state information
                var taskCompletionSource = (TaskCompletionSource<TResult>)state;

                // Fault our proxy with a TimeoutException
                taskCompletionSource.TrySetException(new TimeoutException());

            }, tcs, millisecondsTimeout, Timeout.Infinite);

            // var ctx = Tuple.Create(timer, tcs); //Tuple<Timer, TaskCompletionSource<TResult>>

            // Wire up the logic for what happens when source task completes
            task.ContinueWith(antecedent =>
            {
                // Recover our state data

                // Cancel the Timer
                // ctx.Item1.Dispose();
                timer.Dispose();

                // Marshal results to proxy
                MarshalTaskResults(antecedent, tcs);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default); // TaskScheduler.Current

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
                        castedSource == null ? default(TResult) : // source is a Task
                            castedSource.Result); // source is a Task<TResult>
                    break;
            }
        }

        // public static Task Delay(int milliseconds)
        // {
        // 	return Delay((long)milliseconds);
        // }

        // // Asynchronous NON-BLOCKING method
        // public static Task Delay(long milliseconds)
        // {
        // 	//var tcs = new TaskCompletionSource<object>();
        // 	var tcs = new TaskCompletionSource<VoidTypeStruct>();
        // 	//new Timer(_ => tcs.SetResult(null)).Change(milliseconds, -1); //TrySetResult
        // 	new Timer(_ => tcs.SetResult(default(VoidTypeStruct))).Change(milliseconds, -1); //TrySetResult
        // 	return tcs.Task;
        // }

        // public static Task Delay(TimeSpan timeSpan)
        // {
        // 	return Delay((long)timeSpan.TotalMilliseconds);
        // }
    }
}
