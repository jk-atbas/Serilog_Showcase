using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SerilogShowcase.CustomSinks;
using SerilogShowcase.Logging;
using SerilogShowcase.Redaction;

namespace SerilogShowcase.Configuration;

// ============================================================================
// SERILOG SETUP: Central Setup
// ============================================================================
// There are generally two established ways:
//
// 1. Fluent API – more control, compile-time-safety
// 2. appsettings.json (Config-based) – modifiable during runtime, operations-friendly.
//
// It is generally recommended to combine both:
// Base configuration in appsettings
// Custom Sinks and Enrichers in code
// ============================================================================

public static class SerilogSetup
{
	/// <summary>
	/// Configures Serilog as the host logging provider
	/// Replaces Microsoft.Extensions.Logging with Serilog
	/// however <see cref="ILogger{TCategoryName}"/> is still injectable via DI.
	/// </summary>
	public static IHostBuilder ConfigureSerilog(this IHostBuilder hostBuilder)
	{
		return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
		{
			loggerConfiguration
				// Basis: Load from appsettings.json
				// MinimumLevel, Overrides, WriteTo (Console, File) are sourced from there
				.ReadFrom.Configuration(context.Configuration)

				// Use DI registered services
				.ReadFrom.Services(services)

				// Custom Enrichers
				// Registered additionally from the appsettings Settings
				.Enrich.With<CorrelationIdEnricher>()
				.Enrich.With<AssemblyVersionEnricher>()

				// Redaction / Destructuring
				// Determines how objects are logged when annotated with {@Destructure}
				.Destructure.With<SensitiveDataDestructuringPolicy>()

				// Limits max Destructure-Depth
				.Destructure.ToMaximumDepth(4)
				.Destructure.ToMaximumStringLength(200)
				.Destructure.ToMaximumCollectionCount(10)

				// Custom Sinks (custom code-based sinks cannot be set in appsettings)
				.WriteTo.InMemory(minimumLevel: LogEventLevel.Debug)
				.WriteTo.Batching(batchSize: 10, flushInterval: TimeSpan.FromSeconds(3))
				.WriteTo.AsyncDatabase(
					context.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
					25,
					TimeSpan.FromSeconds(3));

			// Async File Sink example
			// .WriteTo.Async(a => a.File("logs/serilog-showcase-.log", rollingInterval: RollingInterval.Day))

			// Conditional Sink: Only in use for debugging
			if (context.HostingEnvironment.IsDevelopment())
			{
				loggerConfiguration.WriteTo.Debug();
			}
		});
	}

	/// <summary>
	/// Registers additional services that are required for logging
	/// </summary>
	public static IServiceCollection AddLoggingServices(this IServiceCollection services)
	{
		// InMemorySink could be a callable service for health api endpoints
		// ...
		return services;
	}
}
