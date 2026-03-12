using System;
using System.Threading.Tasks;

namespace Privy.Utils
{
    /// <summary>
    /// Common helpers for safely firing and forgetting asynchronous tasks.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        /// Executes a task without awaiting it. Any exception thrown by the task
        /// will be caught and optionally passed to <paramref name="onException"/>.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="onException">Optional callback invoked if the task faults.</param>
        internal static async void SafeFireAndForget(this Task task, Action<Exception> onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }
}
