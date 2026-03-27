using System.Net.Http.Json;
using System.Text.Json;

var client = new HttpClient
{
	BaseAddress = new Uri("http://localhost:5088")
};

var jsonOptions = new JsonSerializerOptions
{
	PropertyNameCaseInsensitive = true
};

UserSession? currentUser = null;

while (true)
{
	if (currentUser is null)
	{
		var choice = SelectMenu("=== WebShop ===", new[]
		{
			"Register",
			"Login",
			"Exit"
		});

		if (choice == 0)
		{
			await Register();
		}
		else if (choice == 1)
		{
			currentUser = await Login();
		}
		else if (choice == 2)
		{
			break;
		}

		continue;
	}

	var userChoice = SelectMenu($"=== Welcome {currentUser.Email} ===", new[]
	{
		"List products",
		"Add item to cart",
		"View cart",
		"Remove item from cart",
		"Checkout",
		"My orders",
		"View API logs (last 20)",
		"Logout",
		"Exit"
	});

	if (userChoice == 0)
	{
		await ListProducts();
	}
	else if (userChoice == 1)
	{
		await AddToCart(currentUser.UserId);
	}
	else if (userChoice == 2)
	{
		await ViewCart(currentUser.UserId);
	}
	else if (userChoice == 3)
	{
		await RemoveCartItem(currentUser.UserId);
	}
	else if (userChoice == 4)
	{
		await Checkout(currentUser.UserId);
	}
	else if (userChoice == 5)
	{
		await ViewOrders(currentUser.UserId);
	}
	else if (userChoice == 6)
	{
		ViewApiLogs();
	}
	else if (userChoice == 7)
	{
		currentUser = null;
	}
	else if (userChoice == 8)
	{
		break;
	}
}

return;

static int SelectMenu(string title, IReadOnlyList<string> options)
{
	if (options.Count == 0)
	{
		throw new ArgumentException("Menu options cannot be empty.", nameof(options));
	}

	var selectedIndex = 0;
	while (true)
	{
		Console.Clear();
		Console.WriteLine(title);
		Console.WriteLine("Use Up/Down arrows and Enter.");
		Console.WriteLine();

		for (var i = 0; i < options.Count; i++)
		{
			if (i == selectedIndex)
			{
				Console.ForegroundColor = ConsoleColor.Black;
				Console.BackgroundColor = ConsoleColor.White;
				Console.WriteLine($"> {options[i]}");
				Console.ResetColor();
			}
			else
			{
				Console.WriteLine($"  {options[i]}");
			}
		}

		var key = Console.ReadKey(intercept: true).Key;
		if (key == ConsoleKey.UpArrow)
		{
			selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
			continue;
		}

		if (key == ConsoleKey.DownArrow)
		{
			selectedIndex = (selectedIndex + 1) % options.Count;
			continue;
		}

		if (key == ConsoleKey.Enter)
		{
			Console.Clear();
			return selectedIndex;
		}
	}
}

async Task Register()
{
	Console.WriteLine();
	var email = PromptEmail("Email");
	var password = PromptPassword("Password", 6, 128);
	var firstName = PromptOptionalWithMaxLength("First name (optional)", 100);
	var lastName = PromptOptionalWithMaxLength("Last name (optional)", 100);

	var response = await client.PostAsJsonAsync("/auth/register", new
	{
		email,
		password,
		firstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
		lastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName
	});

	await PrintResult(response, "Registration successful.");
}

async Task<UserSession?> Login()
{
	Console.WriteLine();
	var email = PromptEmail("Email");
	var password = PromptPassword("Password", 6, 128);

	var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
	if (!response.IsSuccessStatusCode)
	{
		await PrintResult(response, "Login failed.");
		return null;
	}

	var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(jsonOptions);
	if (payload is null)
	{
		Console.WriteLine("Login failed: empty response.");
		return null;
	}

	Console.WriteLine("Login successful.");
	return new UserSession(payload.UserId, payload.Email, payload.Role);
}

async Task ListProducts()
{
	var products = await client.GetFromJsonAsync<List<Product>>("/products", jsonOptions);
	if (products is null || products.Count == 0)
	{
		Console.WriteLine("No products found.");
		return;
	}

	Console.WriteLine();
	foreach (var product in products)
	{
		Console.WriteLine($"#{product.Id} | {product.Name} | EUR {product.Price:F2} | stock: {product.Stock}");
	}
}

async Task AddToCart(long userId)
{
	var productId = PromptLongPositive("Product id");
	var quantity = PromptIntRange("Quantity", 1, 100);

	var response = await client.PostAsJsonAsync("/cart/items", new
	{
		userId,
		productId,
		quantity
	});

	await PrintResult(response, "Cart updated.");
}

async Task ViewCart(long userId)
{
	var cart = await client.GetFromJsonAsync<CartResponse>($"/cart/{userId}", jsonOptions);
	if (cart is null || cart.Items.Count == 0)
	{
		Console.WriteLine("Cart is empty.");
		return;
	}

	Console.WriteLine();
	double total = 0;
	foreach (var item in cart.Items)
	{
		var lineTotal = item.UnitPrice * item.Quantity;
		total += lineTotal;
		Console.WriteLine($"Item #{item.ItemId} | Product #{item.ProductId} {item.ProductName} | {item.Quantity} x EUR {item.UnitPrice:F2} = EUR {lineTotal:F2}");
	}

	Console.WriteLine($"Cart total: EUR {total:F2}");
}

async Task RemoveCartItem(long userId)
{
	var itemId = PromptLongPositive("Cart item id to remove");
	var response = await client.DeleteAsync($"/cart/items/{itemId}?userId={userId}");
	await PrintResult(response, "Item removed.");
}

async Task Checkout(long userId)
{
	var shippingAddress = PromptWithMaxLength("Shipping address", 250);
	var discountCode = PromptOptionalWithMaxLength("Discount code (optional)", 40);

	var response = await client.PostAsJsonAsync("/orders/checkout", new
	{
		userId,
		shippingAddress,
		discountCode = string.IsNullOrWhiteSpace(discountCode) ? null : discountCode
	});

	await PrintResult(response, "Checkout completed.");
}

async Task ViewOrders(long userId)
{
	var orders = await client.GetFromJsonAsync<List<OrderResponse>>($"/orders/{userId}", jsonOptions);
	if (orders is null || orders.Count == 0)
	{
		Console.WriteLine("No orders found.");
		return;
	}

	Console.WriteLine();
	foreach (var order in orders)
	{
		Console.WriteLine($"Order #{order.OrderId} ({order.OrderNumber}) - EUR {order.TotalPrice:F2}");
		Console.WriteLine($"Address: {order.ShippingAddress}");
		foreach (var item in order.Items)
		{
			Console.WriteLine($"  - {item.ProductName} ({item.Quantity} x EUR {item.UnitPrice:F2})");
		}
	}
}

void ViewApiLogs()
{
	var logPath = Path.GetFullPath(Path.Combine(
		AppContext.BaseDirectory,
		"..", "..", "..", "..",
		"WebShop.Api",
		"Logs",
		"requests.log"
	));

	Console.WriteLine();
	Console.WriteLine("=== API Request Logs (last 20) ===");
	Console.WriteLine($"Source: {logPath}");

	if (!File.Exists(logPath))
	{
		Console.WriteLine("No log file found yet. Start the API and make at least one request.");
		Pause();
		return;
	}

	var lines = File.ReadAllLines(logPath);
	var start = Math.Max(0, lines.Length - 20);
	for (var i = start; i < lines.Length; i++)
	{
		Console.WriteLine(lines[i]);
	}

	if (lines.Length == 0)
	{
		Console.WriteLine("Log file is empty.");
	}

	Pause();
}

static void Pause()
{
	Console.WriteLine();
	Console.Write("Press Enter to continue...");
	while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter)
	{
	}
	Console.WriteLine();
}

static string Prompt(string label, bool required = true)
{
	while (true)
	{
		Console.Write($"{label}: ");
		var value = Console.ReadLine() ?? string.Empty;
		if (!required || !string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
	}
}

static int PromptIntRange(string label, int min, int max)
{
	while (true)
	{
		var value = Prompt(label);
		if (ConsoleInputValidation.TryParseIntInRange(value, min, max, out var number))
		{
			return number;
		}

		Console.WriteLine($"Please enter a valid number between {min} and {max}.");
	}
}

static long PromptLongPositive(string label)
{
	while (true)
	{
		var value = Prompt(label);
		if (ConsoleInputValidation.TryParsePositiveLong(value, out var number))
		{
			return number;
		}

		Console.WriteLine("Please enter a positive number.");
	}
}

static string PromptWithMaxLength(string label, int maxLength)
{
	while (true)
	{
		var value = Prompt(label);
		if (ConsoleInputValidation.IsValidRequiredText(value, maxLength))
		{
			return value.Trim();
		}

		Console.WriteLine($"Maximum length is {maxLength} characters.");
	}
}

static string? PromptOptionalWithMaxLength(string label, int maxLength)
{
	while (true)
	{
		var value = Prompt(label, required: false);
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (ConsoleInputValidation.IsValidOptionalText(value, maxLength))
		{
			return value.Trim();
		}

		Console.WriteLine($"Maximum length is {maxLength} characters.");
	}
}

static string PromptEmail(string label)
{
	while (true)
	{
		var value = Prompt(label);
		if (ConsoleInputValidation.TryNormalizeEmail(value, out var normalizedEmail))
		{
			return normalizedEmail;
		}

		Console.WriteLine("Please enter a valid email address.");
	}
}

static string PromptPassword(string label, int minLength, int maxLength)
{
	while (true)
	{
		var value = Prompt(label);
		if (ConsoleInputValidation.IsValidPassword(value, minLength, maxLength))
		{
			return value;
		}

		Console.WriteLine($"Password must be between {minLength} and {maxLength} characters.");
	}
}

async Task PrintResult(HttpResponseMessage response, string successMessage)
{
	if (response.IsSuccessStatusCode)
	{
		Console.WriteLine(successMessage);
		var body = await response.Content.ReadAsStringAsync();
		if (!string.IsNullOrWhiteSpace(body))
		{
			Console.WriteLine(body);
		}
		return;
	}

	var errorBody = await response.Content.ReadAsStringAsync();
	if (string.IsNullOrWhiteSpace(errorBody))
	{
		Console.WriteLine($"Request failed: {(int)response.StatusCode} {response.StatusCode}");
		return;
	}

	try
	{
		using var doc = JsonDocument.Parse(errorBody);
		if (doc.RootElement.TryGetProperty("message", out var message))
		{
			Console.WriteLine($"Request failed: {message.GetString()}");
			return;
		}
	}
	catch
	{
		// Fall through and print the raw body if it is not JSON.
	}

	Console.WriteLine($"Request failed: {errorBody}");
}

record UserSession(long UserId, string Email, int Role);
record AuthResponse(long UserId, string Email, int Role);
record Product(long Id, long CategoryId, string Name, double Price, int Stock);
record CartResponse(long CartId, long UserId, List<CartItem> Items);
record CartItem(long ItemId, long ProductId, string ProductName, double UnitPrice, int Quantity);
record OrderResponse(long OrderId, string OrderNumber, double TotalPrice, string ShippingAddress, List<OrderItem> Items);
record OrderItem(long ProductId, string ProductName, int Quantity, double UnitPrice);
