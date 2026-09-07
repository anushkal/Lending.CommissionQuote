using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CommissionQuote.Service.Tests;

/// <summary>
/// Captures every log entry emitted during a request so tests can assert the constitution-
/// mandated level (Information/Warning/Error) for each quote outcome, rather than only the HTTP
/// response.
/// </summary>
public sealed class ListLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new ListLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    public sealed record LogEntry(string CategoryName, LogLevel Level, string Message);

    private sealed class ListLogger(string categoryName, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(categoryName, logLevel, formatter(state, exception)));
        }
    }
}
