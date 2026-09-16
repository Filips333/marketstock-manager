using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MarketStockManager.Data;
using MarketStockManager.Models;

namespace MarketStockManager;

public partial class MainWindow : Window
{
    private readonly DatabaseService _database = new();
    private readonly ObservableCollection<SaleCartItem> _cartItems = new();
    private int _selectedProductId;
    private bool _isLoading;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            _database.InitializeDatabase();
            CartGrid.ItemsSource = _cartItems;
            StockTypeComboBox.SelectedIndex = 0;
            UnitComboBox.SelectedIndex = 0;
            RefreshAll();
            ClearProductForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pokretanju aplikacije:\n{ex.Message}", "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshAll_Click(object sender, RoutedEventArgs e) => RefreshAll();

    private void RefreshAll()
    {
        _isLoading = true;
        LoadCategories();
        _isLoading = false;

        LoadProducts();
        LoadDashboard();
        LoadMovements();
        LoadReports();
        UpdateCartTotal();
    }

    private void LoadCategories()
    {
        // Pamtimo trenutni izbor da osvežavanje ne bi resetovalo filter ni formu proizvoda.
        var selectedFilter = CategoryFilterComboBox.SelectedValue as int? ?? 0;
        var selectedProductCategory = ProductCategoryComboBox.SelectedValue as int?;

        var categories = _database.GetCategories();
        var filterCategories = new List<Category> { new() { Id = 0, Name = "Sve kategorije" } };
        filterCategories.AddRange(categories);

        CategoryFilterComboBox.ItemsSource = filterCategories;
        CategoryFilterComboBox.SelectedValue = filterCategories.Any(c => c.Id == selectedFilter) ? selectedFilter : 0;

        ProductCategoryComboBox.ItemsSource = categories;
        if (selectedProductCategory.HasValue && categories.Any(c => c.Id == selectedProductCategory.Value))
            ProductCategoryComboBox.SelectedValue = selectedProductCategory.Value;
        else if (categories.Count > 0)
            ProductCategoryComboBox.SelectedIndex = 0;
    }

    private void LoadProducts()
    {
        var search = ProductSearchTextBox?.Text ?? string.Empty;
        int? categoryId = null;

        if (CategoryFilterComboBox?.SelectedValue is int selectedCategory && selectedCategory > 0)
            categoryId = selectedCategory;

        var lowOnly = LowStockOnlyCheckBox?.IsChecked == true;
        var products = _database.GetProducts(search, categoryId, lowOnly);

        ProductsGrid.ItemsSource = products;
        DashboardLowStockGrid.ItemsSource = _database.GetProducts(lowStockOnly: true);

        var comboProducts = _database.GetProducts();
        SetProductComboSource(StockProductComboBox, comboProducts);
        SetProductComboSource(SaleProductComboBox, comboProducts);
    }

    private static void SetProductComboSource(ComboBox comboBox, List<Product> products)
    {
        var previousId = comboBox.SelectedValue as int?;
        comboBox.ItemsSource = products;

        if (previousId.HasValue && products.Any(p => p.Id == previousId.Value))
            comboBox.SelectedValue = previousId.Value;
        else if (products.Count > 0)
            comboBox.SelectedIndex = 0;
    }

    private void LoadDashboard()
    {
        var stats = _database.GetDashboardStats();
        ProductCountText.Text = stats.ProductCount.ToString("N0");
        LowStockCountText.Text = stats.LowStockCount.ToString("N0");
        StockValueText.Text = FormatCurrency(stats.StockValue);
        TodaySalesText.Text = FormatCurrency(stats.TodaySales);
        TodayReceiptsText.Text = stats.TodayReceipts.ToString("N0");
        TotalSalesText.Text = FormatCurrency(stats.TotalSales);
    }

    private void LoadMovements()
    {
        MovementsGrid.ItemsSource = _database.GetStockMovements();
    }

    private void LoadReports()
    {
        TopProductsGrid.ItemsSource = _database.GetTopSellingProducts();
        SalesGrid.ItemsSource = _database.GetSales();
    }

    private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoading)
            LoadProducts();
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading)
            LoadProducts();
    }

    private void LowStockOnly_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
            LoadProducts();
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
            return;

        _selectedProductId = product.Id;
        SkuTextBox.Text = product.Sku;
        BarcodeTextBox.Text = product.Barcode;
        NameTextBox.Text = product.Name;
        ProductCategoryComboBox.SelectedValue = product.CategoryId;
        SetUnit(product.Unit);
        PurchasePriceTextBox.Text = product.PurchasePrice.ToString("0.##", CultureInfo.CurrentCulture);
        SalePriceTextBox.Text = product.SalePrice.ToString("0.##", CultureInfo.CurrentCulture);
        StockQuantityTextBox.Text = product.StockQuantity.ToString("0.##", CultureInfo.CurrentCulture);
        MinStockTextBox.Text = product.MinStock.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e) => ClearProductForm();

    private void ClearProductForm_Click(object sender, RoutedEventArgs e) => ClearProductForm();

    private void ClearProductForm()
    {
        _selectedProductId = 0;
        ProductsGrid.SelectedItem = null;
        SkuTextBox.Text = GenerateSku();
        BarcodeTextBox.Text = string.Empty;
        NameTextBox.Text = string.Empty;
        ProductCategoryComboBox.SelectedIndex = ProductCategoryComboBox.Items.Count > 0 ? 0 : -1;
        UnitComboBox.SelectedIndex = 0;
        PurchasePriceTextBox.Text = "0";
        SalePriceTextBox.Text = "0";
        StockQuantityTextBox.Text = "0";
        MinStockTextBox.Text = "0";
        NameTextBox.Focus();
    }

    private void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ProductCategoryComboBox.SelectedValue is not int categoryId || categoryId <= 0)
                throw new InvalidOperationException("Izaberite kategoriju.");

            var product = new Product
            {
                Id = _selectedProductId,
                Sku = SkuTextBox.Text,
                Barcode = BarcodeTextBox.Text,
                Name = NameTextBox.Text,
                CategoryId = categoryId,
                Unit = GetSelectedUnit(),
                PurchasePrice = ParseDecimal(PurchasePriceTextBox.Text, "Nabavna cena"),
                SalePrice = ParseDecimal(SalePriceTextBox.Text, "Prodajna cena"),
                StockQuantity = ParseDecimal(StockQuantityTextBox.Text, "Stanje lagera"),
                MinStock = ParseDecimal(MinStockTextBox.Text, "Minimalna zaliha")
            };

            _selectedProductId = _database.SaveProduct(product);
            RefreshAll();
            MessageBox.Show("Proizvod je uspešno sačuvan.", "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProductId == 0)
        {
            MessageBox.Show("Prvo izaberite proizvod za brisanje.", "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show("Da li želite da obrišete izabrani proizvod? Proizvod se arhivira i ne prikazuje u listama.",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            _database.DeleteProduct(_selectedProductId);
            ClearProductForm();
            RefreshAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddMovement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (StockProductComboBox.SelectedValue is not int productId || productId <= 0)
                throw new InvalidOperationException("Izaberite proizvod.");

            var type = GetComboBoxText(StockTypeComboBox);
            var quantity = ParseDecimal(MovementQuantityTextBox.Text, "Količina");
            var unitCost = string.IsNullOrWhiteSpace(MovementUnitCostTextBox.Text)
                ? 0
                : ParseDecimal(MovementUnitCostTextBox.Text, "Nabavna cena");

            _database.AddStockMovement(productId, type, quantity, unitCost, MovementNoteTextBox.Text);
            MovementQuantityTextBox.Text = string.Empty;
            MovementUnitCostTextBox.Text = string.Empty;
            MovementNoteTextBox.Text = string.Empty;
            RefreshAll();
            MessageBox.Show("Promena zaliha je upisana.", "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SaleProductComboBox.SelectedItem is not Product product)
                throw new InvalidOperationException("Izaberite proizvod.");

            var quantity = ParseDecimal(SaleQuantityTextBox.Text, "Količina");
            if (quantity <= 0)
                throw new InvalidOperationException("Količina mora biti veća od nule.");

            var existing = _cartItems.FirstOrDefault(x => x.ProductId == product.Id);
            var newQuantity = (existing?.Quantity ?? 0) + quantity;

            if (newQuantity > product.StockQuantity)
                throw new InvalidOperationException($"Nema dovoljno zaliha. Dostupno: {product.StockQuantity:N2} {product.Unit}.");

            if (existing == null)
            {
                _cartItems.Add(new SaleCartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    AvailableStock = product.StockQuantity,
                    Quantity = quantity,
                    UnitPrice = product.SalePrice
                });
            }
            else
            {
                existing.Quantity = newQuantity;
                CartGrid.Items.Refresh();
            }

            SaleQuantityTextBox.Text = "1";
            UpdateCartTotal();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is SaleCartItem selected)
        {
            _cartItems.Remove(selected);
            UpdateCartTotal();
        }
    }

    private void FinishSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_cartItems.Count == 0)
                throw new InvalidOperationException("Račun je prazan.");

            var total = _cartItems.Sum(x => x.LineTotal);
            var result = MessageBox.Show($"Potvrdi prodaju na iznos {FormatCurrency(total)}?",
                "Završi prodaju", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var saleId = _database.CreateSale(_cartItems.ToList());
            _cartItems.Clear();
            UpdateCartTotal();
            RefreshAll();
            MessageBox.Show($"Prodaja je završena. ID računa: {saleId}", "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateCartTotal()
    {
        CartTotalText.Text = $"Ukupno: {FormatCurrency(_cartItems.Sum(x => x.LineTotal))}";
    }

    private static decimal ParseDecimal(string value, string fieldName)
    {
        // Prihvata i "12,5" i "12.5". Separator hiljada se namerno ne dozvoljava,
        // jer bi u srpskoj kulturi "12.5" bilo pročitano kao 125.
        var normalized = (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace(',', '.');
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        if (normalized.Length > 0 &&
            decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out var result))
        {
            if (result < 0)
                throw new InvalidOperationException($"Polje '{fieldName}' ne može biti negativno.");

            return result;
        }

        throw new InvalidOperationException($"Polje '{fieldName}' nije ispravan broj.");
    }

    private static string FormatCurrency(decimal value) => $"{value:N2} RSD";

    private string GetSelectedUnit()
    {
        var text = GetComboBoxText(UnitComboBox);
        return string.IsNullOrWhiteSpace(text) ? "kom" : text;
    }

    private static string GetComboBoxText(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            ComboBoxItem item => item.Content?.ToString() ?? string.Empty,
            Product product => product.Name,
            Category category => category.Name,
            _ => comboBox.SelectedValue?.ToString() ?? string.Empty
        };
    }

    private void SetUnit(string unit)
    {
        foreach (var item in UnitComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), unit, StringComparison.OrdinalIgnoreCase))
            {
                UnitComboBox.SelectedItem = item;
                return;
            }
        }

        UnitComboBox.SelectedIndex = 0;
    }

    private static string GenerateSku() => $"ART-{DateTime.Now:yyyyMMddHHmmssfff}";
}
