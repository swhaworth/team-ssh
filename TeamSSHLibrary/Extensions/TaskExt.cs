using System.Threading;
using System.Threading.Tasks;
using TeamSSHLibrary.Helpers;

namespace TeamSSHLibrary.Extensions
{
    public static class TaskExt
    {
        #region Public Methods

        public static Task ContinueWithEvent(this Task task, AutoResetEvent evt)
        {
            var tcs = new TaskCompletionSource<bool>();
            task.ContinueWith((t) => TaskExt.ContinueWithEventHandler(t, tcs, evt), TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task<T> ContinueWithEvent<T>(this Task<T> task, AutoResetEvent evt)
        {
            var tcs = new TaskCompletionSource<T>();
            task.ContinueWith((t) => TaskExt.ContinueWithEventHandler(t, tcs, evt), TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        #endregion

        #region Private Methods

        private static void ContinueWithEventHandler(Task result, TaskCompletionSource<bool> tcs, AutoResetEvent evt)
        {
            ExceptionHelpers.WrapObjectDisposedException(() => evt.Set());
            if (result.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else if (result.IsFaulted)
            {
                tcs.TrySetException(result.Exception);
            }
            else
            {
                tcs.TrySetResult(true);
            }
        }

        private static T ContinueWithEventHandler<T>(Task<T> result, TaskCompletionSource<T> tcs, AutoResetEvent evt)
        {
            ExceptionHelpers.WrapObjectDisposedException(() => evt.Set());
            if (result.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else if (result.IsFaulted)
            {
                tcs.TrySetException(result.Exception);
            }
            else
            {
                tcs.TrySetResult(result.Result);
            }
            return default(T);
        }

        #endregion
    }
}
