using Serilog.Core;
using Serilog.Events;

namespace SerilogShowcase.Logging;

// ============================================================================
// CUSTOM ENRICHER: Attach context automatically to each log event
// ============================================================================
// Enrichers represent a core-concept of Serilog. They add context to every log event which are in effect
// more or less just additional properties without knowing the calling logic.
//
// Typical use-cases are:
// - CorrelationId for request-tracing in ASP.NET Core Apis
// - Tenant-Id in multi-tenant-apps
// - Build version / Deployment infos
// - Username in a security context
// ============================================================================

/// <summary>
/// Attaches a CorrelationId to every log event.
/// In a production environment they are typically supplied through an http header or AsyncLocal storage.
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
	private static readonly AsyncLocal<string?> CorrelationId = new();

	public static string SetCorrelationId(string? id = null)
	{
		var idValue = id ?? Guid.NewGuid().ToString("N")[..12];
		CorrelationIdEnricher.CorrelationId.Value = idValue;
		return idValue;
	}

	public static string? GetCorrelationId()
	{
		return CorrelationId.Value;
	}

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		var id = CorrelationIdEnricher.CorrelationId.Value ?? "no-correlation";

		logEvent.AddPropertyIfAbsent(
			propertyFactory.CreateProperty("CorrelationId", id));
	}
}

/// <summary>
/// Attaches the current assembly version to every log event.
/// Could be useful to know in which exact assembly version an error occured
/// </summary>
public class AssemblyVersionEnricher : ILogEventEnricher
{
	private readonly string version = typeof(AssemblyVersionEnricher).Assembly
		.GetName().Version?.ToString() ?? "unknown";

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		logEvent.AddPropertyIfAbsent(
			propertyFactory.CreateProperty("AssemblyVersion", version));
	}
}
