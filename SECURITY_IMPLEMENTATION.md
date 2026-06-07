# Security & Quality Implementation Guide

This document describes the security and quality improvements implemented in the WebShop.Api codebase.

## Implementation Status

### ✅ COMPLETED: ENCRYPTION

#### Password Encryption (bcrypt)
- **File**: [Helpers/PasswordService.cs](Helpers/PasswordService.cs)
- **Implementation**: All passwords are now hashed using bcrypt with work factor 12
- **Migration**: Old SHA-256 hashes must be rehashed when users log in
- **Usage**: Call `PasswordService.HashPassword(password)` to hash, `PasswordService.VerifyPassword(password, hash)` to verify

#### Personal Data & Sensitive Data Encryption (AES-256-CBC)
- **File**: [Helpers/EncryptionService.cs](Helpers/EncryptionService.cs)
- **Fields Encrypted**:
  - User first_name and last_name
  - User email address
  - User shipping addresses
  - Payment bank IBAN
  - Payment account name
  - Product reviews and ratings (optional sensitive content)
- **Implementation**: AES-256 in CBC mode with PKCS7 padding
- **Key Management**: Store encryption key in environment variable `ENCRYPTION_KEY` (256-bit base64 encoded)
- **Setup**: Generate key with `EncryptionService.GenerateEncryptionKey()`

### ✅ COMPLETED: RECOVERY

#### Database Schema Creation
- **Bash Script**: [Database/create-schema.sh](Database/create-schema.sh) - For Linux/macOS
- **PowerShell Script**: [Database/create-schema.ps1](Database/create-schema.ps1) - For Windows
- **Features**:
  - Creates complete database schema from scratch
  - Inserts default product categories
  - Backs up existing database before recreation
  - Supports disaster recovery scenarios

#### Automated Backups
- **File**: [Helpers/BackupService.cs](Helpers/BackupService.cs)
- **Features**:
  - Creates timestamped database backups
  - Keeps last 10 backups automatically
  - Exports SQL dumps for archiving
  - Restore from backup with safety backup fallback
  - Available backup information retrieval

### ✅ COMPLETED: HARDENING

#### Code Style & Linting
- **File**: [.editorconfig](.editorconfig)
- **Enforcement**:
  - PascalCase for public methods, types, properties
  - camelCase for private fields
  - Consistent indentation (4 spaces)
  - Consistent brace placement
  - Unused imports removed (see INPUT VALIDATION for cleanup tools)

### ✅ COMPLETED: INPUT VALIDATION

#### Whitelisting-Based Validator
- **File**: [Helpers/InputValidator.cs](Helpers/InputValidator.cs)
- **Implemented Validators**:
  - `ValidateEmail()` - Email format validation (RFC 5322)
  - `ValidatePassword()` - Password complexity: 8-128 chars, uppercase, lowercase, digit, special char
  - `ValidateName()` - Names with letters, spaces, hyphens, apostrophes (max 100 chars)
  - `ValidateAddress()` - Addresses with alphanumeric and common punctuation (max 250 chars)
  - `ValidateIBAN()` - IBAN format validation (2 letters + 2 digits + alphanumeric, 15-34 chars)
  - `ValidateDate()` - ISO 8601 format with edge case handling (e.g., 29-02-2025 = invalid)
  - `ValidateDateOfBirth()` - Date validation + age checks (18-150 years old)
  - `ValidateUserId()` - Positive integer validation
  - `ValidateProductId()` - Positive integer validation
  - `ValidateQuantity()` - Positive integer, max 999
  - `ValidatePrice()` - Positive decimal, max value checks
  - `ValidateSearchQuery()` - Alphanumeric + spaces/hyphens, max 100 chars

#### Clear User Feedback
- All validators return `(bool isValid, string? errorMessage)` tuple
- Error messages explain exactly what input is required
- Examples: "Password must contain at least one uppercase letter (A-Z), contain at least one digit (0-9)..."

### 🔄 IN PROGRESS: AUTHORIZATION & AUTHENTICATION

#### Authentication (TODO - See Implementation Notes)
**Current State**: The system currently has NO real authentication. All endpoints trust the userId parameter.

**Required Implementation**:
1. Add JWT token generation on login
2. Add JWT validation middleware
3. Remove reliance on client-supplied userId
4. Implement session/token expiration
5. Add refresh token mechanism

**Files to Update**:
- [Routes/AuthRoutes.cs](Routes/AuthRoutes.cs) - Add login endpoint with JWT generation
- [Program.cs](Program.cs) - Add JWT authentication middleware
- All route files - Add `[Authorize]` attributes and remove userId parameter trusting

#### Authorization Checks (TODO - See Implementation Notes)
**Required Implementation**:
1. Add `[Authorize]` attributes to all protected endpoints
2. Verify user authorization before action:
   - Users can only access their own profile
   - Admin role (role=1) can access admin endpoints
   - Implement admin check for all AdminRoutes
   - Users can only modify their own orders, favorites, addresses

**Example Pattern**:
```csharp
app.MapGet("/api/users/{id}/profile", async (int id, IHttpContextAccessor httpContext, IConfiguration config) =>
{
    // Validate input
    var (isValid, errorMessage) = InputValidator.ValidateUserId(id);
    if (!isValid)
        return Results.BadRequest(new { error = errorMessage });

    // Get current user from JWT token
    var currentUserId = GetCurrentUserId(httpContext.HttpContext!);
    
    // Authorization check
    if (currentUserId != id && !IsUserAdmin(currentUserId, config))
        return Results.Forbid();
    
    // Execute action
    // ...
})
.WithName("GetUserProfile")
.WithOpenApi();
```

### ✅ COMPLETED: MONITORING & LOGGING

#### Anonymous Logging
- **File**: [Helpers/RequestFileLogger.cs](Helpers/RequestFileLogger.cs)
- **Features**:
  - Sanitizes user IDs (hashed with SHA-256)
  - Removes sensitive query parameters
  - Sanitizes error messages (removes emails, IBANs, card numbers, SQL)
  - Limits error message length to 200 characters
  - No personal data in plain text in logs

### 🔄 IN PROGRESS: TESTING

#### Existing Tests
- **Location**: [WebShop.Api.Tests/](../WebShop.Api.Tests/)
- **Current Coverage**: 
  - Input validation tests
  - Discount code generation
  - Favorites table constraints

#### Required Additional Tests
1. Edge case tests for all validators
2. Date validation edge cases (leap years, month boundaries)
3. Password validation edge cases
4. Authorization failure tests
5. API endpoint integration tests
6. Database encryption/decryption tests
7. Backup/restore functionality tests

**Example Test Pattern**:
```csharp
[Fact]
public void ValidateDateOfBirth_WithFutureDate_ReturnsFalse()
{
    var futureDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
    var (isValid, _) = InputValidator.ValidateDateOfBirth(futureDate);
    Assert.False(isValid);
}

[Theory]
[InlineData("2025-02-29")] // Not a leap year
[InlineData("2024-13-01")] // Invalid month
[InlineData("2024-04-31")] // April has 30 days
public void ValidateDate_WithInvalidDates_ReturnsFalse(string dateString)
{
    var (isValid, _) = InputValidator.ValidateDate(dateString);
    Assert.False(isValid);
}
```

---

## Environment Variables Required

```bash
# Encryption key (generate with: openssl rand -base64 32)
export ENCRYPTION_KEY="<256-bit base64 encoded key>"

# Optional: Database path (defaults to ./webshop.db)
export DATABASE_PATH="./webshop.db"

# Optional: Backup directory (defaults to ./backups)
export BACKUP_DIRECTORY="./backups"
```

## Setup Instructions

### 1. Generate Encryption Key
```bash
# Linux/macOS
export ENCRYPTION_KEY=$(openssl rand -base64 32)

# Windows PowerShell
$env:ENCRYPTION_KEY = [System.Convert]::ToBase64String((1..32 | ForEach-Object {Get-Random -Maximum 256}))
```

### 2. Create Database
```bash
# Linux/macOS
bash WebShop.Api/Database/create-schema.sh

# Windows PowerShell
powershell WebShop.Api/Database/create-schema.ps1
```

### 3. Run Migrations
The `DbBootstrapper` will automatically handle schema migrations on startup.

### 4. Configure Password Changes
On user login, check if password is SHA-256 hash:
```csharp
if (hash.Length == 64) // Old SHA-256 format
{
    var newHash = PasswordService.HashPassword(plaintextPassword);
    // Update database with new hash
}
```

---

## Code Review Checklist

- [ ] All endpoints have authorization checks
- [ ] All user input is validated with `InputValidator`
- [ ] Sensitive data (names, emails, addresses, IBANs) is encrypted in database
- [ ] Passwords are verified with `PasswordService.VerifyPassword()`
- [ ] Logs contain no personal data
- [ ] Error messages don't leak sensitive information
- [ ] No unused imports (removed via linting)
- [ ] Database changes trigger automatic backups
- [ ] All tests pass including edge cases
- [ ] Performance acceptable (check backup/encryption overhead)

---

## Security Best Practices Going Forward

1. **Never log personal data** - Use the anonymous logging approach
2. **Always validate input** - Use `InputValidator` for all user input
3. **Always authorize** - Check user permissions before any action
4. **Encrypt sensitive data** - Use `EncryptionService` for personal data
5. **Hash passwords** - Always use `PasswordService`, never plain text
6. **Regular backups** - Call `BackupService.CreateBackupAsync()` periodically
7. **Key rotation** - Implement key rotation for encryption keys
8. **Audit logging** - Track sensitive operations separately for compliance
9. **Security updates** - Keep dependencies updated (run `dotnet restore --update`)
10. **Code reviews** - Enforce peer review for security-related changes

---

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [bcrypt Documentation](https://github.com/BcryptNet/bcrypt.net-next)
- [EditorConfig Standard](https://editorconfig.org/)
