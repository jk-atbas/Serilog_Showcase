using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using SerilogShowcase.Configuration;
using SerilogShowcase.CustomSinks;
using SerilogShowcase.Logging.Legacy;
using SerilogShowcase.Logging.Providers;
using SerilogShowcase.Models;
using SerilogShowcase.Services;
using System.Diagnostics;
using System.Text;

namespace SerilogShowcase;

// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  SERILOG SHOWCASE – Demonstration and transition						  ║
// ║                                                                          ║
// ║  Shows the path: Legacy logger -> ILogger Facade -> Serilog              ║
// ╚══════════════════════════════════════════════════════════════════════════╝

internal class Program
{
	public static void Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;

		// ============================================================================
		// PHASE 1: Legacy Logger behind ILogger Facade
		// ============================================================================
		PrintHeader("PHASE 1: Legacy Logger behind ILogger<T> Facade");
		RunWithLegacyLogger();

		Console.WriteLine();
		Console.WriteLine("  ──────────────────────────────────────────────────────────");
		Console.WriteLine("  ↑ Legacy-Output");
		Console.WriteLine("  ↓ Serilog-Output: structured, enriched, masked");
		Console.WriteLine("  ──────────────────────────────────────────────────────────");
		Console.WriteLine();

		// ============================================================================
		// PHASE 2: Serilog as Drop-in-Replacement
		// ============================================================================
		PrintHeader("PHASE 2: Serilog as ILogger-Provider");
		RunWithSerilog();

		// ============================================================================
		// PHASE 3: Advanced Features Showcase
		// ============================================================================
		PrintHeader("PHASE 3: Advanced Serilog features");
		DemonstrateAdvancedFeatures();

		// ============================================================================
		// PHASE 4: InMemorySink - Display results
		// ============================================================================
		PrintHeader("PHASE 4: InMemorySink - Collected Entries");
		ShowInMemoryEntries();

		// Shutdown Serilog safely
		Log.CloseAndFlush();

		Console.WriteLine();
		PrintHeader("DEMO COMPLETE");
		Console.WriteLine("  Check the logs/ folder for the File-Sink logs!");
	}

	// ============================================================================
	// IMPLEMENTATION
	// ============================================================================
	private static void RunWithLegacyLogger()
	{
		var host = Host.CreateDefaultBuilder()
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
			})
			.ConfigureServices(services =>
			{
				// Legacy-Logger registration
				services.AddSingleton<ILegacyLogger, LegacyLogger>();
				services.AddSingleton<ILoggerProvider, LegacyLoggerProvider>();

				services.AddTransient<OrderService>();
			})
			.Build();

		// Use service
		var orderService = host.Services.GetRequiredService<OrderService>();
		var order = CreateSampleOrder();

		Console.WriteLine("-> OrderService.ProcessOrder() with Legacy-Logger:\n");
		orderService.ProcessOrder(order);
	}

	private static void RunWithSerilog()
	{
		// Very similar setup as legacy
		var host = Host.CreateDefaultBuilder()
			// Serilog registration
			.ConfigureSerilog()
			.ConfigureServices(services =>
			{
				services.AddLoggingServices();

				services.AddTransient<OrderService>();
				services.AddTransient<UserService>();
				services.AddTransient<NoisyService>();
			})
			.Build();

		// OrderService: Structured Logging & Destructuring
		Console.WriteLine("-> OrderService with Serilog:\n");
		var orderService = host.Services.GetRequiredService<OrderService>();
		var order = CreateSampleOrder();
		orderService.ProcessOrder(order);

		Console.WriteLine();

		// OrderService: Exception handling
		Console.WriteLine("-> Exception Logging:\n");
		orderService.ProcessOrderWithError(order);

		Console.WriteLine();

		// UserService: Redaction & CorrelationId
		Console.WriteLine("-> UserService mit Redaction:\n");
		var userService = host.Services.GetRequiredService<UserService>();
		var user = new UserProfile(
			UserId: 42,
			Username: "mmueller",
			Email: "max.mueller@example.com",
			SocialSecurityNumber: "123-45-6789",
			PhoneNumber: "+49-170-1234567"
		);
		userService.AuthenticateUser(user);

		Console.WriteLine();

		// NoisyService: Level Overrides
		Console.WriteLine("-> NoisyService (MinimumLevel Override to Warning):\n");
		var noisyService = host.Services.GetRequiredService<NoisyService>();
		noisyService.DoNoisyWork();
	}

	private static void DemonstrateAdvancedFeatures()
	{
		// LogContext.PushProperty: Temporäre Properties
		Console.WriteLine("-> LogContext.PushProperty (temporary Context):\n");

		using (LogContext.PushProperty("RequestPath", "/api/orders"))
		using (LogContext.PushProperty("UserId", 42))
		{
			Log.ForContext<Program>().Information("Request startet – these properties are automatically part of the context");
			Log.ForContext<Program>().Information("Another log message in the same context");
		}

		// Outside the using block - Properties are gone now
		Log.Information("Außerhalb des Kontexts – keine Request-Properties mehr");

		Console.WriteLine();

		// ForContext: Sub-Logger mit zusätzlichem Kontext
		Console.WriteLine("-> ForContext (Named Sub-Logger):\n");

		var paymentLogger = Log.ForContext("PaymentProvider", "Stripe")
			.ForContext("MerchantId", "merchant_abc123");

		paymentLogger.Information("Payment initiated for {Amount:C}", 99.95m);
		paymentLogger.Warning("Retry #{RetryCount} for transaction", 2);

		Console.WriteLine();

		// Timing / Performance Logging
		Console.WriteLine("-> Performance-Messung:\n");

		using (var _ = new OperationTimer("Database-Query"))
		{
			// Simulate some work
			Thread.Sleep(150);
		}

		Console.WriteLine();

		// Conditional logging with Serilog.Expressions
		Console.WriteLine("-> Filter in appsettings: 'HealthCheck'-Messages were filtered:\n");
		Log.Information("Normal Log – Is displayed");
		Log.Information("HealthCheck: System OK – Filter excludes this message");
		Log.Information("Another normal Log – Is also displayed");
	}

	private static void ShowInMemoryEntries()
	{
		var entries = InMemorySink.GetEntries();
		Console.WriteLine($"  InMemorySink collected {entries.Count} Entries.\n");

		// Zeige die letzten 5 Einträge
		var lastEntries = entries.TakeLast(5).ToList();
		Console.WriteLine("  Last 5 Entries:");
		foreach (var entry in lastEntries)
		{
			Console.WriteLine($"    [{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {entry.Message}");
			if (entry.Properties.Count > 0)
			{
				var propsStr = string.Join(", ", entry.Properties.Select(p => $"{p.Key}={p.Value}"));
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.WriteLine($"      Properties: {propsStr}");
				Console.ResetColor();
			}
		}
	}

	private static Order CreateSampleOrder()
	{
		return new(
			OrderId: 4711,
			CustomerName: "Max Mustermann",
			CustomerEmail: "max.mustermann@example.com",
			CreditCardNumber: "4111-1111-1111-1234",
			TotalAmount: 259.97m,
			Items:
			[
				new("Mechanical Keyboard", 1, 149.99m),
					new("USB-C Kabel", 2, 14.99m),
					new("Mauspad XL", 1, 79.99m)
			]
		);
	}

	private static void PrintHeader(string title)
	{
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine($"\n  ╔{'═'.ToString().PadRight(title.Length + 4, '═')}╗");
		Console.WriteLine($"  ║  {title}  ║");
		Console.WriteLine($"  ╚{'═'.ToString().PadRight(title.Length + 4, '═')}╝\n");
		Console.ResetColor();
	}
}

// ============================================================================
// Helper for the operation timer for the demo performance logging
// ============================================================================

/// <summary>
/// Simple Timer which logs the duration the operation runs.
/// Shows how Serilog can be used with an IDisposable pattern for timing
/// </summary>
file class OperationTimer : IDisposable
{
	private readonly string operationName;
	private readonly Stopwatch sw;

	public OperationTimer(string operationName)
	{
		this.operationName = operationName;
		sw = Stopwatch.StartNew();
		Log.Information("Operation '{operationName}' started", this.operationName);
	}

	public void Dispose()
	{
		sw.Stop();
		Log.Information(
			"Operation '{operationName}' completed in {elapsed}ms",
			operationName,
			sw.ElapsedMilliseconds);
	}
}
