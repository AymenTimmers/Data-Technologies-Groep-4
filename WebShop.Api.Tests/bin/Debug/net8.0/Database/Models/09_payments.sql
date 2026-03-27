CREATE TABLE payments (
    id INTEGER PRIMARY KEY,
    order_id INTEGER NOT NULL,
    transaction_reference TEXT NOT NULL UNIQUE,
    total_paid REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id)
);