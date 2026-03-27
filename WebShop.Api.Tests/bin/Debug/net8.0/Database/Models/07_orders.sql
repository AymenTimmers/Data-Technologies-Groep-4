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