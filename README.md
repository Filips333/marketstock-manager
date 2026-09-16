# MarketStock Manager

![Build](https://github.com/YOUR-USERNAME/marketstock-manager/actions/workflows/build.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4)
![SQLite](https://img.shields.io/badge/database-SQLite-003B57)
![License](https://img.shields.io/badge/license-MIT-green)

**MarketStock Manager** is a Windows desktop application for managing a small shop or supermarket. It covers the full day-to-day workflow: products and categories, stock intake and write-offs, sales through a receipt/cart, a live dashboard, and sales reports, all backed by a local SQLite database that is created automatically on first launch.

> The user interface is in Serbian (prices are shown in RSD).

## Features

**Dashboard**
- Number of active products, total stock value, and low-stock count
- Today's revenue, today's receipt count, and all-time revenue
- Table of products that are at or below their minimum stock level

**Products**
- Create, edit, and archive products (soft delete, so sales history stays intact)
- SKU, barcode, category, unit of measure (`kom`, `kg`, `l`, `pak`), purchase price, sale price, and minimum stock
- Search by name, SKU, or barcode, filter by category, and show only low-stock items
- Automatic SKU generation for new products

**Stock management**
- Stock intake (`Ulaz robe`), write-off (`Otpis`), and positive/negative corrections
- Full stock movement history: every change is recorded, including initial stock and manual edits made through the product form
- Validation that prevents stock from going below zero

**Sales**
- Cart-based checkout with quantity validation against available stock
- Each sale runs in a single database transaction: the receipt, its line items, the stock deductions, and the movement records are saved together or not at all
- Unique receipt numbers (`MSM-yyyyMMdd-HHmmss-fff`)

**Reports**
- Top-selling products by revenue
- Latest receipts

## Tech stack

| Area | Technology |
|------|------------|
| Language | C# |
| Framework | .NET 8 (`net8.0-windows`) |
| UI | WPF (XAML) |
| Database | SQLite via `Microsoft.Data.Sqlite` |
| CI | GitHub Actions (Windows build) |

## Project structure

```text
marketstock-manager/
├── .github/workflows/build.yml   # CI: restore + build on windows-latest
├── MarketStockManager.sln
├── MarketStockManager.csproj
├── App.xaml / App.xaml.cs        # app styles, culture setup, global error handler
├── MainWindow.xaml / .xaml.cs    # main window with all tabs
├── DatabaseService.cs            # data access layer (schema, seeding, queries, transactions)
├── Category.cs                   # models
├── Product.cs
├── StockMovement.cs
├── Sale.cs
├── SaleItem.cs
├── SaleCartItem.cs
├── DashboardStats.cs
├── ReportRow.cs
├── database-schema.sql           # database schema reference
├── LICENSE
└── README.md
```

## Getting started

### Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (optional, with the *.NET desktop development* workload)

### Run with Visual Studio

1. Clone the repository.
2. Open `MarketStockManager.sln`.
3. Wait for NuGet packages to restore.
4. Press **F5** to run.

### Run from the terminal

```bash
git clone https://github.com/YOUR-USERNAME/marketstock-manager.git
cd marketstock-manager
dotnet restore
dotnet run
```

## Database

On first launch the app creates `marketstock.db` next to the executable, for example:

```text
bin/Debug/net8.0-windows/marketstock.db
```

It also seeds demo data so the app can be used immediately:

- **Categories:** Pića (drinks), Mlečni proizvodi (dairy), Pekara (bakery), Slatkiši (sweets), Hemija (household chemicals)
- **Products:** soft drink, mineral water, milk, bread, chocolate, and detergent, each with initial stock recorded in the movement history. Mineral water starts below its minimum stock, so the low-stock alert is visible right away.

**To reset the data**, close the app and delete `marketstock.db`. It will be recreated with fresh demo data on the next launch.

### Schema

| Table | Purpose |
|-------|---------|
| `Categories` | Product categories |
| `Products` | Product catalog, prices, and current stock |
| `StockMovements` | Audit log of every stock change |
| `Sales` | Receipts |
| `SaleItems` | Line items for each receipt |

The full schema is in [`database-schema.sql`](database-schema.sql).

## Design notes

- **Transactions:** sales, stock movements, and product edits that change stock are written atomically.
- **Audit trail:** stock is never changed without a matching entry in `StockMovements`.
- **Soft delete:** archived products are hidden from lists but kept for history and reports. Because SKUs are unique, an archived product's SKU cannot be reused.
- **Input handling:** decimal fields accept both `12,5` and `12.5`, regardless of the Windows regional settings.
- **Parameterized queries:** all SQL uses parameters, which protects against SQL injection.

## Possible improvements

- MVVM architecture with data binding and commands
- Unit tests for the data layer
- Receipt printing and PDF/Excel export
- Date-range filters for reports
- User accounts and roles (cashier / manager)

## License

This project is licensed under the [MIT License](LICENSE).
