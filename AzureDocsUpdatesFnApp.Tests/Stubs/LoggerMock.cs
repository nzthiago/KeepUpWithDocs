using System;
using Microsoft.Extensions.Logging;

namespace AzureDocsUpdatesFnApp.Tests.Stubs
{
    public class LoggerMock : ILogger
    {
        public class NullOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NullOpDisposable();
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {            
        }
    }
}
