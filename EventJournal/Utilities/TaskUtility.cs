// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaskUtility.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal.Utilities
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="Task"/>.
    /// </summary>
    public static class TaskUtility
    {
        /// <summary>
        /// Returns a <see cref="Task{Object}"/> for the provided <paramref name="task"/>.
        /// </summary>
        /// <param name="task">
        /// The task.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        public static Task<object> Box(this Task task)
        {
            return task.ContinueWith(
                antecedent =>
                {
                    if (antecedent.Exception != null)
                    {
                        throw new AggregateException(antecedent.Exception);
                    }

                    return (object)default(string);
                }, 
                TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Returns a <see cref="Task{Object}"/> for the provided <paramref name="task{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The underlying type of <paramref name="task"/>.
        /// </typeparam>
        /// <param name="task">
        /// The task.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        public static Task<object> Box<T>(this Task<T> task)
        {
            var completion = new TaskCompletionSource<object>();
            task.ContinueWith(
                antecedent =>
                {
                    if (antecedent.Exception != null)
                    {
                        antecedent.Exception.Flatten();
                        completion.TrySetException(antecedent.Exception.InnerException);
                    }
                    else
                    {
                        completion.TrySetResult(antecedent.Result);
                    }
                }, 
                TaskContinuationOptions.ExecuteSynchronously);
            return completion.Task;
        }
    }
}
