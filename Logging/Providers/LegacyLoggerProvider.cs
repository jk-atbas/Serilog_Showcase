using Microsoft.Extensions.Logging;
using SerilogShowcase.Logging.Legacy;

namespace SerilogShowcase.Logging.Providers;

// ============================================================================
// Migration-Step 1: Hiding the legacy logger behind a ILoggerProvider
// ============================================================================
// This exemplifies how a migration could look like.
// The existing logging infrastructure is hidden behind a custom ILoggerProvider implementation.
// That allows for services registered in the DI to get a ILogger<T> injected into their ctor.
//
// When Serilog is introduced the legacy logging registration can be safely removed from DI
// and everything should work as-is.
// ============================================================================

/// <summary>
/// Adapter that relays the ILogger calls to the legacy logger
/// </summary>
public class LegacyLoggerAdapter(ILegacyLogger legacyLogger, string categoryName) : ILogger
{
	private readonly ILegacyLogger legacyLogger = legacyLogger;
	private readonly string categoryName = categoryName;

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return NullScope.Instance;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel != LogLevel.None;
	}

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		var message = formatter(state, exception);

		switch (logLevel)
		{
			case LogLevel.Debug:
			case LogLevel.Trace:
				legacyLogger.LogDebug(message, categoryName);
				break;
			case LogLevel.Information:
				legacyLogger.LogInfo(message, categoryName);
				break;
			case LogLevel.Warning:
				legacyLogger.LogWarning(message, categoryName);
				break;
			case LogLevel.Error:
			case LogLevel.Critical:
				legacyLogger.LogError(message, exception, categoryName);
				break;
		}
	}

	private class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new();
		public void Dispose() { }
	}
}

/// <summary>
/// Provider for the legacy logger adapter in the DI
/// </summary>
[ProviderAlias("Legacy")] // Allows configuration of "Legacy" in appsettings
public class LegacyLoggerProvider(ILegacyLogger legacyLogger) : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName)
	{
		return new LegacyLoggerAdapter(legacyLogger, categoryName);
	}

	public void Dispose() { }
}
