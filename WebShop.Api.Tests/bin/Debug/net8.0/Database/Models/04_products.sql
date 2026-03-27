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