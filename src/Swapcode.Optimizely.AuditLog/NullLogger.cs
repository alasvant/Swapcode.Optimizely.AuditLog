using System;
using Microsoft.Extensions.Logging;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// Internal null logger instance.
    /// </summary>
    internal sealed class NullLogger : ILogger
    {
        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state)
        {
            return new DummyDisposable();
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // do nothing
        }

        /// <summary>
        /// Just a fake class to be used, if somemethinig calls the BeginScope on the NullLogger instance.
        /// </summary>
        internal sealed class DummyDisposable : IDisposable
        {
            /// <inheritdoc/>
            public void Dispose()
            {
                // do nothing
            }
        }
    }
}
