-- USERS
INSERT INTO users (email, password_hash, first_name, last_name, role)
VALUES
('user1@gmail.com', 'hash1', 'User', 'Lame', 0),
('admin1@gmail.com', 'hash2', 'Admin', 'Cool', 1);

-- CATEGORIES
INSERT INTO categories (name, description)
VALUES
('Books', 'Various books to read :)'),
('Electronics', 'Electronic devices to use B)');

-- PRODUCTS
INSERT INTO products (category_id, name, price, stock, description, brand, publisher, release_year)
VALUES
(1, 'Japans leren', 39.99, 50, 'Japans leren met Aymen', 'JapaneseCulture', 'Sung Jinwoo', 2022),
(1, 'Chinees leren', 49.99, 40, 'Chinees leren met Kynan', 'ChineseCulture', 'Ye Xiu', 2023),
(2, 'Laptop', 999.99, 10, 'Super gaming laptop', 'HP', NULL, NULL);

-- DISCOUNT CODES
INSERT INTO discount_codes (code, discount_percentage, active, valid_until)
VALUES
('DISCOUNT10', 10, 1, '2026-12-31'),
('DISCOUNT20', 20, 0, '2025-01-31');

-- CART
INSERT INTO carts (user_id)
VALUES
(1);

-- CART ITEMS
INSERT INTO cart_items (cart_id, product_id, quantity)
VALUES
(1, 1, 1),
(1, 2, 2);

-- FAVORITES
INSERT INTO favorites (user_id, product_id)
VALUES
(1, 3),
(1, 2);

-- ORDERS
INSERT INTO orders (user_id, order_number, total_price, shipping_address, discount_code_id)
VALUES
(1, 'ORD001', 129.99, 'Straat Mannetje 10, Amsterdam', 1);

-- ORDER ITEMS
INSERT INTO order_items (order_id, product_id, quantity, price)
VALUES
(1, 1, 1, 39.99),
(1, 2, 2, 45.00);

-- PAYMENTS
INSERT INTO payments (order_id, transaction_reference, total_paid)
VALUES
(1, 'TXR123456', 129.99);

-- PRODUCT RATINGS
INSERT INTO product_ratings (user_id, product_id, rating)
VALUES
(1, 1, 5),
(1, 2, 4);
