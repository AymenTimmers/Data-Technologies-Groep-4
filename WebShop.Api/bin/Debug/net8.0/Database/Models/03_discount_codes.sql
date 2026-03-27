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