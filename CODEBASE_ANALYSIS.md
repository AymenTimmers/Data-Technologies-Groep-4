# Data-Technologies-Groep-4 Codebase Analysis

## Executive Summary
This is a .NET 8 e-commerce API built with ASP.NET Core, using SQLite for persistence, Redis for cart caching, and minimal authentication (no JWT/tokens). The codebase processes sensitive personal and financial data without apparent encryption mechanisms.

---

## 1. DATABASE SCHEMA & MODELS

### Database Location & Configuration
- **Path**: `WebShop.Api/Database/Models/` (SQL schema files) + SQLite database
- **Type**: SQLite with foreign key constraints enabled
- **Connection**: [Db.cs](WebShop.Api/Helpers/Db.cs) - Creates connections with pragma `PRAGMA foreign_keys = ON`
- **Initialization**: [DbBootstrapper.cs](WebShop.Api/Helpers/DbBootstrapper.cs) - Schema fingerprinting + auto-migration
- **Csproj Reference**: Uses `Microsoft.Data.Sqlite` v10.0.3

### Complete Database Schema

#### **01_users.sql** - User Accounts
```sql
CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    bank_iban TEXT,
    bank_account_name TEXT,
    role INTEGER NOT NULL
);
```

**Personal Data Fields:**
- `email` (required) - Full email address
- `first_name` (optional) - User's first name
- `last_name` (optional) - User's last name

**Sensitive Data Fields:**
- `password_hash` (required) - User password hash (SHA-256, see Security.cs)
- `bank_iban` (optional) - Full IBAN number (unencrypted)
- `bank_account_name` (optional) - Account holder name (unencrypted)

**Authorization Field:**
- `role` (required) - 0 = user, 1 = admin

#### **02_categories.sql** - Product Categories
```sql
CREATE TABLE categories (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT
);
```

#### **03_discount_codes.sql** - Promotional Codes
```sql
CREATE TABLE discount_codes (
    id INTEGER PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    discount_percentage INTEGER NOT NULL,
    active INTEGER NOT NULL,
    valid_until TEXT NOT NULL,
    max_uses INTEGER NOT NULL DEFAULT 1,
    uses_count INTEGER NOT NULL DEFAULT 0,
    CHECK (discount_percentage BETWEEN 1 AND 90),
    CHECK (max_uses >= 1),
    CHECK (uses_count >= 0)
);
```

#### **04_products.sql** - Product Catalog
```sql
CREATE TABLE products (
    id INTEGER PRIMARY KEY,
    category_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    price REAL NOT NULL,
    stock INTEGER NOT NULL,
    description TEXT,
    brand TEXT,
    publisher TEXT,
    release_year INTEGER,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);
```

#### **07_orders.sql** - Customer Orders
```sql
CREATE TABLE orders (
    id INTEGER PRIMARY KEY,
    user_id INTEGER NOT NULL,
    order_number TEXT NOT NULL UNIQUE,
    total_price REAL NOT NULL,
    shipping_address TEXT NOT NULL,
    discount_code_id INTEGER,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (discount_code_id) REFERENCES discount_codes(id)
);
```

**Personal Data:**
- `shipping_address` (required) - Full shipping address (unencrypted text field)

#### **08_order_items.sql** - Items in Orders
```sql
CREATE TABLE order_items (
    id INTEGER PRIMARY KEY,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    price REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
```

#### **09_payments.sql** - Payment Records
```sql
CREATE TABLE payments (
    id INTEGER PRIMARY KEY,
    order_id INTEGER NOT NULL,
    transaction_reference TEXT NOT NULL UNIQUE,
    total_paid REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id)
);
```

**Note**: Transaction reference appears to be external, but payment data is stored with orders.

#### **10_favorites.sql** - User Favorites
```sql
CREATE TABLE favorites (
    id INTEGER PRIMARY KEY,
    user_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    UNIQUE (user_id, product_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
```

#### **11_product_ratings.sql** - Product Reviews
```sql
CREATE TABLE product_ratings (
    id INTEGER PRIMARY KEY,
    user_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    rating INTEGER NOT NULL,
    explanation TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    CHECK (rating BETWEEN 1 AND 5),
    UNIQUE (user_id, product_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
```

#### **12_user_shipping_addresses.sql** - Saved Shipping Addresses
```sql
CREATE TABLE user_shipping_addresses (
    id INTEGER PRIMARY KEY,
    user_id INTEGER NOT NULL,
    label TEXT,
    shipping_address TEXT NOT NULL,
    is_default INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES users(id)
);
```

**Personal Data:**
- `shipping_address` (required) - Multiple saved addresses (unencrypted)
- `label` (optional) - Address label like "Home", "Work"

### Seed Data Location
- **Path**: [WebShop.Api/Database/seed.sql](WebShop.Api/Database/seed.sql)
- **Default Users**: 
  - `user1@gmail.com` / `password123` (role 0 - regular user)
  - `admin1@gmail.com` / `admin123` (role 1 - admin)

---

## 2. AUTHENTICATION & AUTHORIZATION

### Authentication Implementation
- **File**: [WebShop.Api/Routes/AuthRoutes.cs](WebShop.Api/Routes/AuthRoutes.cs)
- **Type**: No tokens/JWT - Uses **direct password comparison** for each request
- **Password Hashing**: SHA-256 (see [Security.cs](WebShop.Api/Helpers/Security.cs))

#### Registration Endpoint: POST `/auth/register`
```csharp
public record RegisterRequest(string Email, string Password, string? FirstName, string? LastName);
```

**Validation**:
- Email must be valid format (parsed with `MailAddress`)
- Password: 6-128 characters
- First/Last Name: max 100 characters, trimmed
- Email must not already exist (UNIQUE constraint)

**Process**:
1. Normalize email to lowercase
2. Hash password with SHA-256
3. Insert user with role 0 (regular user)
4. Return `{userId, email, role}`

#### Login Endpoint: POST `/auth/login`
```csharp
public record LoginRequest(string Email, string Password);
```

**Response**:
```csharp
public record AuthResponse(long UserId, string Email, int Role);
```

**Security Issues**:
- ⚠️ No session tokens or JWT - just returns user ID and role
- ⚠️ Client must send userId in subsequent requests (trusts client)
- ⚠️ No rate limiting on login attempts
- ⚠️ Password compared as plain text to stored hash each time (not using salted bcrypt)

### Authorization Implementation
- **Files**: [AdminRoutes.cs](WebShop.Api/Routes/AdminRoutes.cs), [UserRoutes.cs](WebShop.Api/Routes/UserRoutes.cs)
- **Mechanism**: Role-based check via [Db.IsAdmin()](WebShop.Api/Helpers/Db.cs#L24)

```csharp
public static bool IsAdmin(SqliteConnection connection, long userId)
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1 FROM users WHERE id = @userId AND role = 1 LIMIT 1";
    command.Parameters.AddWithValue("@userId", userId);
    return command.ExecuteScalar() is not null;
}
```

**Admin Routes** (require `adminUserId` parameter):
- `GET /admin/users/search?adminUserId={id}&query={q}`
- `GET /admin/users/{userId}?adminUserId={id}` - Returns full profile including bank details
- Creates full `AdminUserProfileDto` with all personal/sensitive data

**User Routes** (no authorization - anyone can access if they know userId):
- `GET /users/{userId}/shipping-addresses`
- `POST /users/{userId}/shipping-addresses`
- `DELETE /users/{userId}/shipping-addresses/{addressId}`
- `GET /users/{userId}/favorites`
- `POST /users/{userId}/favorites`
- `DELETE /users/{userId}/favorites/{favId}`

**Authorization Issues**:
- ⚠️ **No authentication required** - userId is passed as URL parameter
- ⚠️ Any client can request any user's shipping addresses/favorites
- ⚠️ Admin check relies on client-supplied `adminUserId` parameter (not validated server-side token)
- ⚠️ User endpoints should check if `userId` matches authenticated user (not implemented)

---

## 3. INPUT VALIDATION

### Validation Helper: [WebShop.Api/Helpers/Input.cs](WebShop.Api/Helpers/Input.cs)

#### Email Validation
```csharp
public static bool TryNormalizeEmail(string? email, out string normalizedEmail)
{
    // - Trims and converts to lowercase
    // - Validates using System.Net.Mail.MailAddress parser
    // - Returns normalized email or false
}
```

#### Password Validation
```csharp
public static bool IsValidPassword(string? password)
{
    // - Min 6 characters, Max 128 characters
    // - Not null or whitespace
}
```

#### Optional Field Normalization
```csharp
public static string? NormalizeOptional(string? input, int maxLength)
{
    // - Trims whitespace
    // - Truncates to maxLength if needed
    // - Returns null if empty
}
```

### Validation Applied Across Routes

**Shipping Address Validation**:
- Max 250 characters
- Must not be empty or whitespace
- Label: max 40 characters (optional)

**User ID Validation**:
- Must be > 0 across all routes
- Existence check via `Db.UserExists()`

**Product ID Validation**:
- Must be > 0
- Existence check via `Db.ProductExists()`

**Search Parameters** (CatalogRoutes.cs):
- SearchTerm: wrapped in `%` for LIKE query (potential SQL injection)
- CategoryId, MinPrice, MaxPrice: typed parameters (safe)

**Data Truncation**:
- First/Last Name: 100 chars max
- Address Label: 40 chars max
- Shipping Address: 250 chars max

---

## 4. LOGGING SYSTEM

### Request Logging: [WebShop.Api/Helpers/RequestFileLogger.cs](WebShop.Api/Helpers/RequestFileLogger.cs)

**Location**: `Logs/requests.log` (created in [Program.cs](WebShop.Api/Program.cs#L49))

**Log Format**:
```
2026-06-07 14:23:45.123 UTC | POST /auth/login | status=200 | elapsedMs=42 | error=
2026-06-07 14:23:46.456 UTC | GET /users/1/shipping-addresses | status=404 | elapsedMs=12 | error=User not found.
```

**Logged Fields**:
- Timestamp (UTC)
- HTTP method + full request path + query string
- HTTP status code
- Response time (milliseconds)
- Error message (if exception occurred)

**Implementation**:
- Thread-safe logging via `lock(Sync)`
- Appends to file via `File.AppendAllText()`
- Error messages have newlines replaced with spaces
- Request/Query string may contain sensitive data (userId, etc.)

**Middleware Hook** (Program.cs):
```csharp
app.Use(async (context, next) =>
{
    var start = Stopwatch.StartNew();
    try {
        await next();
    } catch (Exception ex) {
        errorMessage = ex.Message;
        throw;
    } finally {
        RequestFileLogger.Append(...);
    }
});
```

### Logging Issues
- ⚠️ **Sensitive data in logs**: Full request paths with user IDs
- ⚠️ **No log rotation**: requests.log grows indefinitely
- ⚠️ **Exception messages logged**: May contain sensitive details
- ⚠️ **No log filtering**: All requests logged including auth endpoints

---

## 5. TESTING STRUCTURE

### Test Framework
- **File**: [WebShop.Api.Tests/WebShop.Api.Tests.csproj](WebShop.Api.Tests/WebShop.Api.Tests.csproj)
- **Framework**: xUnit v2.5.3
- **Dependencies**: 
  - Microsoft.NET.Test.SDK v17.8.0
  - coverlet.collector v6.0.0 (code coverage)
  - xunit.runner.visualstudio v2.5.3

### Existing Tests

#### [UnitTest1.cs](WebShop.Api.Tests/UnitTest1.cs) - InputTests & ProductRecommendationCacheTests

**InputTests** (6 test methods):
```csharp
public class InputTests
{
    [Theory] TryNormalizeEmail_ValidEmail_ReturnsNormalized
    [Theory] TryNormalizeEmail_InvalidEmail_ReturnsFalse
    [Theory] IsValidPassword_EnforcesLength
    [Fact] NormalizeOptional_TrimAndCutToMaxLength
    [Fact] HashPassword_SameInput_ProducesSameHash - Verifies SHA-256 deterministic
}
```

**ProductRecommendationCacheTests** (2 test methods):
```csharp
public class ProductRecommendationCacheTests
{
    [Fact] GetRecommendations_EmptyCache_ReturnsEmptyList
    [Fact] LastCacheTime_BeforeRefresh_IsMinValue - Note: Tests static state
}
```

#### [NewFeatureTests.cs](WebShop.Api.Tests/NewFeatureTests.cs) - Feature-specific tests

**DiscountCodeUsage_UpdateDeactivatesWhenLimitReached**:
- Tests discount code auto-deactivation when max_uses reached
- Creates temp SQLite DB, verifies uses_count increments and active flag toggles

**FavoritesTable_UniqueConstraintPreventsDuplicateFavorites**:
- Tests UNIQUE(user_id, product_id) constraint
- Verifies duplicate favorites cannot be inserted

### Test Patterns
- Uses temp databases created with `CreateTempDbPath()`
- Direct SQLite execution for database-layer testing
- No integration tests for API endpoints
- No authentication/authorization tests
- No validation testing for sensitive data fields

---

## 6. DATABASE CONNECTION & INITIALIZATION

### Database Setup: [WebShop.Api/Helpers/DbBootstrapper.cs](WebShop.Api/Helpers/DbBootstrapper.cs)

**Connection Process** (Program.cs):
```csharp
var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "Database");
var databasePath = Path.Combine(databaseFolder, "webshop.db");
DbBootstrapper.EnsureCreated(databasePath, databaseFolder);
builder.Services.AddSingleton(new DbOptions(databasePath));
```

**Schema Fingerprinting**:
1. Reads all `*.sql` files from `Database/Models/`
2. Computes SHA-256 fingerprint of schema + seed files
3. Stores fingerprint in `_meta` table
4. On startup: compares current fingerprint
   - If match: database is up-to-date, no action
   - If mismatch: backs up current DB, recreates from schema

**Seed Data Processing**:
```csharp
var seedSql = File.ReadAllText(seedPath)
    .Replace("'hash1'", $"'{Security.HashPassword("password123")}'")
    .Replace("'hash2'", $"'{Security.HashPassword("admin123")}'");
```
- Replaces placeholder hashes with actual SHA-256 hashes
- Uses `Security.HashPassword()` for hashing

**Connection Options**: [WebShop.Api/Models/DbOptions.cs](WebShop.Api/Models/DbOptions.cs)
```csharp
public class DbOptions
{
    public string DatabasePath { get; }
    public DbOptions(string databasePath) => DatabasePath = databasePath;
}
```

### Connection Creation: [WebShop.Api/Helpers/Db.cs](WebShop.Api/Helpers/Db.cs)

```csharp
public static SqliteConnection CreateOpenConnection(string dbPath)
{
    var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();
    
    using var pragma = connection.CreateCommand();
    pragma.CommandText = "PRAGMA foreign_keys = ON;";
    pragma.ExecuteNonQuery();
    
    return connection;
}
```

**Important**:
- Creates new connection for each request (no connection pooling)
- Enables foreign key constraints via PRAGMA
- Caller responsible for disposing connection

---

## 7. DEPENDENCIES

### WebShop.Api.csproj ([WebShop.Api/WebShop.Api.csproj](WebShop.Api/WebShop.Api.csproj))

**Project Settings**:
- Target Framework: .NET 8.0
- Nullable: enabled
- ImplicitUsings: enabled

**NuGet Packages**:
| Package | Version | Purpose |
|---------|---------|---------|
| Dapper | 2.1.72 | Micro-ORM for parameterized queries |
| Microsoft.Data.Sqlite | 10.0.3 | SQLite database driver |
| Neo4j.Driver | 6.1.2 | Graph database support (not used in this exploration) |
| StackExchange.Redis | 2.12.14 | Redis client for shopping cart |
| Swashbuckle.AspNetCore | 10.2.1 | Swagger UI documentation |

**Project References**:
- WebShop.Contracts.csproj - Shared DTOs and models

---

## 8. PASSWORD HANDLING

### Password Hashing: [WebShop.Api/Helpers/Security.cs](WebShop.Api/Helpers/Security.cs)

```csharp
public static string HashPassword(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes);
}
```

**Critical Issues**:
- ⚠️ **No salt**: Same password always produces same hash (vulnerable to rainbow tables)
- ⚠️ **SHA-256 alone**: Not designed for password hashing (too fast, no PBKDF2/bcrypt/argon2)
- ⚠️ **No pepper**: No application-level secret

### Password Validation: [WebShop.Api/Routes/AuthRoutes.cs](WebShop.Api/Routes/AuthRoutes.cs)

```csharp
public static bool IsValidPassword(string? password)
{
    return !string.IsNullOrWhiteSpace(password)
        && password.Length >= 6
        && password.Length <= 128;
}
```

**Issues**:
- ⚠️ Minimum 6 characters is weak
- ⚠️ No complexity requirements (uppercase, lowercase, numbers, symbols)
- ⚠️ Password passed in plain text over HTTP (should use HTTPS)

### Password Reset
- ⚠️ **Not implemented** - No mechanism to reset forgotten passwords

---

## 9. SENSITIVE DATA EXPOSURE

### Personal Data Fields (GDPR-relevant)
| Field | Location | Encrypted | Risk |
|-------|----------|-----------|------|
| email | users.email | No | PII exposed if DB breached |
| first_name | users.first_name | No | PII exposed if DB breached |
| last_name | users.last_name | No | PII exposed if DB breached |
| shipping_address | orders.shipping_address | No | PII exposed if DB breached |
| shipping_address | user_shipping_addresses.shipping_address | No | Multiple unencrypted copies |

### Sensitive Data Fields (Financial/Security)
| Field | Location | Encrypted | Risk |
|-------|----------|-----------|------|
| password_hash | users.password_hash | Hashed (SHA256, no salt) | Weak hashing algorithm |
| bank_iban | users.bank_iban | No | **Highly sensitive** - unencrypted |
| bank_account_name | users.bank_account_name | No | **Highly sensitive** - unencrypted |
| transaction_reference | payments.transaction_reference | No | Payment tracking data exposed |
| shipping_address | logs/requests.log | No | Logged in request paths |
| userId | logs/requests.log | No | User IDs logged with timestamps |

### Data Exposure Vectors

#### Via API Endpoints
1. **User's own data**: Accessible without authentication (URL parameter = auth)
2. **Admin data**: Accessible if client claims to be admin
   - All personal + bank details visible to admin
   - Search endpoint returns email addresses
3. **Order history**: Visible with user ID
4. **Payment history**: Visible with order details

#### Via Logging
- `requests.log` contains full request paths
- User IDs visible in every request
- No PII sanitization in logs
- File stored locally with no access controls

#### Via Database
- SQLite file stored unencrypted on disk
- All personal/sensitive data in plain text
- No database encryption/TDE
- Backups stored with `.bak` extension (mentioned in DbBootstrapper)

---

## 10. ARCHITECTURE SUMMARY

### File Structure
```
WebShop.Api/
├── Program.cs                    - Startup configuration, middleware
├── Database/
│   └── Models/                   - SQL schema files (01_users.sql, etc.)
│   └── seed.sql                  - Initial data
├── Routes/                       - API endpoint implementations
│   ├── AuthRoutes.cs
│   ├── UserRoutes.cs
│   ├── AdminRoutes.cs
│   ├── CatalogRoutes.cs
│   ├── CartAndOrderRoutes.cs
│   └── SystemRoutes.cs
├── Helpers/
│   ├── Db.cs                     - Connection factory, query helpers
│   ├── DbBootstrapper.cs         - Schema initialization
│   ├── Security.cs               - Password hashing
│   ├── Input.cs                  - Validation utilities
│   ├── RequestFileLogger.cs      - Request logging
│   ├── RedisCartStore.cs         - Cart session management
│   ├── ICartStore.cs             - Cart interface
│   ├── ProductRecommendationCache.cs
│   ├── DiscountCodeGenerator.cs
│   └── ModelDocumentationGenerator.cs
└── Models/
    └── DbOptions.cs              - Configuration container
```

### Technology Stack
- **Framework**: ASP.NET Core 8.0 (minimal APIs)
- **Database**: SQLite with Dapper ORM
- **Caching**: Redis (for shopping cart only)
- **Documentation**: Swagger/Swashbuckle
- **Testing**: xUnit
- **Authentication**: Session-less (role check only)

### Request Flow
1. Request enters through middleware chain
2. Middleware logs request before routing
3. Route handler receives request (no authentication middleware)
4. Creates SQLite connection for each request
5. Executes query with parameterized SQL
6. Returns JSON response
7. Response status logged with timing

---

## 11. SECURITY FINDINGS SUMMARY

### CRITICAL Issues
1. ⚠️ **No Authentication/Authorization** - endpoints accept `userId` parameter without verification
2. ⚠️ **Unencrypted Sensitive Data** - bank IBAN/account names stored in plain text
3. ⚠️ **Weak Password Hashing** - SHA-256 without salt (not designed for passwords)
4. ⚠️ **Client-side Trust** - Authorization checks rely on client-supplied role/userId
5. ⚠️ **No HTTPS/Transport Security** - Program.cs shows HTTP only

### HIGH Issues
1. ⚠️ **SQL Injection Risk** - SearchTerm uses LIKE with user input (though parameterized)
2. ⚠️ **No Rate Limiting** - Brute force attacks possible on auth endpoints
3. ⚠️ **Sensitive Data in Logs** - User IDs and request paths logged to disk
4. ⚠️ **Information Disclosure** - Admin can access all user bank details
5. ⚠️ **No CSRF Protection** - POST endpoints have no token validation

### MEDIUM Issues
1. ⚠️ **Weak Password Policy** - 6 character minimum, no complexity requirements
2. ⚠️ **No Data Encryption at Rest** - SQLite file unencrypted
3. ⚠️ **No Audit Trail** - No who/when/what logging for data access
4. ⚠️ **Static Test Data** - Hardcoded password hashes in seed.sql
5. ⚠️ **Connection Per Request** - No connection pooling (performance + resource issues)

---

## 12. CURRENT IMPORTS & NAMESPACES

### System Namespaces
- `System.Diagnostics` - Stopwatch for timing
- `System.Security.Cryptography` - SHA256 hashing
- `System.Net.Mail` - Email validation
- `System.Text` - UTF8 encoding

### NuGet Namespaces
- `Dapper` - Data access mapping
- `Microsoft.Data.Sqlite` - SQLite connections/commands
- `StackExchange.Redis` - Redis client
- `Swashbuckle.AspNetCore` - Swagger generation

### Project Namespaces
- `WebShop.Api.Helpers` - Utility classes
- `WebShop.Api.Models` - DTOs and configuration
- `WebShop.Api.Routes` - Endpoint definitions
- `WebShop.Contracts.Models` - Shared request/response contracts
