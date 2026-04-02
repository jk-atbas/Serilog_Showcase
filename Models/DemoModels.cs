namespace SerilogShowcase.Models;

/// <summary>
/// Example model for customer order.
/// Is used to show the structured logging in Serilog works
/// </summary>
public record Order(
	int OrderId,
	string CustomerName,
	string CustomerEmail,
	string CreditCardNumber,
	decimal TotalAmount,
	List<OrderItem> Items
);

public record OrderItem(
	string ProductName,
	int Quantity,
	decimal UnitPrice
);

/// <summary>
/// Example model that contains sensible data
/// </summary>
public record UserProfile(
	int UserId,
	string Username,
	string Email,
	string SocialSecurityNumber,
	string PhoneNumber
);
