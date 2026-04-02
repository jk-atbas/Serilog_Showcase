using Serilog.Core;
using Serilog.Events;
using SerilogShowcase.Models;

namespace SerilogShowcase.Redaction;

// ============================================================================
// REDACTION / MASKING: Protecting sensible data in logs
// ============================================================================
// Serilog provides a possibility to transform object when logged, this is implemented through IDestructuringPolicy.
// This allows for more safety and flexibility than overriding an objects ToString-method. Another advantage is
// that the IDestructuringPolicy is directly integrable into Serilog's logging pipeline.
//
// TLDR: Allows for central redaction logic that allows testing and most importantly applies to all sinks.
// ============================================================================

/// <summary>
/// Masks sensible properties when logging objects.
/// Activates when a logging string contains an annotated @ in front of a placeholder
/// Example: logger.LogInformation("Order: {@Order}", order);
/// </summary>
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
	public bool TryDestructure(
		object value,
		ILogEventPropertyValueFactory propertyValueFactory,
		out LogEventPropertyValue? result)
	{
		result = value switch
		{
			Order order => DestructureOrder(order, propertyValueFactory),
			UserProfile user => DestructureUserProfile(user, propertyValueFactory),
			_ => null
		};

		return result is not null;
	}

	private static StructureValue DestructureOrder(
		Order order,
		ILogEventPropertyValueFactory factory)
	{
		var properties = new List<LogEventProperty>
		{
			new("OrderId", factory.CreatePropertyValue(order.OrderId)),
			new("CustomerName", factory.CreatePropertyValue(order.CustomerName)),
			new("CustomerEmail", factory.CreatePropertyValue(MaskEmail(order.CustomerEmail))),
			new("CreditCardNumber", factory.CreatePropertyValue(MaskCreditCard(order.CreditCardNumber))),
			new("TotalAmount", factory.CreatePropertyValue(order.TotalAmount)),
			new("ItemCount", factory.CreatePropertyValue(order.Items.Count))
		};

		return new StructureValue(properties);
	}

	private static StructureValue DestructureUserProfile(
		UserProfile user,
		ILogEventPropertyValueFactory factory)
	{
		var properties = new List<LogEventProperty>
		{
			new("UserId", factory.CreatePropertyValue(user.UserId)),
			new("Username", factory.CreatePropertyValue(user.Username)),
			new("Email", factory.CreatePropertyValue(MaskEmail(user.Email))),
			new("SocialSecurityNumber", factory.CreatePropertyValue("***-**-****")),
			new("PhoneNumber", factory.CreatePropertyValue(MaskPhone(user.PhoneNumber)))
		};

		return new StructureValue(properties);
	}

	private static string MaskEmail(string email)
	{
		var parts = email.Split('@');
		if (parts.Length != 2)
		{
			return "***@***";
		}

		var local = parts[0];
		var masked = local.Length > 2
			? $"{local[0]}***{local[^1]}"
			: "***";

		return $"{masked}@{parts[1]}";
	}

	private static string MaskCreditCard(string cardNumber)
	{
		var digits = cardNumber.Replace("-", "").Replace(" ", "");
		return digits.Length >= 4
			? $"****-****-****-{digits[^4..]}"
			: "****";
	}

	private static string MaskPhone(string phone)
	{
		return phone.Length >= 4
			? $"***-***-{phone[^4..]}"
			: "****";
	}
}
