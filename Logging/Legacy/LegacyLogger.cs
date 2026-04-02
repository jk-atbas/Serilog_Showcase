using System.Runtime.CompilerServices;

namespace SerilogShowcase.Logging.Legacy;

/// <summary>
/// Simulates a legacy logger that is hidden behind the ILogger interface
/// </summary>
public interface ILegacyLogger
{
	void LogInfo(string message, string? caller = null);
	void LogWarning(string message, string? caller = null);
	void LogError(string message, Exception? exception = null, string? caller = null);
	void LogDebug(string message, string? caller = null);
}

/// <summary>
/// Example implementation for a legacy logger
/// </summary>
public class LegacyLogger : ILegacyLogger
{
	private const string BaseFormat = "{0}: {1}, {2} - {3}";

	public void LogInfo(string message, [CallerMemberName] string? caller = null)
	{
		Console.WriteLine(FormatString(caller, "Info", message));
	}

	public void LogWarning(string message, [CallerMemberName] string? caller = null)
	{
		Console.WriteLine(FormatString(caller, "Warn", message));
	}

	public void LogError(string message, Exception? exception = null, [CallerMemberName] string? caller = null)
	{
		Console.WriteLine(FormatString(caller, "Error", message, exception));
	}

	public void LogDebug(string message, [CallerMemberName] string? caller = null)
	{
		Console.WriteLine(FormatString(caller, "Debug", message));
	}

	private static string FormatString(string? caller, string logLevel, string message, Exception? exception = null)
	{
		var formattedTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm:ss,fff");

		return exception is not null
			? string.Format(BaseFormat, formattedTime, caller ?? "General", logLevel, message)
			  + $" \n {exception.Message}"
			: string.Format(BaseFormat, formattedTime, caller ?? "General", logLevel, message);
	}
}
