# Database Models and Relations

Generated at: 2026-03-27 09:38:05 UTC

## categories

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| name | TEXT | Yes | No |  |
| description | TEXT | No | No |  |

## discount_codes

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| code | TEXT | Yes | No |  |
| discount_percentage | INTEGER | Yes | No |  |
| active | INTEGER | Yes | No |  |
| valid_until | TEXT | Yes | No |  |

## favorites

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| user_id | INTEGER | Yes | No |  |
| product_id | INTEGER | Yes | No |  |

Relations:
- favorites.product_id -> products.id
- favorites.user_id -> users.id

## order_items

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| order_id | INTEGER | Yes | No |  |
| product_id | INTEGER | Yes | No |  |
| quantity | INTEGER | Yes | No |  |
| price | REAL | Yes | No |  |

Relations:
- order_items.product_id -> products.id
- order_items.order_id -> orders.id

## orders

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| user_id | INTEGER | Yes | No |  |
| order_number | TEXT | Yes | No |  |
| total_price | REAL | Yes | No |  |
| shipping_address | TEXT | Yes | No |  |
| discount_code_id | INTEGER | No | No |  |

Relations:
- orders.discount_code_id -> discount_codes.id
- orders.user_id -> users.id

## payments

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| order_id | INTEGER | Yes | No |  |
| transaction_reference | TEXT | Yes | No |  |
| total_paid | REAL | Yes | No |  |

Relations:
- payments.order_id -> orders.id

## product_ratings

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| user_id | INTEGER | Yes | No |  |
| product_id | INTEGER | Yes | No |  |
| rating | INTEGER | Yes | No |  |

Relations:
- product_ratings.product_id -> products.id
- product_ratings.user_id -> users.id

## products

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| category_id | INTEGER | Yes | No |  |
| name | TEXT | Yes | No |  |
| price | REAL | Yes | No |  |
| stock | INTEGER | Yes | No |  |
| description | TEXT | No | No |  |
| brand | TEXT | No | No |  |
| publisher | TEXT | No | No |  |
| release_year | INTEGER | No | No |  |

Relations:
- products.category_id -> categories.id

## users

| Column | Type | Not Null | PK | Default |
|---|---|---|---|---|
| id | INTEGER | No | Yes |  |
| email | TEXT | Yes | No |  |
| password_hash | TEXT | Yes | No |  |
| first_name | TEXT | No | No |  |
| last_name | TEXT | No | No |  |
| role | INTEGER | Yes | No |  |

## ER Diagram (Mermaid)

```mermaid
erDiagram
  categories {
    INTEGER id PK
    TEXT name
    TEXT description
  }
  discount_codes {
    INTEGER id PK
    TEXT code
    INTEGER discount_percentage
    INTEGER active
    TEXT valid_until
  }
  favorites {
    INTEGER id PK
    INTEGER user_id FK
    INTEGER product_id FK
  }
  order_items {
    INTEGER id PK
    INTEGER order_id FK
    INTEGER product_id FK
    INTEGER quantity
    REAL price
  }
  orders {
    INTEGER id PK
    INTEGER user_id FK
    TEXT order_number
    REAL total_price
    TEXT shipping_address
    INTEGER discount_code_id FK
  }
  payments {
    INTEGER id PK
    INTEGER order_id FK
    TEXT transaction_reference
    REAL total_paid
  }
  product_ratings {
    INTEGER id PK
    INTEGER user_id FK
    INTEGER product_id FK
    INTEGER rating
  }
  products {
    INTEGER id PK
    INTEGER category_id FK
    TEXT name
    REAL price
    INTEGER stock
    TEXT description
    TEXT brand
    TEXT publisher
    INTEGER release_year
  }
  users {
    INTEGER id PK
    TEXT email
    TEXT password_hash
    TEXT first_name
    TEXT last_name
    INTEGER role
  }
  products ||--o{ favorites : "product_id->id"
  users ||--o{ favorites : "user_id->id"
  products ||--o{ order_items : "product_id->id"
  orders ||--o{ order_items : "order_id->id"
  discount_codes ||--o{ orders : "discount_code_id->id"
  users ||--o{ orders : "user_id->id"
  orders ||--o{ payments : "order_id->id"
  products ||--o{ product_ratings : "product_id->id"
  users ||--o{ product_ratings : "user_id->id"
  categories ||--o{ products : "category_id->id"
```
