-- USERS
CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    email TEXT,
    password_hash TEXT,
    first_name TEXT,
    last_name TEXT,
    role INTEGER
);

-- CATEGORIES
CREATE TABLE categories (
    id INTEGER PRIMARY KEY,
    name TEXT,
    description TEXT
);

-- PRODUCTS
CREATE TABLE products (
    id INTEGER PRIMARY KEY,
    category_id INTEGER,
    name TEXT,
    price REAL,
    stock INTEGER,
    description TEXT,
    brand TEXT,
    publisher TEXT,
    release_year INTEGER
);

-- CARTS
CREATE TABLE carts (
    id INTEGER PRIMARY KEY,
    user_id INTEGER
);

-- CART ITEMS
CREATE TABLE cart_items (
    id INTEGER PRIMARY KEY,
    cart_id INTEGER,
    product_id INTEGER,
    quantity INTEGER
);

-- ORDERS
CREATE TABLE orders (
    id INTEGER PRIMARY KEY,
    user_id INTEGER,
    order_number TEXT,
    total_price REAL,
    shipping_address TEXT,
    discount_code_id INTEGER
);

-- ORDER ITEMS
CREATE TABLE order_items (
    id INTEGER PRIMARY KEY,
    order_id INTEGER,
    product_id INTEGER,
    quantity INTEGER,
    price REAL
);

-- PAYMENTS
CREATE TABLE payments (
    id INTEGER PRIMARY KEY,
    order_id INTEGER,
    transaction_reference TEXT,
    total_paid REAL
);

-- DISCOUNT CODES
CREATE TABLE discount_codes (
    id INTEGER PRIMARY KEY,
    code TEXT,
    discount_percentage INTEGER,
    active INTEGER,
    valid_until TEXT
);

-- FAVORITES
CREATE TABLE favorites (
    id INTEGER PRIMARY KEY,
    user_id INTEGER,
    product_id INTEGER
);

-- PRODUCT RATINGS
CREATE TABLE product_ratings (
    id INTEGER PRIMARY KEY,
    user_id INTEGER,
    product_id INTEGER,
    rating INTEGER
);
