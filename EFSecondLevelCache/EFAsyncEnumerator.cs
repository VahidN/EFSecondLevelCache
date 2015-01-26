#if !NET40
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Asynchronous version of the IEnumerator of T interface that allows elements to be retrieved asynchronously.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EFAsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        /// <summary>
        /// Asynchronous version of the IEnumerator of T interface that allows elements to be retrieved asynchronously.
        /// </summary>
        /// <param name="inner">The inner IEnumerator</param>
        public EFAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _inner.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element in the sequence, returning the result asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the sequence.</returns>
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }

        /// <summary>
        /// Gets the current element in the iteration.
        /// </summary>
        public T Current
        {
            get { return _inner.Current; }
        }

        /// <summary>
        /// Gets the current element in the iteration.
        /// </summary>
        object IDbAsyncEnumerator.Current
        {
            get { return Current; }
        }
    }
}
#endif