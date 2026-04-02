using Microsoft.Extensions.Logging;
using SerilogShowcase.Logging;
using SerilogShowcase.Models;

namespace SerilogShowcase.Services;

public class UserService(ILogger<UserService> logger)
{
	/// <summary>
	/// Demonstrates data redaction with user-data and the use of correlation ids
	/// </summary>
	public void AuthenticateUser(UserProfile user)
	{
		// Sets a new CorrelationId for the request
		var correlationId = CorrelationIdEnricher.SetCorrelationId();

		logger.LogInformation(
			"Authentication startet for user {username} [CorrelationId: {correlationId}]",
			user.Username,
			correlationId);

		// Destructured logging – the SensitiveDataDestructuringPolicy masks
		// email, ssn and phone number automatically
		logger.LogInformation(
			"Benutzerprofil geladen: {@userProfile}",
			user);

		// Simulate successful authentication
		logger.LogInformation(
			"User {username} (ID: {userId}) erfolgreich authentifiziert",
			user.Username,
			user.UserId);
	}
}
