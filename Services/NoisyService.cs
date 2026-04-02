using Microsoft.Extensions.Logging;

namespace SerilogShowcase.Services;

/// <summary>
/// Simulates a "noisy" service that produces lots of logs.
/// In the demo this service has a loglevel override to only log warning level logs in appsettings.
/// Simuliert einen "lauten" Service, der viele Logs produziert.
/// </summary>
public class NoisyService(ILogger<NoisyService> logger)
{
	public void DoNoisyWork()
	{
		// These won't be logged (MinimumLevel Override = Warning)
		logger.LogDebug("NoisyService: Debug message (Should not be visible)");
		logger.LogInformation("NoisyService: Info message (Should not be visible)");

		// This is logged
		logger.LogWarning("NoisyService: Warning - This is visible due to the override");
	}
}
