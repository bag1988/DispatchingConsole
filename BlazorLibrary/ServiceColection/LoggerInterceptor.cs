
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BlazorLibrary.ServiceColection
{
    public class LoggerInterceptor<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public LoggerInterceptor(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger(typeof(T));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            message = $"{DateTime.Now} {message}";
            _logger.Log(logLevel, eventId, exception, message);
        }
    }
}
