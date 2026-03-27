using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebShop.Contracts.Models;

namespace WebShop.Desktop.Api;

public sealed class WebShopApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebShopApiClient(string baseAddress)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/auth/login", new LoginRequest(email, password), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, cancellationToken);
    }

    public async Task RegisterAsync(string email, string password, string? firstName = null, string? lastName = null, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password, firstName, lastName),
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<ProductDto>>("/products", cancellationToken) ?? [];
    }

    public async Task<List<ProductDto>> SearchProductsAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ProductSearchRequest, List<ProductDto>>("/products/search", request, cancellationToken) ?? [];
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<CategoryDto>>("/categories", cancellationToken) ?? [];
    }

    public async Task<List<ProductReviewDto>> GetReviewsAsync(long productId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<ProductReviewDto>>($"/products/{productId}/reviews", cancellationToken) ?? [];
    }

    public async Task SubmitReviewAsync(long productId, CreateProductReviewRequest request, CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync($"/products/{productId}/reviews", request, cancellationToken);
    }

    public async Task<RecommendationResponse?> GetRecommendationsAsync(long productId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<RecommendationResponse>($"/products/{productId}/recommendations", cancellationToken);
    }

    public async Task AddCartItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync("/cart/items", request, cancellationToken);
    }

    public async Task<CartResponseDto?> GetCartAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<CartResponseDto>($"/cart/{userId}", cancellationToken);
    }

    public async Task RemoveCartItemAsync(long itemId, long userId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"/cart/items/{itemId}?userId={userId}", cancellationToken);
    }

    public async Task CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync("/orders/checkout", request, cancellationToken);
    }

    public async Task<List<OrderResponseDto>> GetOrdersAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<OrderResponseDto>>($"/orders/{userId}", cancellationToken) ?? [];
    }

    public async Task<List<FavoriteProductDto>> GetFavoritesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<FavoriteProductDto>>($"/users/{userId}/favorites", cancellationToken) ?? [];
    }

    public async Task AddFavoriteAsync(long userId, long productId, CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync($"/users/{userId}/favorites/{productId}", new { }, cancellationToken);
    }

    public async Task RemoveFavoriteAsync(long userId, long favoriteId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"/users/{userId}/favorites/{favoriteId}", cancellationToken);
    }

    public async Task<List<ShippingAddressDto>> GetShippingAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<ShippingAddressDto>>($"/users/{userId}/shipping-addresses", cancellationToken) ?? [];
    }

    public async Task AddShippingAddressAsync(long userId, CreateShippingAddressRequest request, CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync($"/users/{userId}/shipping-addresses", request, cancellationToken);
    }

    public async Task RemoveShippingAddressAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"/users/{userId}/shipping-addresses/{addressId}", cancellationToken);
    }

    public async Task<List<TopSoldProductDto>> GetTopSoldAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<TopSoldProductDto>>("/products/top-sold", cancellationToken) ?? [];
    }

    public async Task RefreshRecommendationCacheAsync(CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync("/cache/recommendations/refresh", new { }, cancellationToken);
    }

    public async Task GenerateModelDocsAsync(CancellationToken cancellationToken = default)
    {
        await PostNoResponseAsync("/docs/models/generate", new { }, cancellationToken);
    }

    public async Task<List<AdminUserSummaryDto>> AdminSearchUsersAsync(long adminUserId, string? query, CancellationToken cancellationToken = default)
    {
        var encodedQuery = Uri.EscapeDataString(query ?? string.Empty);
        return await GetAsync<List<AdminUserSummaryDto>>($"/admin/users/search?adminUserId={adminUserId}&query={encodedQuery}", cancellationToken) ?? [];
    }

    public async Task<AdminUserProfileDto?> AdminGetUserProfileAsync(long adminUserId, long userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<AdminUserProfileDto>($"/admin/users/{userId}?adminUserId={adminUserId}", cancellationToken);
    }

    public async Task<DiscountCodeDto?> AdminCreateDiscountCodeAsync(CreateRandomDiscountCodeRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateRandomDiscountCodeRequest, DiscountCodeDto>("/admin/discount-codes/random", request, cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);
    }

    private async Task PostNoResponseAsync<TRequest>(string url, TRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public record RecommendationResponse(long ProductId, List<ProductRecommendedDto> Recommendations, DateTime CacheLastRefreshed);
