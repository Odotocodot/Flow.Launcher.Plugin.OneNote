using System;
using System.Collections;
using System.Threading;

namespace Odotocodot.OneNote.Linq
{
    public static class Util
    {
        /// <summary>
        /// Do retry and return some value
        /// </summary>
        /// <typeparam name="TRet">Return type</typeparam>
        /// <typeparam name="TException">Type of exception to catch</typeparam>
        /// <param name="action">Action to perform</param>
        /// <param name="retryInterval">Interval between retries</param>
        /// <param name="retryCount">How many times to retry</param>
        /// <param name="onRetry">Action to call on retry</param>
        /// <returns>Action return</returns>
        internal static TRet TryCatchAndRetry<TRet, TException>(Func<TRet> action, TimeSpan retryInterval, int retryCount, Action<TException> onRetry = null)
            where TException : Exception
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return action();
                }
                catch (TException ex)
                {
                    if (attempt++ < retryCount)
                    {
                        onRetry(ex);
                        Thread.Sleep(retryInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
