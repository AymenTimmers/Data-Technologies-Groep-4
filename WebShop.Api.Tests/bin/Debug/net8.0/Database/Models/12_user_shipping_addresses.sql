CREATE TABLE user_shipping_addresses (
    id INTEGER PRIMARY KEY,
    user_id INTEGER NOT NULL,
    label TEXT,
    shipping_address TEXT NOT NULL,
    is_default INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES users(id)
);