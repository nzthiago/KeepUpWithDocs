using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace AzureDocsUpdatesFnApp.Tests.Stubs
{
    public class AsyncCollectorStub<T> : IAsyncCollector<T>
    {
        public List<T> Items { get; } = new List<T>();

        public Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            Items.Add(item);
            return Task.FromResult(true);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }
    }
}
