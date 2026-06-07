# WebShop Database Schema Creation and Recovery Script (Windows PowerShell)
# This script creates the database schema from scratch and can be used for disaster recovery

param(
    [string]$DbPath = "./webshop.db",
    [string]$BackupDir = "./backups"
)

Write-Host "=== WebShop Database Schema Creation Script ===" -ForegroundColor Cyan
Write-Host "Database Path: $DbPath"
Write-Host "Backup Directory: $BackupDir"
Write-Host ""

# Create backup directory if it doesn't exist
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Write-Host "Created backup directory: $BackupDir"
}

# Backup existing database if it exists
if (Test-Path $DbPath) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = Join-Path $BackupDir "webshop.db.backup.$timestamp.db"
    Write-Host "Backing up existing database to: $backupFile"
    Copy-Item $DbPath $backupFile
    Remove-Item $DbPath
    Write-Host "Database backed up successfully"
    Write-Host ""
}

# Create SQLite database with schema
Write-Host "Creating new database schema..."

$schemaSQL = @"
-- Enable foreign key constraints
PRAGMA foreign_keys = ON;

-- Users table - stores user account information
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    email TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    role INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Categories table - product categories
CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT UNIQUE NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Products table - product catalog
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    stock INTEGER DEFAULT 0,
    description TEXT,
    brand TEXT,
    publisher TEXT,
    release_year INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- Discount codes table
CREATE TABLE IF NOT EXISTS discount_codes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT UNIQUE NOT NULL,
    discount_percentage DECIMAL(5, 2) NOT NULL,
    active INTEGER DEFAULT 1,
    valid_until DATE,
    max_uses INTEGER,
    uses_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Orders table
CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status TEXT DEFAULT 'pending',
    discount_code_id INTEGER,
    total_price DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (discount_code_id) REFERENCES discount_codes(id)
);

-- Order items table
CREATE TABLE IF NOT EXISTS order_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

-- Payments table
CREATE TABLE IF NOT EXISTS payments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL UNIQUE,
    iban_encrypted TEXT,
    account_name_encrypted TEXT,
    transaction_ref TEXT,
    payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status TEXT DEFAULT 'pending',
    FOREIGN KEY (order_id) REFERENCES orders(id)
);

-- Favorites table
CREATE TABLE IF NOT EXISTS favorites (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, product_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

-- Product ratings table
CREATE TABLE IF NOT EXISTS product_ratings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
    review TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (product_id, user_id),
    FOREIGN KEY (product_id) REFERENCES products(id),
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- User shipping addresses table
CREATE TABLE IF NOT EXISTS user_shipping_addresses (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    address_encrypted TEXT NOT NULL,
    is_default INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Database metadata table
CREATE TABLE IF NOT EXISTS __db_meta (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_orders_user ON orders(user_id);
CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product ON order_items(product_id);
CREATE INDEX IF NOT EXISTS idx_favorites_user ON favorites(user_id);
CREATE INDEX IF NOT EXISTS idx_product_ratings_product ON product_ratings(product_id);

-- Insert default categories
INSERT OR IGNORE INTO categories (name, description) VALUES
('Books', 'Learning, fiction, and business books'),
('Electronics', 'Computers, devices, and accessories'),
('Gaming', 'Consoles, games, and gaming gear'),
('Home and Kitchen', 'Appliances and home essentials'),
('Sports and Outdoor', 'Fitness and outdoor equipment'),
('Beauty and Care', 'Skincare, haircare, and grooming'),
('Toys', 'Educational and fun toys for all ages'),
('Office', 'Work from home and office supplies'),
('Fashion', 'Clothing, footwear, and accessories'),
('Music and Audio', 'Headphones, speakers, and instruments'),
('Smart Home', 'Connected devices and home automation'),
('Health', 'Wellness, medical, and recovery products');
"@

# Check if sqlite3 is available
try {
    $testResult = & sqlite3 --version
    Write-Host "SQLite3 found: $testResult"
}
catch {
    Write-Host "ERROR: sqlite3 command not found. Please install SQLite3." -ForegroundColor Red
    exit 1
}

# Create the database
$schemaSQL | & sqlite3 $DbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database schema created successfully" -ForegroundColor Green
    Write-Host "✓ Default categories inserted" -ForegroundColor Green
    Write-Host ""
    Write-Host "Database is ready for use at: $DbPath"
    Write-Host "Backup saved at: $BackupDir\"
    Write-Host ""
    Write-Host "IMPORTANT: Set the ENCRYPTION_KEY environment variable:" -ForegroundColor Yellow
    Write-Host "  `$env:ENCRYPTION_KEY = [System.Convert]::ToBase64String((1..32 | ForEach-Object {Get-Random -Maximum 256}))"
} else {
    Write-Host "ERROR: Failed to create database schema" -ForegroundColor Red
    exit 1
}
