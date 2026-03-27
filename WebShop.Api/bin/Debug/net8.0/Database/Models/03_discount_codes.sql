CREATE TABLE discount_codes (
    id INTEGER PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    discount_percentage INTEGER NOT NULL,
    active INTEGER NOT NULL,
    valid_until TEXT NOT NULL
);