-- USERS
INSERT INTO users (email, password_hash, first_name, last_name, role)
VALUES
('user1@gmail.com', 'hash1', 'User', 'Lame', 0),
('admin1@gmail.com', 'hash2', 'Admin', 'Cool', 1);

-- CATEGORIES
INSERT INTO categories (name, description)
VALUES
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

-- PRODUCTS
INSERT INTO products (category_id, name, price, stock, description, brand, publisher, release_year)
VALUES
(1, 'Japans leren', 39.99, 50, 'Japans leren met Aymen', 'JapaneseCulture', 'Sung Jinwoo', 2022),
(1, 'Chinees leren', 49.99, 40, 'Chinees leren met Kynan', 'ChineseCulture', 'Ye Xiu', 2023),
(2, 'Laptop', 999.99, 10, 'Super gaming laptop', 'HP', NULL, 2024),
(1, 'Python for Data Projects', 34.95, 75, 'Practical Python and SQL examples', 'CodePress', 'Nexa Books', 2024),
(1, 'Mastering C Sharp', 44.50, 62, 'From basics to production services', 'TechReads', 'Core Editions', 2023),
(1, 'SQL Query Patterns', 29.90, 91, 'Reusable SQL patterns for apps', 'QueryWorks', 'Blue Table', 2022),
(1, 'Cloud Fundamentals', 31.25, 55, 'Modern cloud and deployment basics', 'InfraLab', 'Nimbus House', 2023),
(1, 'Design Systems Essentials', 27.80, 48, 'Build coherent product UI systems', 'UXFoundry', 'Gridline Press', 2021),
(2, 'Ultrabook 14', 1199.00, 18, 'Lightweight laptop for productivity', 'Lenovo', NULL, 2025),
(2, 'Gaming Laptop X15', 1499.00, 14, 'High FPS laptop with RTX graphics', 'Acer', NULL, 2025),
(2, '27 Inch 4K Monitor', 379.00, 35, 'Sharp IPS display with USB C', 'Dell', NULL, 2024),
(2, 'Mechanical Keyboard TKL', 89.00, 77, 'Hot swap switches and RGB', 'KeyForge', NULL, 2024),
(2, 'Wireless Mouse Pro', 59.00, 120, 'Low latency ergonomic mouse', 'LogiTech', NULL, 2025),
(2, 'USB C Dock Station', 129.00, 46, 'Dual display and ethernet dock', 'Anker', NULL, 2023),
(2, 'Portable SSD 2TB', 159.00, 64, 'Fast NVMe external storage', 'Samsung', NULL, 2025),
(3, 'Next Gen Console', 549.00, 28, '4K gaming console bundle', 'NovaPlay', NULL, 2024),
(3, 'Pro Controller', 79.00, 93, 'Wireless controller with haptics', 'NovaPlay', NULL, 2024),
(3, 'Racing Wheel Set', 249.00, 19, 'Force feedback wheel and pedals', 'DriveTech', NULL, 2023),
(3, 'Open World Adventure', 69.00, 110, 'Story rich exploration game', 'PixelPeak', 'PixelPeak Studios', 2025),
(3, 'Strategy Builder', 49.00, 85, 'Turn based strategy city builder', 'MindForge', 'MindForge Interactive', 2024),
(3, 'VR Headset Lite', 329.00, 22, 'Standalone headset for mixed reality', 'VisionBox', NULL, 2025),
(4, 'Air Fryer XL', 139.00, 39, 'Large basket air fryer with presets', 'KitchenPro', NULL, 2024),
(4, 'Espresso Machine', 289.00, 21, '15 bar pump with milk steamer', 'BaristaHome', NULL, 2023),
(4, 'Robot Vacuum S1', 219.00, 33, 'Mapping robot vacuum and mop', 'CleanBot', NULL, 2025),
(4, 'Memory Foam Pillow', 39.00, 112, 'Cooling support pillow', 'SleepWell', NULL, 2022),
(4, 'Knife Set 6 Piece', 74.00, 58, 'Stainless steel chef set', 'ChefLine', NULL, 2023),
(4, 'Smart Blender', 119.00, 45, 'High speed blender with app recipes', 'BlendGo', NULL, 2024),
(5, 'Yoga Mat Pro', 35.00, 140, 'Non slip extra thick mat', 'FlexFit', NULL, 2024),
(5, 'Dumbbell Set 20kg', 149.00, 27, 'Adjustable steel dumbbell kit', 'IronPeak', NULL, 2023),
(5, 'Running Shoes Velocity', 119.00, 66, 'Lightweight shoes for daily runs', 'SprintOne', NULL, 2025),
(5, 'Mountain Bike Helmet', 79.00, 52, 'Light helmet with dial fit system', 'TrailGuard', NULL, 2024),
(5, 'Camping Tent 3 Person', 199.00, 31, 'Waterproof quick setup tent', 'WildCamp', NULL, 2023),
(5, 'Hydration Backpack', 59.00, 73, '2 liter pack for cycling and hiking', 'SummitFlow', NULL, 2022),
(6, 'Vitamin C Serum', 22.00, 160, 'Brightening daily facial serum', 'GlowLab', NULL, 2025),
(6, 'Hydrating Face Cream', 26.00, 149, 'Moisture lock cream for all skin', 'DermaPure', NULL, 2024),
(6, 'Hair Dryer Ionic', 69.00, 57, 'Fast dry ionic technology', 'StyleAir', NULL, 2023),
(6, 'Electric Toothbrush', 49.00, 88, 'Rechargeable brush with timer', 'SmileTech', NULL, 2025),
(6, 'Shampoo Repair 500ml', 14.00, 190, 'Protein rich formula for dry hair', 'HairSense', NULL, 2022),
(6, 'Grooming Trimmer Kit', 59.00, 61, 'Multi attachment trimmer set', 'TrimFlex', NULL, 2024),
(7, 'Building Blocks 500', 59.00, 97, 'Creative construction set', 'BrickBox', NULL, 2024),
(7, 'Remote Control Car', 45.00, 83, 'Fast RC car with spare battery', 'TurboToy', NULL, 2023),
(7, 'Puzzle 2000 Pieces', 28.00, 74, 'Landscape premium puzzle', 'MindPlay', NULL, 2022),
(7, 'STEM Robot Kit', 89.00, 49, 'Learn coding with modular robot', 'EduBot', NULL, 2025),
(7, 'Board Game Family Pack', 39.00, 69, 'Classic and modern family games', 'TableFun', NULL, 2024),
(7, 'Plush Animal Set', 33.00, 118, 'Soft plush set of five animals', 'CuddleTown', NULL, 2023),
(8, 'Ergonomic Office Chair', 229.00, 37, 'Lumbar support and mesh back', 'WorkEase', NULL, 2025),
(8, 'Standing Desk 140cm', 359.00, 24, 'Electric height adjustable desk', 'LiftDesk', NULL, 2024),
(8, 'Notebook Pack 10', 19.00, 210, 'A5 lined notebooks bundle', 'PaperTrail', NULL, 2022),
(8, 'Gel Pen Set 24', 16.00, 188, 'Smooth writing color pens', 'WriteFlow', NULL, 2023),
(8, 'Monitor Arm Dual', 89.00, 44, 'Dual arm gas spring mount', 'DeskPilot', NULL, 2024),
(8, 'Webcam 1080p', 69.00, 71, 'Auto focus streaming webcam', 'VisionCam', NULL, 2025),
(9, 'Denim Jacket Classic', 89.00, 56, 'Everyday fit mid wash jacket', 'UrbanWeave', NULL, 2024),
(9, 'Sneakers Street White', 109.00, 62, 'Minimal low top sneakers', 'StepLine', NULL, 2025),
(9, 'Backpack City 20L', 59.00, 84, 'Water resistant daily backpack', 'CarryOn', NULL, 2023),
(9, 'Cotton T Shirt Pack', 29.00, 150, 'Set of three premium tees', 'BasicForm', NULL, 2022),
(9, 'Winter Scarf Knit', 25.00, 90, 'Soft knit scarf', 'NorthThread', NULL, 2024),
(9, 'Leather Belt Formal', 35.00, 73, 'Reversible leather belt', 'EdgeCraft', NULL, 2023),
(10, 'Noise Canceling Headphones', 199.00, 48, 'Over ear ANC headphones', 'SoundArc', NULL, 2025),
(10, 'Bluetooth Speaker Mini', 49.00, 127, 'Compact speaker with deep bass', 'BeatBox', NULL, 2024),
(10, 'Studio Microphone USB', 99.00, 42, 'Podcast mic with desktop stand', 'VoiceLab', NULL, 2023),
(10, 'Acoustic Guitar Starter', 179.00, 26, 'Full size guitar with bag', 'StrumCo', NULL, 2022),
(10, 'MIDI Keyboard 49 Keys', 149.00, 30, 'USB MIDI controller keyboard', 'NoteCraft', NULL, 2025),
(10, 'In Ear Monitors', 79.00, 68, 'Hi fi detachable cable earbuds', 'PureTone', NULL, 2024),
(11, 'Smart Bulb 4 Pack', 45.00, 111, 'Color bulbs with app control', 'LumiLink', NULL, 2025),
(11, 'Video Doorbell', 129.00, 39, 'HD doorbell with motion alerts', 'SafeNest', NULL, 2024),
(11, 'Smart Plug 2 Pack', 29.00, 172, 'WiFi smart plugs with timer', 'GridEase', NULL, 2023),
(11, 'Thermostat WiFi', 189.00, 22, 'Programmable smart thermostat', 'TempFlow', NULL, 2025),
(11, 'Indoor Camera 2K', 69.00, 65, 'Pan tilt camera with night vision', 'WatchHome', NULL, 2024),
(11, 'Smart Lock Secure', 219.00, 18, 'Keyless lock with app and keypad', 'LockWise', NULL, 2025),
(12, 'Digital Thermometer', 18.00, 140, 'Fast reading oral thermometer', 'MediQuick', NULL, 2023),
(12, 'Blood Pressure Monitor', 59.00, 52, 'Upper arm monitor with memory', 'HealthTrack', NULL, 2024),
(12, 'First Aid Kit 180', 39.00, 98, 'Comprehensive emergency kit', 'SafeAid', NULL, 2022),
(12, 'Foam Roller 45cm', 24.00, 88, 'Recovery roller for muscles', 'RecoverFit', NULL, 2023),
(12, 'Magnesium Supplements', 21.00, 132, '120 tablets daily support', 'NutriCore', NULL, 2025),
(12, 'Sleep Support Tea', 12.00, 167, 'Herbal tea for evening routine', 'CalmLeaf', NULL, 2024);

-- DISCOUNT CODES
INSERT INTO discount_codes (code, discount_percentage, active, valid_until)
VALUES
('DISCOUNT10', 10, 1, '2027-12-31'),
('DISCOUNT20', 20, 0, '2025-01-31'),
('WELCOME5', 5, 1, '2028-01-31'),
('SPRING15', 15, 1, '2027-05-31'),
('VIP25', 25, 1, '2027-12-31'),
('BOOKWORM12', 12, 1, '2028-06-30'),
('GAMER8', 8, 1, '2028-03-31'),
('HOME10', 10, 1, '2027-10-31');

-- CART
INSERT INTO carts (user_id)
VALUES
(1),
(2);

-- CART ITEMS
INSERT INTO cart_items (cart_id, product_id, quantity)
VALUES
(1, 1, 1),
(1, 2, 2),
(2, 10, 1),
(2, 23, 1);

-- FAVORITES
INSERT INTO favorites (user_id, product_id)
VALUES
(1, 3),
(1, 2),
(1, 45),
(2, 64),
(2, 70);

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
INSERT INTO product_ratings (user_id, product_id, rating, explanation, created_at)
VALUES
(1, 1, 5, 'Very clear and practical language lessons.', '2026-01-10 10:00:00'),
(1, 2, 4, 'Good structure, could use more exercises.', '2026-01-11 10:00:00'),
(1, 10, 5, 'Excellent performance for gaming and daily use.', '2026-01-15 11:00:00'),
(1, 64, 4, 'Great sound quality and battery life.', '2026-01-20 09:30:00'),
(2, 3, 5, 'Solid laptop and very reliable so far.', '2026-01-13 13:45:00'),
(2, 70, 5, 'Useful smart lock with easy setup.', '2026-01-21 08:15:00');
