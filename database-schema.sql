-- MarketStock Manager database schema
-- SQLite local database created automatically on first app start.

CREATE TABLE Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Sku TEXT NOT NULL UNIQUE,
    Barcode TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    Unit TEXT NOT NULL DEFAULT 'kom',
    PurchasePrice REAL NOT NULL DEFAULT 0,
    SalePrice REAL NOT NULL DEFAULT 0,
    StockQuantity REAL NOT NULL DEFAULT 0,
    MinStock REAL NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE StockMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Quantity REAL NOT NULL,
    UnitCost REAL NOT NULL DEFAULT 0,
    Note TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);

CREATE TABLE Sales (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReceiptNumber TEXT NOT NULL UNIQUE,
    TotalAmount REAL NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE SaleItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    UnitPrice REAL NOT NULL,
    FOREIGN KEY(SaleId) REFERENCES Sales(Id),
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);
