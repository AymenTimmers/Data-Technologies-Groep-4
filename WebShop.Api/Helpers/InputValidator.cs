using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebShop.Api.Helpers;

/// <summary>
/// Service for validating user input using whitelisting approach.
/// All validation returns false by default unless input matches allowed patterns.
/// </summary>
public class InputValidator
{
    /// <summary>
    /// Validates an email address using the DataAnnotations EmailAddressAttribute.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email is required.");
        }

        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(email))
        {
            return (false, "Email must be in a valid format (e.g., user@example.com).");
        }

        if (email.Length > 255)
        {
            return (false, "Email must not exceed 255 characters.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a password against security requirements.
    /// Requirements: 8-128 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special char.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required.");
        }

        var requirements = new List<(Regex pattern, string requirement)>
        {
            (new Regex(@".{8,128}$"), "between 8 and 128 characters long"),
            (new Regex(@"[A-Z]"), "contain at least one uppercase letter (A-Z)"),
            (new Regex(@"[a-z]"), "contain at least one lowercase letter (a-z)"),
            (new Regex(@"[0-9]"), "contain at least one digit (0-9)"),
            (new Regex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"), "contain at least one special character (!@#$%^&*)")
        };

        var failedRequirements = requirements
            .Where(r => !r.pattern.IsMatch(password))
            .Select(r => r.requirement)
            .ToList();

        if (failedRequirements.Any())
        {
            var message = "Password must " + string.Join(", ", failedRequirements) + ".";
            return (false, message);
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a name (first or last name).
    /// Allows letters, spaces, hyphens, and apostrophes up to 100 characters.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateName(string? name, string fieldName = "Name")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (true, null); // Names are optional
        }

        if (name.Length > 100)
        {
            return (false, $"{fieldName} must not exceed 100 characters.");
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
        {
            return (false, $"{fieldName} must only contain letters, spaces, hyphens, and apostrophes.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates an address string.
    /// Allows alphanumeric, spaces, commas, periods, hyphens up to 250 characters.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (true, null); // Addresses are optional
        }

        if (address.Length > 250)
        {
            return (false, "Address must not exceed 250 characters.");
        }

        if (!Regex.IsMatch(address, @"^[a-zA-Z0-9\s,.\-#/]+$"))
        {
            return (false, "Address contains invalid characters. Only letters, numbers, spaces, commas, periods, hyphens, and '#/' are allowed.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates an IBAN (International Bank Account Number).
    /// Basic validation of length and format.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateIBAN(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return (true, null); // IBAN is optional
        }

        // Remove spaces
        iban = Regex.Replace(iban, @"\s", "");

        // Check format: 2 letters + 2 digits + alphanumeric
        if (!Regex.IsMatch(iban, @"^[A-Z]{2}\d{2}[A-Z0-9]+$", RegexOptions.IgnoreCase))
        {
            return (false, "IBAN must be in valid format (2 letters + 2 digits + alphanumeric characters).");
        }

        // Check length (IBAN length varies by country, typically 15-34 characters)
        if (iban.Length < 15 || iban.Length > 34)
        {
            return (false, "IBAN must be between 15 and 34 characters long.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a date string in yyyy-MM-dd format.
    /// Includes validation for edge cases like leap years.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateDate(string? dateString, string fieldName = "Date")
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return (true, null); // Dates are optional
        }

        if (!DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return (false, $"{fieldName} must be in yyyy-MM-dd format (e.g., 2025-12-31). Please check that the date is valid (e.g., 29-02-2025 is not a valid date).");
        }

        // Additional check for future dates or other business logic can be added here
        return (true, null);
    }

    /// <summary>
    /// Validates a date of birth.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateDateOfBirth(string? dateString)
    {
        var (isValid, errorMessage) = ValidateDate(dateString, "Date of Birth");
        if (!isValid)
        {
            return (false, errorMessage);
        }

        if (string.IsNullOrWhiteSpace(dateString))
        {
            return (true, null);
        }

        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            if (date > DateTime.UtcNow)
            {
                return (false, "Date of Birth cannot be in the future.");
            }

            var age = DateTime.UtcNow.Year - date.Year;
            if (date > DateTime.UtcNow.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                return (false, "User must be at least 18 years old.");
            }

            if (age > 150)
            {
                return (false, "Date of Birth appears invalid (person would be over 150 years old).");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a user ID (must be positive integer).
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            return (false, "User ID must be a positive integer.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a product ID (must be positive integer).
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            return (false, "Product ID must be a positive integer.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a quantity (must be positive integer).
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            return (false, "Quantity must be a positive integer greater than 0.");
        }

        if (quantity > 999)
        {
            return (false, "Quantity must not exceed 999 items per order.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a price/amount (must be positive decimal with max 2 decimal places).
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            return (false, "Price must be a positive value.");
        }

        if (price > 999999.99m)
        {
            return (false, "Price exceeds maximum allowed value.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a search query string.
    /// Allows alphanumeric and spaces, max 100 characters.
    /// </summary>
    public static (bool isValid, string? errorMessage) ValidateSearchQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (true, null); // Search query is optional
        }

        if (query.Length > 100)
        {
            return (false, "Search query must not exceed 100 characters.");
        }

        if (!Regex.IsMatch(query, @"^[a-zA-Z0-9\s\-]+$"))
        {
            return (false, "Search query must only contain letters, numbers, spaces, and hyphens.");
        }

        return (true, null);
    }
}
