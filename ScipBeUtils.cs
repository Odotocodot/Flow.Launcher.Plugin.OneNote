// MIT License

// Copyright (c) 2017 Stefan Cruysberghs

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;

namespace Flow.Launcher.Plugin.OneNote.ScipBeUtils
{
    /// <summary>
    /// <para>Utils from ScipBe.Common.Office.OneNote </para>
    /// </summary>
    public static class Utils
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
        public static TRet TryCatchAndRetry<TRet, TException>(Func<TRet> action, TimeSpan retryInterval, int retryCount, Action<TException> onRetry = null)
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
                        System.Threading.Thread.Sleep(retryInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal static void CallOneNoteSafely(Action<Application> action)
        {
            Application oneNote = null;
            try
            {
                oneNote = TryCatchAndRetry<Application, COMException>(
                    () => new Application(),
                    TimeSpan.FromMilliseconds(100),
                    3,
                    ex => System.Diagnostics.Trace.TraceError(ex.Message));
                action(oneNote);
            }
            finally
            {
                if (oneNote != null)
                {
                    Marshal.ReleaseComObject(oneNote);
                }
            }
        }
        internal static T CallOneNoteSafely<T>(Func<Application, T> action)
        {
            Application oneNote = null;
            try
            {
                oneNote = TryCatchAndRetry<Application, COMException>(
                    () => new Application(),
                    TimeSpan.FromMilliseconds(100),
                    3,
                    ex => System.Diagnostics.Trace.TraceError(ex.Message));
                return action(oneNote);
            }
            finally
            {
                if (oneNote != null)
                {
                    Marshal.ReleaseComObject(oneNote);
                }
            }
        }
    }
}
