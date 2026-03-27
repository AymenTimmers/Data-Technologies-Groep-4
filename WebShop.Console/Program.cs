using System.Net.Http.Json;
using System.Text.Json;
using WebShop.Contracts.Models;

var apiBaseUrl = Environment.GetEnvironmentVariable("WEBSHOP_API_BASE_URL");
var client = new HttpClient
{
	BaseAddress = new Uri(string.IsNullOrWhiteSpace(apiBaseUrl) ? "http://localhost:5088" : apiBaseUrl)
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
		"Browse & Search Products",
		"Top 5 sold products",
		"View cart",
		"Remove item from cart",
		"Checkout",
		"My orders",
		"Generate model docs",
		"Refresh recommendations cache",
		"View API logs (last 20)",
		"Logout",
		"Exit"
	});

	if (userChoice == 0)
	{
		await BrowseProducts(currentUser.UserId);
	}
	else if (userChoice == 1)
	{
		await ViewTopSoldProducts();
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
		await GenerateModelDocumentation();
	}
	else if (userChoice == 7)
	{
		await RefreshRecommendationsCache();
	}
	else if (userChoice == 8)
	{
		ViewApiLogs();
	}
	else if (userChoice == 9)
	{
		currentUser = null;
	}
	else if (userChoice == 10)
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

	var response = await SafePostAsJson("/auth/register", new
	{
		email,
		password,
		firstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
		lastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName
	});
	if (response is null)
	{
		return;
	}

	await PrintResult(response, "Registration successful.");
}

async Task<UserSession?> Login()
{
	Console.WriteLine();
	var email = PromptEmail("Email");
	var password = PromptPassword("Password", 6, 128);

	var response = await SafePostAsJson("/auth/login", new { email, password });
	if (response is null)
	{
		return null;
	}

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

async Task BrowseProducts(long userId)
{
	while (true)
	{
		var categories = await SafeGetFromJson<List<CategoryDto>>("/categories");
		if (categories is null) return;

		var browseChoice = SelectMenu("=== Browse Products ===", new[]
		{
			"View all products",
			"Search by name/brand",
			"Filter by category",
			"Filter by price range",
			"Advanced search",
			"Back to menu"
		});

		List<ProductDto>? products = null;

		if (browseChoice == 0)
		{
			products = await SafeGetFromJson<List<ProductDto>>("/products");
		}
		else if (browseChoice == 1)
		{
			var searchTerm = Prompt("Search for (name/brand/description)", required: false);
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				products = await SafePostAsJsonGetJson<object, List<ProductDto>>("/products/search", new
				{
					SearchTerm = searchTerm,
					CategoryId = (long?)null,
					MinPrice = (double?)null,
					MaxPrice = (double?)null
				});
			}
		}
		else if (browseChoice == 2)
		{
			Console.WriteLine("\nAvailable categories:");
			for (int i = 0; i < categories.Count; i++)
			{
				Console.WriteLine($"{i + 1}. {categories[i].Name}");
			}
			var catIndex = PromptIntRange("Select category", 1, categories.Count) - 1;
			products = await SafePostAsJsonGetJson<object, List<ProductDto>>("/products/search", new
			{
				SearchTerm = (string?)null,
				CategoryId = categories[catIndex].Id,
				MinPrice = (double?)null,
				MaxPrice = (double?)null
			});
		}
		else if (browseChoice == 3)
		{
			var minPrice = PromptDouble("Minimum price (0 for no limit)", 0);
			var maxPrice = PromptDouble("Maximum price (0 for no limit)", 0);
			products = await SafePostAsJsonGetJson<object, List<ProductDto>>("/products/search", new
			{
				SearchTerm = (string?)null,
				CategoryId = (long?)null,
				MinPrice = minPrice,
				MaxPrice = maxPrice
			});
		}
		else if (browseChoice == 4)
		{
			Console.WriteLine("\n=== Advanced Search ===");
			var searchTerm = Prompt("Search term (optional)", required: false);
			var showCategories = categories.Count > 0;
			long? categoryId = null;
			if (showCategories)
			{
				Console.WriteLine("\nAvailable categories:");
				for (int i = 0; i < categories.Count; i++)
				{
					Console.WriteLine($"{i + 1}. {categories[i].Name}");
				}
				Console.WriteLine($"{categories.Count + 1}. (Skip)");
				var catChoice = PromptIntRange("Select category", 1, categories.Count + 1);
				if (catChoice <= categories.Count)
				{
					categoryId = categories[catChoice - 1].Id;
				}
			}
			var minPrice = PromptDouble("Minimum price (0 for no limit)", 0);
			var maxPrice = PromptDouble("Maximum price (0 for no limit)", 0);

			products = await SafePostAsJsonGetJson<object, List<ProductDto>>("/products/search", new
			{
				SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
				CategoryId = categoryId,
				MinPrice = minPrice,
				MaxPrice = maxPrice
			});
		}
		else
		{
			return;
		}

		if (products is null) continue;

		if (products.Count == 0)
		{
			Console.WriteLine("\nNo products found matching your criteria.");
			Pause();
			continue;
		}

		// Display products and allow selection
		while (true)
		{
			Console.Clear();
			Console.WriteLine("=== Product Results ===\n");
			for (int i = 0; i < Math.Min(products.Count, 20); i++)
			{
				var p = products[i];
				var stock = p.Stock > 0 ? $"In Stock ({p.Stock})" : "Out of Stock";
				Console.WriteLine($"{i + 1}. {p.Name}");
				Console.WriteLine($"   EUR {p.Price:F2} | {stock}");
			}

			if (products.Count > 20)
			{
				Console.WriteLine($"\n... and {products.Count - 20} more products");
			}

			Console.WriteLine("\nEnter product number to view details (or 0 to go back):");
			var input = Prompt("Selection", required: false);
			if (int.TryParse(input, out var selection) && selection > 0 && selection <= products.Count)
			{
				await ViewProductDetails(products[selection - 1], userId);
			}
			else if (input == "0")
			{
				break;
			}
			else
			{
				Console.WriteLine("Invalid selection. Try again.");
				Pause();
			}
		}
	}
}

async Task ViewProductDetails(ProductDto product, long userId)
{
	while (true)
	{
		Console.Clear();
		Console.WriteLine("═══════════════════════════════════════════════════════════");
		Console.WriteLine($"  {product.Name}");
		Console.WriteLine("═══════════════════════════════════════════════════════════\n");

		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"Price: EUR {product.Price:F2}");
		Console.ResetColor();

		var stockStatus = product.Stock > 0 ? $"In Stock ({product.Stock})" : "Out of Stock";
		Console.ForegroundColor = product.Stock > 0 ? ConsoleColor.Green : ConsoleColor.Red;
		Console.WriteLine($"Stock: {stockStatus}");
		Console.ResetColor();

		if (!string.IsNullOrWhiteSpace(product.Description))
		{
			Console.WriteLine($"\nDescription:\n{product.Description}");
		}

		var details = new List<string>();
		if (!string.IsNullOrWhiteSpace(product.Brand))
			details.Add($"Brand: {product.Brand}");
		if (!string.IsNullOrWhiteSpace(product.Publisher))
			details.Add($"Publisher: {product.Publisher}");
		if (product.ReleaseYear.HasValue)
			details.Add($"Release Year: {product.ReleaseYear}");

		if (details.Count > 0)
		{
			Console.WriteLine("\nDetails:");
			foreach (var detail in details)
			{
				Console.WriteLine($"  • {detail}");
			}
		}

		// Fetch and display reviews
		Console.WriteLine("\n───────────────────────────────────────────────────────────");
		Console.WriteLine("REVIEWS:");
		var reviews = await SafeGetFromJson<List<ProductReviewDto>>($"/products/{product.Id}/reviews");
		if (reviews is not null && reviews.Count > 0)
		{
			var avgRating = reviews.Average(r => r.Stars);
			Console.WriteLine($"\nAverage Rating: {avgRating:F1}/5 ({reviews.Count} reviews)\n");

			foreach (var review in reviews.Take(5))
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write($"★ {review.Stars}/5");
				Console.ResetColor();
				Console.WriteLine($" by {review.UserEmail} ({review.CreatedAtUtc})");
				Console.WriteLine($"{review.Explanation}\n");
			}

			if (reviews.Count > 5)
			{
				Console.WriteLine($"... and {reviews.Count - 5} more reviews");
			}
		}
		else
		{
			Console.WriteLine("\nNo reviews yet. Be the first to review!");
		}

		Console.WriteLine("───────────────────────────────────────────────────────────");
		Console.WriteLine("RECOMMENDATIONS:");
		var recsResponse = await SafeGetFromJson<RecommendationResponse>($"/products/{product.Id}/recommendations");
		if (recsResponse is not null && recsResponse.Recommendations.Count > 0)
		{
			Console.WriteLine($"\n{recsResponse.Recommendations.Count} products bought together with this item:\n");
			var displayCount = 0;
			foreach (var rec in recsResponse.Recommendations.Take(5))
			{
				displayCount++;
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"{displayCount}. {rec.ProductName}");
				Console.ResetColor();
				Console.WriteLine($"   EUR {rec.Price:F2} | Stock: {rec.Stock} | Bought {rec.BuyCount}x together");
				if (!string.IsNullOrWhiteSpace(rec.Description))
				{
					var desc = rec.Description.Length > 60 ? rec.Description[..60] + "..." : rec.Description;
					Console.WriteLine($"   {desc}");
				}
				Console.WriteLine();
			}
			if (recsResponse.Recommendations.Count > 5)
			{
				Console.WriteLine($"... and {recsResponse.Recommendations.Count - 5} more recommendations");
			}
		}
		else
		{
			Console.WriteLine("\nNo recommendations yet - this is a new product!");
		}

		Console.WriteLine("───────────────────────────────────────────────────────────\n");

		var action = SelectMenu("What would you like to do?", new[]
		{
			"Add to cart",
			"Leave a review",
			"Back to product list"
		});

		if (action == 0)
		{
			var quantity = PromptIntRange("Quantity", 1, Math.Min(100, product.Stock));
			var response = await SafePostAsJson("/cart/items", new
			{
				userId,
				productId = product.Id,
				quantity
			});
			if (response is not null)
			{
				await PrintResult(response, $"Added {quantity} x {product.Name} to cart!");
				Pause();
			}
		}
		else if (action == 1)
		{
			await LeaveProductReviewDetailed(product.Id, userId);
		}
		else
		{
			break;
		}
	}
}

async Task LeaveProductReviewDetailed(long productId, long userId)
{
	Console.Clear();
	Console.WriteLine($"=== Leave a Review for Product #{productId} ===\n");

	var stars = PromptIntRange("Rating (1-5 stars)", 1, 5);
	var explanation = PromptWithMaxLength("Your review", 1000);

	var response = await SafePostAsJson($"/products/{productId}/reviews", new
	{
		userId,
		stars,
		explanation
	});
	if (response is not null)
	{
		await PrintResult(response, "Thank you! Your review has been saved.");
		Pause();
	}
}

async Task ViewTopSoldProducts()
{
	var products = await SafeGetFromJson<List<TopSoldProductDto>>("/products/top-sold");
	if (products is null)
	{
		return;
	}

	if (products.Count == 0)
	{
		Console.WriteLine("No sold products yet.");
		return;
	}

	Console.WriteLine();
	foreach (var product in products)
	{
		Console.WriteLine($"#{product.ProductId} | {product.ProductName} | sold: {product.SoldQuantity} | revenue: EUR {product.Revenue:F2}");
	}
}

async Task ViewCart(long userId)
{
	var cart = await SafeGetFromJson<CartResponseDto>($"/cart/{userId}");
	if (cart is null)
	{
		return;
	}

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
	var response = await SafeDelete($"/cart/items/{itemId}?userId={userId}");
	if (response is null)
	{
		return;
	}

	await PrintResult(response, "Item removed.");
}

async Task Checkout(long userId)
{
	var shippingAddress = PromptWithMaxLength("Shipping address", 250);
	var discountCode = PromptOptionalWithMaxLength("Discount code (optional)", 40);

	var response = await SafePostAsJson("/orders/checkout", new
	{
		userId,
		shippingAddress,
		discountCode = string.IsNullOrWhiteSpace(discountCode) ? null : discountCode
	});
	if (response is null)
	{
		return;
	}

	await PrintResult(response, "Checkout completed.");
}

async Task ViewOrders(long userId)
{
	var orders = await SafeGetFromJson<List<OrderResponseDto>>($"/orders/{userId}");
	if (orders is null)
	{
		return;
	}

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

async Task GenerateModelDocumentation()
{
	var response = await SafePostAsJson("/docs/models/generate", new { });
	if (response is null)
	{
		return;
	}

	await PrintResult(response, "Model documentation generated.");
}

async Task RefreshRecommendationsCache()
{
	Console.WriteLine("\nRefreshing recommendations cache...");
	var response = await SafePostAsJson("/cache/recommendations/refresh", new { });
	if (response is null)
	{
		return;
	}

	await PrintResult(response, "Recommendations cache refreshed successfully!");
	Pause();
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

static double? PromptDouble(string label, double minValue)
{
	while (true)
	{
		var value = Prompt(label);
		if (double.TryParse(value, out var number) && number >= minValue)
		{
			return number == 0 ? null : (double?)number;
		}

		Console.WriteLine($"Please enter a valid number >= {minValue}.");
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

async Task<HttpResponseMessage?> SafePostAsJson<T>(string url, T body)
{
	try
	{
		return await client.PostAsJsonAsync(url, body);
	}
	catch (HttpRequestException ex)
	{
		ShowApiUnavailable(ex);
		return null;
	}
}

async Task<HttpResponseMessage?> SafeDelete(string url)
{
	try
	{
		return await client.DeleteAsync(url);
	}
	catch (HttpRequestException ex)
	{
		ShowApiUnavailable(ex);
		return null;
	}
}

async Task<T?> SafeGetFromJson<T>(string url)
{
	try
	{
		return await client.GetFromJsonAsync<T>(url, jsonOptions);
	}
	catch (HttpRequestException ex)
	{
		ShowApiUnavailable(ex);
		return default;
	}
}

async Task<TResponse?> SafePostAsJsonGetJson<TRequest, TResponse>(string url, TRequest body)
{
	try
	{
		var response = await client.PostAsJsonAsync(url, body);
		if (!response.IsSuccessStatusCode)
		{
			return default;
		}
		return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions);
	}
	catch (HttpRequestException ex)
	{
		ShowApiUnavailable(ex);
		return default;
	}
}

void ShowApiUnavailable(HttpRequestException ex)
{
	Console.WriteLine();
	Console.WriteLine("API is not reachable.");
	Console.WriteLine($"Configured API URL: {client.BaseAddress}");
	Console.WriteLine("Start WebShop.Api and try again.");
	Console.WriteLine($"Details: {ex.Message}");
	Pause();
}

record UserSession(long UserId, string Email, int Role);

record RecommendationResponse(
    long ProductId,
    List<ProductRecommendedDto> Recommendations,
    DateTime CacheLastRefreshed
);
