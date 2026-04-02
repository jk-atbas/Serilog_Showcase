using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace SerilogShowcase.CustomSinks;

// ============================================================================
// CUSTOM SINK: Custom-Logging-Targets
// ============================================================================
// Serilog sinks are classes that implement interfaces like ILogEventSink.
// A sink receives rendered log events and decides itself what it does with them - Database, API, Queues, ....
//
// There are following examples:
// 1. A simple In-Memory sink (possibly for tests and demos)
// 2. A batching sink
// 3. Async database sink outline
// 4. Extension methods for a fluent configuration
// ============================================================================

/// <summary>
/// Simple custom sink that collects log events in-memory.
/// May be useful for unit tests
/// </summary>
public class InMemorySink(
	IFormatProvider? formatProvider = null,
	LogEventLevel minimumLevel = LogEventLevel.Information)
	: ILogEventSink
{
	private static readonly List<LogEntry> Entries = [];
	private static readonly object Lock = new();

	public void Emit(LogEvent logEvent)
	{
		if (logEvent.Level < minimumLevel)
		{
			return;
		}

		var entry = new LogEntry
		{
			Timestamp = logEvent.Timestamp,
			Level = logEvent.Level.ToString(),
			Message = logEvent.RenderMessage(formatProvider),
			Properties = logEvent.Properties
				.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()),
			Exception = logEvent.Exception?.ToString()
		};

		lock (Lock)
		{
			Entries.Add(entry);

			if (Entries.Count > 1000)
			{
				Entries.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Return all log entries
	/// </summary>
	public static IReadOnlyList<LogEntry> GetEntries()
	{
		lock (Lock)
		{
			return Entries.ToList().AsReadOnly();
		}
	}

	/// <summary>
	/// Empties the log cache
	/// </summary>
	public static void Clear()
	{
		lock (Lock)
		{
			Entries.Clear();
		}
	}
}

/// <summary>
/// Log entry representation 
/// </summary>
public record LogEntry
{
	public DateTimeOffset Timestamp { get; init; }
	public string Level { get; init; } = string.Empty;
	public string Message { get; init; } = string.Empty;
	public Dictionary<string, string> Properties { get; init; } = [];
	public string? Exception { get; init; }
}

// ============================================================================
// BATCHING SINK
// ============================================================================

/// <summary>
/// Demonstrates a sink that collects events and processes them periodically or when a threshold is met
/// Could be used for a bulk insert into a database
/// </summary>
public class BatchingSink : ILogEventSink, IDisposable
{
	private readonly int batchSize;
	private readonly List<LogEvent> buffer = [];
	private readonly object @lock = new();
	private readonly Timer timer;

	public BatchingSink(int batchSize = 50, TimeSpan? flushInterval = null)
	{
		this.batchSize = batchSize;
		var flushInterval1 = flushInterval ?? TimeSpan.FromSeconds(5);
		timer = new Timer(_ => Flush(), null, flushInterval1, flushInterval1);
	}

	public void Emit(LogEvent logEvent)
	{
		lock (@lock)
		{
			buffer.Add(logEvent);

			if (buffer.Count >= batchSize)
			{
				FlushInternal();
			}
		}
	}

	private void Flush()
	{
		lock (@lock)
		{
			FlushInternal();
		}
	}

	private void FlushInternal()
	{
		if (buffer.Count == 0)
		{
			return;
		}

		var batch = buffer.ToList();
		buffer.Clear();

		// Do something with logs...

		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.WriteLine(
			$"  [BatchingSink] Flushing {batch.Count} events " +
			$"(would send to DB/API in production)");
		Console.ResetColor();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		timer.Dispose();
		Flush();
	}
}

/// <summary>
/// Example for an async batched sink implementation
/// </summary>
/// <remarks>
/// This is just demo. For production an actual state-of-the-art database sink is to be used obviously
/// </remarks>
public class AsyncDatabaseSink(string connectionString) : Serilog.Sinks.PeriodicBatching.IBatchedLogEventSink
{
	/// <inheritdoc />
	public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
	{
		List<LogEvent> events = batch.ToList();

		// Some database operations ...
		await Task.Delay(50);

		Console.ForegroundColor = ConsoleColor.DarkMagenta;
		Console.WriteLine(
			$"  [AsyncDatabaseSink] Batch mit {events.Count} Events " +
			$"geschrieben (Connection: {connectionString[..30]}...)");

		Console.ResetColor();
	}

	/// <inheritdoc />
	public Task OnEmptyBatchAsync()
	{
		return Task.CompletedTask;
	}
}

// ============================================================================
// EXTENSION METHODS: Fluent apis for registration
// ============================================================================
// Convention: Sinks are registered through extension methods on LoggerSinkConfiguration
// That allows for the well-known .WriteTo.SomeMethod() syntax

/// <summary>
/// Extension methods for registering the custom sinks
/// </summary>
public static class CustomSinkExtensions
{
	public static LoggerConfiguration InMemory(
		this LoggerSinkConfiguration sinkConfiguration,
		LogEventLevel minimumLevel = LogEventLevel.Information,
		IFormatProvider? formatProvider = null)
	{
		return sinkConfiguration.Sink(
			new InMemorySink(formatProvider, minimumLevel));
	}

	public static LoggerConfiguration Batching(
		this LoggerSinkConfiguration sinkConfiguration,
		int batchSize = 50,
		TimeSpan? flushInterval = null)
	{
		return sinkConfiguration.Sink(
			new BatchingSink(batchSize, flushInterval));
	}

	public static LoggerConfiguration AsyncDatabase(
		this LoggerSinkConfiguration sinkConfiguration,
		string connectionString,
		int batchSizeLimit = 50,
		TimeSpan? period = null,
		int queueLimit = 10000,
		LogEventLevel minimumLevel = LogEventLevel.Information)
	{
		var sink = new AsyncDatabaseSink(connectionString);

		var batchingOptions = new PeriodicBatchingSinkOptions
		{
			BatchSizeLimit = batchSizeLimit,
			Period = period ?? TimeSpan.FromSeconds(2),
			QueueLimit = queueLimit
		};

		var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);

		return sinkConfiguration.Sink(batchingSink, minimumLevel);
	}
}
