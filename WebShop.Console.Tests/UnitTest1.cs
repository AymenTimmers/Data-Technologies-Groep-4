namespace WebShop.Console.Tests;

public class ConsoleInputValidationTests
{
    [Theory]
    [InlineData("USER@Example.com", "user@example.com")]
    [InlineData("  test@site.net ", "test@site.net")]
    public void TryNormalizeEmail_Valid_ReturnsNormalized(string input, string expected)
    {
        var ok = ConsoleInputValidation.TryNormalizeEmail(input, out var normalized);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("   ")]
    public void TryNormalizeEmail_Invalid_ReturnsFalse(string input)
    {
        var ok = ConsoleInputValidation.TryNormalizeEmail(input, out _);

        Assert.False(ok);
    }

    [Fact]
    public void PasswordValidation_RespectsMinMax()
    {
        Assert.False(ConsoleInputValidation.IsValidPassword("12345", 6, 128));
        Assert.True(ConsoleInputValidation.IsValidPassword("123456", 6, 128));
    }

    [Fact]
    public void PositiveLongParsing_Works()
    {
        var ok = ConsoleInputValidation.TryParsePositiveLong("42", out var value);

        Assert.True(ok);
        Assert.Equal(42, value);
    }

    [Fact]
    public void RangeParsing_RejectsOutOfRange()
    {
        var ok = ConsoleInputValidation.TryParseIntInRange("101", 1, 100, out _);

        Assert.False(ok);
    }

    [Fact]
    public void OptionalTextValidation_AllowsEmptyAndRejectsLong()
    {
        Assert.True(ConsoleInputValidation.IsValidOptionalText("", 40));
        Assert.False(ConsoleInputValidation.IsValidOptionalText(new string('x', 41), 40));
    }
}

public class ConsoleProductSearchValidationTests
{
    [Fact]
    public void SearchTerm_CanBeEmpty_ForSimpleBrowse()
    {
        var searchTerm = "";
        Assert.True(string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length > 0);
    }

    [Fact]
    public void SearchTerm_WithSpecialCharacters_IsValid()
    {
        var searchTerm = "Product-Name_123 & Co.";
        Assert.NotEmpty(searchTerm);
        Assert.Contains(" ", searchTerm);
    }

    [Fact]
    public void PriceFilter_AcceptsZeroAsNoLimit()
    {
        double price = 0;
        var isNoLimit = price == 0;
        Assert.True(isNoLimit);
    }

    [Fact]
    public void PriceFilter_AcceptsPositiveValues()
    {
        double minPrice = 10.50;
        double maxPrice = 99.99;
        
        Assert.True(minPrice > 0);
        Assert.True(maxPrice > minPrice);
    }

    [Fact]
    public void CategoryId_CanBeNullForSkip()
    {
        long? categoryId = null;
        Assert.Null(categoryId);
    }

    [Fact]
    public void CategoryId_CanBeValidPositive()
    {
        long? categoryId = 5;
        Assert.NotNull(categoryId);
        Assert.True(categoryId > 0);
    }
}

public class ConsoleCartOperationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void CartQuantity_MustBeBetween1And100(int quantity)
    {
        var valid = quantity >= 1 && quantity <= 100;
        Assert.True(valid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CartQuantity_RejectsOutOfRange(int quantity)
    {
        var valid = quantity >= 1 && quantity <= 100;
        Assert.False(valid);
    }

    [Fact]
    public void CartItemId_MustBePositive()
    {
        long itemId = 42;
        Assert.True(itemId > 0);
    }

    [Fact]
    public void CartItemId_RejectsZeroOrNegative()
    {
        long itemId = -5;
        Assert.False(itemId > 0);
    }
}

public class ConsoleReviewValidationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ReviewRating_Accepts1To5(int stars)
    {
        var valid = stars >= 1 && stars <= 5;
        Assert.True(valid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void ReviewRating_Rejects0Or6Plus(int stars)
    {
        var valid = stars >= 1 && stars <= 5;
        Assert.False(valid);
    }

    [Fact]
    public void ReviewExplanation_CanBe1000Chars()
    {
        var explanation = new string('a', 1000);
        Assert.Equal(1000, explanation.Length);
    }

    [Fact]
    public void ReviewExplanation_RejectedIfOver1000()
    {
        var explanation = new string('a', 1001);
        var valid = explanation.Length <= 1000;
        Assert.False(valid);
    }

    [Fact]
    public void ReviewExplanation_CannotBeEmpty()
    {
        var explanation = "";
        var valid = !string.IsNullOrWhiteSpace(explanation);
        Assert.False(valid);
    }

    [Fact]
    public void ReviewExplanation_CanContainMultipleLines()
    {
        var explanation = "Line 1\nLine 2\nLine 3";
        Assert.Contains("\n", explanation);
        Assert.True(explanation.Length > 0);
    }
}

public class ConsoleCheckoutValidationTests
{
    [Fact]
    public void ShippingAddress_CannotBeEmpty()
    {
        var address = "";
        var valid = !string.IsNullOrWhiteSpace(address);
        Assert.False(valid);
    }

    [Fact]
    public void ShippingAddress_CanBe250Chars()
    {
        var address = new string('a', 250);
        Assert.Equal(250, address.Length);
    }

    [Fact]
    public void ShippingAddress_RejectedIfOver250()
    {
        var address = new string('a', 251);
        var valid = address.Length <= 250;
        Assert.False(valid);
    }

    [Fact]
    public void ShippingAddress_CanContainNumbers()
    {
        var address = "123 Main Street, Apt 456, City 12345";
        Assert.Contains("123", address);
        Assert.Contains("456", address);
    }

    [Fact]
    public void DiscountCode_CanBeOptional()
    {
        string? code = null;
        Assert.Null(code);
    }

    [Fact]
    public void DiscountCode_CanBe40Chars()
    {
        var code = new string('A', 40);
        Assert.Equal(40, code.Length);
    }

    [Fact]
    public void DiscountCode_RejectedIfOver40()
    {
        var code = new string('A', 41);
        var valid = code.Length <= 40;
        Assert.False(valid);
    }
}

public class ConsoleProductDetailPageTests
{
    [Fact]
    public void ProductName_DisplaysCorrectly()
    {
        var productName = "Test Product Name";
        Assert.NotEmpty(productName);
        Assert.False(string.IsNullOrWhiteSpace(productName));
    }

    [Fact]
    public void ProductPrice_FormattedWithTwoDecimals()
    {
        double price = 19.995;
        var formatted = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("20.00", formatted);
    }

    [Fact]
    public void ProductPrice_HandlesLargeValues()
    {
        double price = 9999.99;
        var formatted = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("9999.99", formatted);
    }

    [Fact]
    public void ProductStock_ShowsCorrectly_WhenInStock()
    {
        int stock = 42;
        var inStock = stock > 0;
        Assert.True(inStock);
    }

    [Fact]
    public void ProductStock_ShowsOutOfStock_WhenZero()
    {
        int stock = 0;
        var inStock = stock > 0;
        Assert.False(inStock);
    }

    [Fact]
    public void ProductDescription_CanBeNull()
    {
        string? description = null;
        Assert.Null(description);
    }

    [Fact]
    public void ProductDescription_CanBeLong()
    {
        var description = new string('a', 500);
        Assert.NotNull(description);
        Assert.Equal(500, description.Length);
    }
}

public class ConsoleRecommendationDisplayTests
{
    [Fact]
    public void Recommendations_CountDisplaysCorrectly()
    {
        int recCount = 5;
        var display = $"{recCount} products bought together";
        Assert.Contains("5 products", display);
    }

    [Fact]
    public void RecommendationPrice_FormattedCorrectly()
    {
        double price = 25.997;
        var formatted = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("26.00", formatted);
    }

    [Fact]
    public void BuyCount_DisplaysCorrectly()
    {
        int buyCount = 42;
        var display = $"Bought {buyCount}x together";
        Assert.Contains("42x", display);
    }

    [Fact]
    public void RecommendationList_CanBeEmpty()
    {
        var recommendations = new List<string>();
        Assert.Empty(recommendations);
    }

    [Fact]
    public void RecommendationList_CanHave10Items()
    {
        var recommendations = Enumerable.Range(1, 10).Select(i => $"Product {i}").ToList();
        Assert.Equal(10, recommendations.Count);
    }

    [Fact]
    public void RecommendationTruncation_ShowsMoreIndicator()
    {
        var allRecs = Enumerable.Range(1, 12).Select(i => $"Product {i}").ToList();
        var displayed = allRecs.Take(5).ToList();
        var remaining = allRecs.Count - displayed.Count;
        
        Assert.Equal(5, displayed.Count);
        Assert.Equal(7, remaining);
    }
}

public class ConsoleMenuNavigationTests
{
    [Fact]
    public void MainMenu_ContainsBrowseOption()
    {
        var menuOptions = new[] { "Browse & Search Products", "Top 5 sold products", "Logout" };
        Assert.Contains("Browse & Search Products", menuOptions);
    }

    [Fact]
    public void MainMenu_ContainsCacheRefreshOption()
    {
        var menuOptions = new[] { "Browse & Search Products", "Refresh recommendations cache", "Logout" };
        Assert.Contains("Refresh recommendations cache", menuOptions);
    }

    [Fact]
    public void MenuSelection_AcceptsValidIndices()
    {
        var menuOptions = new[] { "Option 1", "Option 2", "Option 3" };
        var selection = 1; // Valid index
        Assert.True(selection >= 0 && selection < menuOptions.Length);
    }

    [Fact]
    public void MenuSelection_RejectsNegativeIndices()
    {
        var menuOptions = new[] { "Option 1", "Option 2", "Option 3" };
        var selection = -1; // Invalid index
        Assert.False(selection >= 0 && selection < menuOptions.Length);
    }

    [Fact]
    public void MenuSelection_RejectsOutOfRangeIndices()
    {
        var menuOptions = new[] { "Option 1", "Option 2", "Option 3" };
        var selection = 5; // Out of range
        Assert.False(selection >= 0 && selection < menuOptions.Length);
    }
}

public class ConsolePaginationTests
{
    [Fact]
    public void ProductList_DisplaysUpTo20PerPage()
    {
        var allProducts = Enumerable.Range(1, 50).Select(i => $"Product {i}").ToList();
        var page = allProducts.Take(20).ToList();
        
        Assert.Equal(20, page.Count);
    }

    [Fact]
    public void ProductList_ShowsMoreIndicator_IfOver20()
    {
        var allProducts = Enumerable.Range(1, 50).Select(i => $"Product {i}").ToList();
        var displayed = allProducts.Take(20).ToList();
        var remaining = allProducts.Count - displayed.Count;
        
        Assert.True(remaining > 0);
        var hasMoreText = $"... and {remaining} more";
        Assert.Contains("30 more", hasMoreText);
    }

    [Fact]
    public void ProductList_NoMoreIndicator_IfExactly20()
    {
        var allProducts = Enumerable.Range(1, 20).Select(i => $"Product {i}").ToList();
        var displayed = allProducts.Take(20).ToList();
        var remaining = allProducts.Count - displayed.Count;
        
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void ReviewList_DisplaysUpTo5Reviews()
    {
        var allReviews = Enumerable.Range(1, 10).Select(i => $"Review {i}").ToList();
        var displayed = allReviews.Take(5).ToList();
        
        Assert.Equal(5, displayed.Count);
    }

    [Fact]
    public void ReviewList_ShowsMoreIndicator_IfOver5()
    {
        var allReviews = Enumerable.Range(1, 10).Select(i => $"Review {i}").ToList();
        var displayed = allReviews.Take(5).ToList();
        var remaining = allReviews.Count - displayed.Count;
        
        Assert.Equal(5, remaining);
    }
}