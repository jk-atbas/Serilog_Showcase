using Microsoft.Extensions.Logging;
using SerilogShowcase.Models;

namespace SerilogShowcase.Services;

// ============================================================================
// SERVICE with ILogger<T>: That's how a service should look like,
// when a service doesn't need its logging logic altered when switching logging frameworks
// ============================================================================
// This service exclusively uses Microsoft.Extensions.Logging.ILogger<T>.
// Whether Serilog or the legacy logging is in use, doesn't matter to the service
// ============================================================================

public class OrderService(ILogger<OrderService> logger)
{
	/// <summary>
	/// Simply simulates an order process and how the logging facade is used alongside Serilog
	/// </summary>
	public void ProcessOrder(Order order)
	{
		// 1. Structured logging with message templates
		// Serilog's killer-feature: Properties are safed as key-value pairs.
		// This allows analysis engines like elastic search to look up logs specific to a property value (like OrderId=42)
		logger.LogInformation(
			"Order {orderId} will be processed for customer {customerName}",
			order.OrderId,
			order.CustomerName);

		// 2. Object destructuring with @
		// The @ in front of the placeholder signals Serilog: "Deconstruct the given object into
		// its properties". Here is where the IDestructuringPolicy is applied!
		logger.LogInformation(
			"Orderdetails: {@order}",
			order);

		// 3. Scoped Logging mit BeginScope
		// Everything inside a scope automatically gets these properties added to their context.
		// Incredibly useful for request and transaction contexts.
		using (logger.BeginScope(new Dictionary<string, object>
		{
			["OrderId"] = order.OrderId,
			["TransactionPhase"] = "Validation"
		}))
		{
			logger.LogDebug("Validating order positions...");

			foreach (var item in order.Items)
			{
				logger.LogDebug(
					"Position: {productName}, Quantity: {quantity}, Price: {unitPrice:C}",
					item.ProductName,
					item.Quantity,
					item.UnitPrice);
			}

			ValidateOrder(order);
		}

		// 4. Performance-relevante bedingte Logs
		// Does not interpolate the string if the log level is not active.
		// Serilog does this automatically through the given message template.
		// For very expensive logs a check on the active loglevel could be still useful.
		if (logger.IsEnabled(LogLevel.Debug))
		{
			var totalItems = order.Items.Sum(i => i.Quantity);
			logger.LogDebug(
				"Order {orderId} contains a total amount of {totalItems} articles " +
				"with a total value of {totalAmount:C}",
				order.OrderId,
				totalItems,
				order.TotalAmount);
		}

		// 5. EventIds for categorised events
		// Allows for centralized filtering when log aggregators are used
		var eventId = new EventId(1001, "OrderCompleted");
		logger.Log(
			LogLevel.Information,
			eventId,
			"Order {orderId} processed successfully. Amount: {totalAmount:C}",
			order.OrderId,
			order.TotalAmount);
	}

	/// <summary>
	/// Demonstrates how an error is logged
	/// </summary>
	public void ProcessOrderWithError(Order order)
	{
		try
		{
			logger.LogInformation(
				"Start processing problematic Order {orderId}",
				order.OrderId);

			// Simulated error
			throw new InvalidOperationException(
				$"Article '{order.Items.First().ProductName}' is not in stock!");
		}
		catch (Exception e)
		{
			// 6. Exception Logging
			// The exception is the first parameter - Serilog will add the corresponding stack trace
			logger.LogError(
				e,
				"A error occured while processing Order {orderId} " +
				"for customer {customerName}. Amount: {totalAmount:C}",
				order.OrderId,
				order.CustomerName,
				order.TotalAmount);
		}
	}

	private void ValidateOrder(Order order)
	{
		if (order.TotalAmount <= 0)
		{
			logger.LogWarning(
				"Order {orderId} has invalid amount: {totalAmount}",
				order.OrderId,
				order.TotalAmount);
		}
	}
}
