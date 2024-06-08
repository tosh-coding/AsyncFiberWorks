using AsyncFiberWorks.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Repeatedly invoke a driver.
    /// </summary>
    public class AsyncActionInvokerRepeatInvoker : IDisposable
    {
        private readonly IAsyncExecutor _executor;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="executor"></param>
        public AsyncActionInvokerRepeatInvoker(IAsyncExecutor executor = null)
        {
            if (executor == null)
            {
                executor = AsyncSimpleExecutor.Instance;
            }
            _executor = executor;
        }

        /// <summary>
        /// Start invocation.
        /// </summary>
        /// <param name="invoker"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Invoke(IAsyncActionInvoker invoker, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var token = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, cancellationToken);
            while (!token.IsCancellationRequested)
            {
                await _executor
                    .Execute(async () => await invoker.Invoke().ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stop invocation.
        /// </summary>
        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}
