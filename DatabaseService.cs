using System.Globalization;
using MarketStockManager.Models;
using Microsoft.Data.Sqlite;

namespace MarketStockManager.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "marketstock.db");
        _connectionString = $"Data Source={dbPath}";
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    public void InitializeDatabase()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Products (
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

CREATE TABLE IF NOT EXISTS StockMovements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Quantity REAL NOT NULL,
    UnitCost REAL NOT NULL DEFAULT 0,
    Note TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Sales (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReceiptNumber TEXT NOT NULL UNIQUE,
    TotalAmount REAL NOT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SaleItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    UnitPrice REAL NOT NULL,
    FOREIGN KEY(SaleId) REFERENCES Sales(Id),
    FOREIGN KEY(ProductId) REFERENCES Products(Id)
);
";
        command.ExecuteNonQuery();
        SeedDataIfEmpty(connection);
    }

    private void SeedDataIfEmpty(SqliteConnection connection)
    {
        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Categories;";
        var categoryCount = Convert.ToInt32(countCommand.ExecuteScalar());

        if (categoryCount == 0)
        {
            var categories = new[]
            {
                ("Pića", "Sokovi, voda, energetska pića"),
                ("Mlečni proizvodi", "Mleko, jogurt, sir"),
                ("Pekara", "Hleb, peciva, tost"),
                ("Slatkiši", "Čokolade, keks, grickalice"),
                ("Hemija", "Kućna hemija i higijena")
            };

            foreach (var category in categories)
            {
                using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Categories (Name, Description) VALUES ($name, $description);";
                insert.Parameters.AddWithValue("$name", category.Item1);
                insert.Parameters.AddWithValue("$description", category.Item2);
                insert.ExecuteNonQuery();
            }
        }

        using var productCountCommand = connection.CreateCommand();
        productCountCommand.CommandText = "SELECT COUNT(*) FROM Products;";
        var productCount = Convert.ToInt32(productCountCommand.ExecuteScalar());

        if (productCount == 0)
        {
            var categoryIds = new Dictionary<string, int>();
            using (var categoryCommand = connection.CreateCommand())
            {
                categoryCommand.CommandText = "SELECT Id, Name FROM Categories;";
                using var categoryReader = categoryCommand.ExecuteReader();
                while (categoryReader.Read())
                    categoryIds[categoryReader.GetString(1)] = categoryReader.GetInt32(0);
            }

            int GetCategoryId(string name) => categoryIds.TryGetValue(name, out var id) ? id : categoryIds.Values.First();

            var products = new[]
            {
                new Product { Sku = "PICE-001", Barcode = "8600000000011", Name = "Gazirani sok 1.5L", CategoryId = GetCategoryId("Pića"), Unit = "kom", PurchasePrice = 92, SalePrice = 139, StockQuantity = 46, MinStock = 12 },
                new Product { Sku = "PICE-002", Barcode = "8600000000028", Name = "Mineralna voda 1.5L", CategoryId = GetCategoryId("Pića"), Unit = "kom", PurchasePrice = 38, SalePrice = 69, StockQuantity = 8, MinStock = 20 },
                new Product { Sku = "MLEK-001", Barcode = "8600000000035", Name = "Mleko 2.8% 1L", CategoryId = GetCategoryId("Mlečni proizvodi"), Unit = "kom", PurchasePrice = 104, SalePrice = 149, StockQuantity = 31, MinStock = 15 },
                new Product { Sku = "PEKA-001", Barcode = "8600000000042", Name = "Hleb beli 500g", CategoryId = GetCategoryId("Pekara"), Unit = "kom", PurchasePrice = 54, SalePrice = 89, StockQuantity = 18, MinStock = 10 },
                new Product { Sku = "SLAT-001", Barcode = "8600000000059", Name = "Čokolada 100g", CategoryId = GetCategoryId("Slatkiši"), Unit = "kom", PurchasePrice = 79, SalePrice = 129, StockQuantity = 64, MinStock = 18 },
                new Product { Sku = "HEMI-001", Barcode = "8600000000066", Name = "Deterdžent 2kg", CategoryId = GetCategoryId("Hemija"), Unit = "kom", PurchasePrice = 420, SalePrice = 599, StockQuantity = 11, MinStock = 6 }
            };

            using var transaction = connection.BeginTransaction();
            foreach (var product in products)
            {
                var id = InsertProduct(connection, transaction, product);
                InsertMovement(connection, transaction, id, "Početno stanje", product.StockQuantity,
                    product.PurchasePrice, "Demo lager za portfolio projekat");
            }
            transaction.Commit();
        }
    }

    public List<Category> GetCategories()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description FROM Categories ORDER BY Name;";
        using var reader = command.ExecuteReader();

        var categories = new List<Category>();
        while (reader.Read())
        {
            categories.Add(new Category
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString() ?? string.Empty,
                Description = reader["Description"].ToString() ?? string.Empty
            });
        }

        return categories;
    }

    public int SaveCategory(Category category)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        if (category.Id == 0)
        {
            command.CommandText = @"
INSERT INTO Categories (Name, Description) VALUES ($name, $description);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Categories SET Name = $name, Description = $description WHERE Id = $id;
SELECT $id;";
            command.Parameters.AddWithValue("$id", category.Id);
        }

        command.Parameters.AddWithValue("$name", category.Name.Trim());
        command.Parameters.AddWithValue("$description", category.Description.Trim());

        try
        {
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (SqliteException ex) when (IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Kategorija sa ovim nazivom već postoji.");
        }
    }

    public List<Product> GetProducts(string search = "", int? categoryId = null, bool lowStockOnly = false)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName, p.Unit,
       p.PurchasePrice, p.SalePrice, p.StockQuantity, p.MinStock, p.IsActive, p.CreatedAt
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
WHERE p.IsActive = 1
  AND ($search = '' OR p.Name LIKE $searchLike OR p.Sku LIKE $searchLike OR p.Barcode LIKE $searchLike)
  AND ($categoryId IS NULL OR p.CategoryId = $categoryId)
  AND ($lowStockOnly = 0 OR p.StockQuantity <= p.MinStock)
ORDER BY p.Name;";

        command.Parameters.AddWithValue("$search", search.Trim());
        command.Parameters.AddWithValue("$searchLike", $"%{search.Trim()}%");
        command.Parameters.AddWithValue("$categoryId", categoryId.HasValue ? categoryId.Value : (object)DBNull.Value);
        command.Parameters.AddWithValue("$lowStockOnly", lowStockOnly ? 1 : 0);

        using var reader = command.ExecuteReader();
        var products = new List<Product>();
        while (reader.Read())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public Product? GetProductById(int id)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName, p.Unit,
       p.PurchasePrice, p.SalePrice, p.StockQuantity, p.MinStock, p.IsActive, p.CreatedAt
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
WHERE p.Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProduct(reader) : null;
    }

    public int SaveProduct(Product product)
    {
        ValidateProduct(product);

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            int productId;

            if (product.Id == 0)
            {
                productId = InsertProduct(connection, transaction, product);

                if (product.StockQuantity > 0)
                {
                    InsertMovement(connection, transaction, productId, "Početno stanje", product.StockQuantity,
                        product.PurchasePrice, "Početno stanje pri unosu proizvoda");
                }
            }
            else
            {
                var existing = GetProductByIdInsideTransaction(connection, transaction, product.Id)
                    ?? throw new InvalidOperationException("Proizvod nije pronađen.");

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE Products
SET Sku = $sku,
    Barcode = $barcode,
    Name = $name,
    CategoryId = $categoryId,
    Unit = $unit,
    PurchasePrice = $purchasePrice,
    SalePrice = $salePrice,
    StockQuantity = $stockQuantity,
    MinStock = $minStock
WHERE Id = $id;";
                AddProductParameters(command, product);
                command.Parameters.AddWithValue("$id", product.Id);
                command.ExecuteNonQuery();
                productId = product.Id;

                // Svaka ručna izmena stanja kroz formu mora da ostane u istoriji zaliha.
                var difference = product.StockQuantity - existing.StockQuantity;
                if (difference != 0)
                {
                    InsertMovement(connection, transaction, productId,
                        difference > 0 ? "Korekcija +" : "Korekcija -", difference,
                        product.PurchasePrice, "Izmena stanja kroz formu proizvoda");
                }
            }

            transaction.Commit();
            return productId;
        }
        catch (SqliteException ex) when (IsUniqueViolation(ex))
        {
            throw new InvalidOperationException(
                "Proizvod sa ovim SKU već postoji (moguće i među obrisanim proizvodima). Unesite drugi SKU.");
        }
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Sku))
            throw new InvalidOperationException("SKU je obavezan.");
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new InvalidOperationException("Naziv proizvoda je obavezan.");
        if (product.PurchasePrice < 0 || product.SalePrice < 0)
            throw new InvalidOperationException("Cene ne mogu biti negativne.");
        if (product.StockQuantity < 0 || product.MinStock < 0)
            throw new InvalidOperationException("Zalihe ne mogu biti negativne.");
    }

    private static int InsertProduct(SqliteConnection connection, SqliteTransaction? transaction, Product product)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO Products (Sku, Barcode, Name, CategoryId, Unit, PurchasePrice, SalePrice, StockQuantity, MinStock, IsActive, CreatedAt)
VALUES ($sku, $barcode, $name, $categoryId, $unit, $purchasePrice, $salePrice, $stockQuantity, $minStock, 1, $createdAt);
SELECT last_insert_rowid();";
        AddProductParameters(command, product);
        command.Parameters.AddWithValue("$createdAt", Now());
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertMovement(SqliteConnection connection, SqliteTransaction? transaction, int productId,
        string type, decimal signedQuantity, decimal unitCost, string note)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO StockMovements (ProductId, Type, Quantity, UnitCost, Note, CreatedAt)
VALUES ($productId, $type, $quantity, $unitCost, $note, $createdAt);";
        command.Parameters.AddWithValue("$productId", productId);
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$quantity", signedQuantity);
        command.Parameters.AddWithValue("$unitCost", unitCost);
        command.Parameters.AddWithValue("$note", note.Trim());
        command.Parameters.AddWithValue("$createdAt", Now());
        command.ExecuteNonQuery();
    }

    public void DeleteProduct(int id)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void AddStockMovement(int productId, string type, decimal quantity, decimal unitCost, string note)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Količina mora biti veća od nule.");

        if (unitCost < 0)
            throw new InvalidOperationException("Nabavna cena ne može biti negativna.");

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var currentProduct = GetProductByIdInsideTransaction(connection, transaction, productId)
            ?? throw new InvalidOperationException("Proizvod nije pronađen.");

        var signedQuantity = type switch
        {
            "Ulaz robe" => quantity,
            "Korekcija +" => quantity,
            "Otpis" => -quantity,
            "Korekcija -" => -quantity,
            _ => throw new InvalidOperationException("Nepoznat tip promene zaliha.")
        };

        var newStock = currentProduct.StockQuantity + signedQuantity;
        if (newStock < 0)
            throw new InvalidOperationException("Nema dovoljno zaliha za ovu promenu.");

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE Products SET StockQuantity = $stock WHERE Id = $id;";
        update.Parameters.AddWithValue("$stock", newStock);
        update.Parameters.AddWithValue("$id", productId);
        update.ExecuteNonQuery();

        InsertMovement(connection, transaction, productId, type, signedQuantity, unitCost, note);

        transaction.Commit();
    }

    public int CreateSale(List<SaleCartItem> cartItems)
    {
        if (cartItems.Count == 0)
            throw new InvalidOperationException("Račun je prazan.");

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var item in cartItems)
        {
            var product = GetProductByIdInsideTransaction(connection, transaction, item.ProductId)
                ?? throw new InvalidOperationException($"Proizvod {item.ProductName} nije pronađen.");

            if (item.Quantity <= 0)
                throw new InvalidOperationException("Količina mora biti veća od nule.");

            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Nema dovoljno zaliha za proizvod: {product.Name}.");
        }

        var total = cartItems.Sum(x => x.LineTotal);
        var receiptNumber = $"MSM-{DateTime.Now:yyyyMMdd-HHmmss-fff}";

        using var saleCommand = connection.CreateCommand();
        saleCommand.Transaction = transaction;
        saleCommand.CommandText = @"
INSERT INTO Sales (ReceiptNumber, TotalAmount, CreatedAt)
VALUES ($receiptNumber, $totalAmount, $createdAt);
SELECT last_insert_rowid();";
        saleCommand.Parameters.AddWithValue("$receiptNumber", receiptNumber);
        saleCommand.Parameters.AddWithValue("$totalAmount", total);
        saleCommand.Parameters.AddWithValue("$createdAt", Now());
        var saleId = Convert.ToInt32(saleCommand.ExecuteScalar());

        foreach (var item in cartItems)
        {
            using var itemCommand = connection.CreateCommand();
            itemCommand.Transaction = transaction;
            itemCommand.CommandText = @"
INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice)
VALUES ($saleId, $productId, $quantity, $unitPrice);";
            itemCommand.Parameters.AddWithValue("$saleId", saleId);
            itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
            itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
            itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
            itemCommand.ExecuteNonQuery();

            using var stockCommand = connection.CreateCommand();
            stockCommand.Transaction = transaction;
            stockCommand.CommandText = "UPDATE Products SET StockQuantity = StockQuantity - $quantity WHERE Id = $productId;";
            stockCommand.Parameters.AddWithValue("$quantity", item.Quantity);
            stockCommand.Parameters.AddWithValue("$productId", item.ProductId);
            stockCommand.ExecuteNonQuery();

            InsertMovement(connection, transaction, item.ProductId, "Prodaja", -item.Quantity, 0, $"Račun {receiptNumber}");
        }

        transaction.Commit();
        return saleId;
    }

    public List<StockMovement> GetStockMovements(int limit = 200)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT sm.Id, sm.ProductId, p.Name AS ProductName, sm.Type, sm.Quantity, sm.UnitCost, sm.Note, sm.CreatedAt
FROM StockMovements sm
INNER JOIN Products p ON sm.ProductId = p.Id
ORDER BY sm.CreatedAt DESC, sm.Id DESC
LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var movements = new List<StockMovement>();
        while (reader.Read())
        {
            movements.Add(new StockMovement
            {
                Id = Convert.ToInt32(reader["Id"]),
                ProductId = Convert.ToInt32(reader["ProductId"]),
                ProductName = reader["ProductName"].ToString() ?? string.Empty,
                Type = reader["Type"].ToString() ?? string.Empty,
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                UnitCost = Convert.ToDecimal(reader["UnitCost"]),
                Note = reader["Note"].ToString() ?? string.Empty,
                CreatedAt = ParseDate(reader["CreatedAt"])
            });
        }

        return movements;
    }

    public List<Sale> GetSales(int limit = 100)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ReceiptNumber, TotalAmount, CreatedAt
FROM Sales
ORDER BY CreatedAt DESC, Id DESC
LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var sales = new List<Sale>();
        while (reader.Read())
        {
            sales.Add(new Sale
            {
                Id = Convert.ToInt32(reader["Id"]),
                ReceiptNumber = reader["ReceiptNumber"].ToString() ?? string.Empty,
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                CreatedAt = ParseDate(reader["CreatedAt"])
            });
        }

        return sales;
    }

    public DashboardStats GetDashboardStats()
    {
        using var connection = CreateConnection();
        var todayStart = DateTime.Today.ToString("O", CultureInfo.InvariantCulture);
        var tomorrowStart = DateTime.Today.AddDays(1).ToString("O", CultureInfo.InvariantCulture);

        return new DashboardStats
        {
            ProductCount = ExecuteInt(connection, "SELECT COUNT(*) FROM Products WHERE IsActive = 1;"),
            LowStockCount = ExecuteInt(connection, "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStock;"),
            StockValue = ExecuteDecimal(connection, "SELECT COALESCE(SUM(StockQuantity * PurchasePrice), 0) FROM Products WHERE IsActive = 1;"),
            TodaySales = ExecuteDecimal(connection, "SELECT COALESCE(SUM(TotalAmount), 0) FROM Sales WHERE CreatedAt >= $start AND CreatedAt < $end;", todayStart, tomorrowStart),
            TodayReceipts = ExecuteInt(connection, "SELECT COUNT(*) FROM Sales WHERE CreatedAt >= $start AND CreatedAt < $end;", todayStart, tomorrowStart),
            TotalSales = ExecuteDecimal(connection, "SELECT COALESCE(SUM(TotalAmount), 0) FROM Sales;")
        };
    }

    public List<ReportRow> GetTopSellingProducts(int limit = 10)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Name, COALESCE(SUM(si.Quantity), 0) AS Quantity, COALESCE(SUM(si.Quantity * si.UnitPrice), 0) AS Amount
FROM SaleItems si
INNER JOIN Products p ON si.ProductId = p.Id
GROUP BY p.Id, p.Name
ORDER BY Amount DESC
LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var rows = new List<ReportRow>();
        while (reader.Read())
        {
            rows.Add(new ReportRow
            {
                Name = reader["Name"].ToString() ?? string.Empty,
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                Amount = Convert.ToDecimal(reader["Amount"])
            });
        }

        return rows;
    }

    private static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = Convert.ToInt32(reader["Id"]),
            Sku = reader["Sku"].ToString() ?? string.Empty,
            Barcode = reader["Barcode"].ToString() ?? string.Empty,
            Name = reader["Name"].ToString() ?? string.Empty,
            CategoryId = Convert.ToInt32(reader["CategoryId"]),
            CategoryName = reader["CategoryName"].ToString() ?? string.Empty,
            Unit = reader["Unit"].ToString() ?? "kom",
            PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
            SalePrice = Convert.ToDecimal(reader["SalePrice"]),
            StockQuantity = Convert.ToDecimal(reader["StockQuantity"]),
            MinStock = Convert.ToDecimal(reader["MinStock"]),
            IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
            CreatedAt = ParseDate(reader["CreatedAt"])
        };
    }

    private Product? GetProductByIdInsideTransaction(SqliteConnection connection, SqliteTransaction transaction, int id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT p.Id, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName, p.Unit,
       p.PurchasePrice, p.SalePrice, p.StockQuantity, p.MinStock, p.IsActive, p.CreatedAt
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
WHERE p.Id = $id AND p.IsActive = 1;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProduct(reader) : null;
    }

    private static void AddProductParameters(SqliteCommand command, Product product)
    {
        command.Parameters.AddWithValue("$sku", product.Sku.Trim());
        command.Parameters.AddWithValue("$barcode", product.Barcode.Trim());
        command.Parameters.AddWithValue("$name", product.Name.Trim());
        command.Parameters.AddWithValue("$categoryId", product.CategoryId);
        command.Parameters.AddWithValue("$unit", product.Unit.Trim());
        command.Parameters.AddWithValue("$purchasePrice", product.PurchasePrice);
        command.Parameters.AddWithValue("$salePrice", product.SalePrice);
        command.Parameters.AddWithValue("$stockQuantity", product.StockQuantity);
        command.Parameters.AddWithValue("$minStock", product.MinStock);
    }

    private static string Now() => DateTime.Now.ToString("O", CultureInfo.InvariantCulture);

    private static DateTime ParseDate(object value)
    {
        return DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out var date)
            ? date
            : DateTime.MinValue;
    }

    private static bool IsUniqueViolation(SqliteException ex) =>
        ex.SqliteErrorCode == 19 && ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);

    private static int ExecuteInt(SqliteConnection connection, string sql, string? start = null, string? end = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (start != null) command.Parameters.AddWithValue("$start", start);
        if (end != null) command.Parameters.AddWithValue("$end", end);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static decimal ExecuteDecimal(SqliteConnection connection, string sql, string? start = null, string? end = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (start != null) command.Parameters.AddWithValue("$start", start);
        if (end != null) command.Parameters.AddWithValue("$end", end);
        return Convert.ToDecimal(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
