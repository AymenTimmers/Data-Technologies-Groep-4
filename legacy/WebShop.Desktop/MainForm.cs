using System.Drawing.Drawing2D;
using WebShop.Contracts.Models;
using WebShop.Desktop.Api;

namespace WebShop.Desktop;

public sealed class MainForm : Form
{
    private readonly WebShopApiClient _apiClient = new("http://localhost:5088");

    private readonly TextBox _emailTextBox = new() { PlaceholderText = "Email", Width = 220 };
    private readonly TextBox _passwordTextBox = new() { PlaceholderText = "Password", Width = 220, UseSystemPasswordChar = true };
    private readonly Button _registerButton = new() { Text = "Register", Width = 100, Height = 34 };
    private readonly Button _loginButton = new() { Text = "Sign In", Width = 100, Height = 34 };
    private readonly Button _logoutButton = new() { Text = "Logout", Width = 90, Height = 34, Enabled = false };
    private readonly Label _userLabel = new() { Text = "Not signed in", AutoSize = true };

    private readonly TabControl _tabControl = new() { Dock = DockStyle.Fill };
    private readonly TabPage _shopTab = new("Shop");
    private readonly TabPage _cartOrdersTab = new("Cart + Orders");
    private readonly TabPage _favoritesTab = new("Favorites");
    private readonly TabPage _shippingTab = new("Shipping");
    private readonly TabPage _systemTab = new("System");
    private readonly TabPage _adminTab = new("Admin");

    private readonly TextBox _shopSearchTextBox = new() { PlaceholderText = "Search by name, brand, description", Width = 280 };
    private readonly ComboBox _categoryComboBox = new() { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _minPriceTextBox = new() { PlaceholderText = "Min EUR", Width = 90 };
    private readonly TextBox _maxPriceTextBox = new() { PlaceholderText = "Max EUR", Width = 90 };
    private readonly Button _shopSearchButton = new() { Text = "Search", Width = 90, Height = 30 };
    private readonly Button _shopReloadButton = new() { Text = "View All", Width = 90, Height = 30 };
    private readonly Label _heroTitleLabel = new() { Text = "Discover Your Next Favorite", AutoSize = true };
    private readonly Label _heroSubtitleLabel = new() { Text = "Fresh picks, best sellers, and personalized recommendations.", AutoSize = true };
    private readonly Label _shopResultsLabel = new() { Text = "0 products", AutoSize = true };
    private readonly Panel _featuredPanel = new() { Height = 96, Dock = DockStyle.Top, BackColor = Color.FromArgb(245, 250, 255), Padding = new Padding(12, 8, 12, 8) };
    private readonly Label _featuredTitleLabel = new() { Text = "Featured Pick", AutoSize = true };
    private readonly Label _featuredNameLabel = new() { Text = "Loading...", AutoSize = true };
    private readonly Label _featuredMetaLabel = new() { Text = string.Empty, AutoSize = true };
    private readonly Button _featuredPrevButton = new() { Text = "<", Width = 32, Height = 30 };
    private readonly Button _featuredNextButton = new() { Text = ">", Width = 32, Height = 30 };
    private readonly Button _featuredOpenButton = new() { Text = "Open", Width = 90, Height = 30 };
    private readonly FlowLayoutPanel _productCardsPanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(8)
    };
    private readonly ListView _productsListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly TextBox _productDetailsTextBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button _addToCartButton = new() { Text = "Add To Cart", Width = 120, Height = 34 };
    private readonly Button _addFavoriteButton = new() { Text = "Add To Favorites", Width = 140, Height = 34 };
    private readonly Button _refreshProductDataButton = new() { Text = "Refresh Reviews/Recs", Width = 170, Height = 34 };
    private readonly ListView _recommendationsListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly ListView _reviewsListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly NumericUpDown _starsInput = new() { Minimum = 1, Maximum = 5, Value = 5, Width = 80 };
    private readonly TextBox _reviewExplanationTextBox = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 90, Dock = DockStyle.Fill };
    private readonly Button _submitReviewButton = new() { Text = "Submit Review", Width = 140, Height = 34 };
    private readonly Panel _miniCartPanel = new() { Height = 58, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(255, 249, 230), Padding = new Padding(12, 8, 12, 8) };
    private readonly Label _miniCartItemsLabel = new() { Text = "Cart: 0 items", AutoSize = true };
    private readonly Label _miniCartTotalLabel = new() { Text = "EUR 0.00", AutoSize = true };
    private readonly Button _openCartTabButton = new() { Text = "Open Cart", Width = 110, Height = 32 };

    private readonly ListView _cartListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly Label _cartTotalLabel = new() { Text = "Cart total: EUR 0.00", AutoSize = true };
    private readonly Button _reloadCartButton = new() { Text = "Reload Cart", Width = 110, Height = 34 };
    private readonly Button _removeCartItemButton = new() { Text = "Remove Item", Width = 110, Height = 34 };
    private readonly ComboBox _checkoutAddressComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _checkoutManualAddressTextBox = new() { PlaceholderText = "One-time address (used when no saved address is selected)", Width = 360 };
    private readonly TextBox _checkoutDiscountCodeTextBox = new() { PlaceholderText = "Discount code (optional)", Width = 160 };
    private readonly Button _checkoutButton = new() { Text = "Checkout", Width = 100, Height = 34 };
    private readonly ListView _ordersListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly Button _reloadOrdersButton = new() { Text = "Reload Orders", Width = 120, Height = 34 };

    private readonly ListView _favoritesListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly Button _reloadFavoritesButton = new() { Text = "Reload Favorites", Width = 130, Height = 34 };
    private readonly Button _removeFavoriteButton = new() { Text = "Remove Favorite", Width = 130, Height = 34 };

    private readonly ListView _shippingListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly TextBox _shippingLabelTextBox = new() { PlaceholderText = "Label (Home, Office)", Width = 150 };
    private readonly TextBox _shippingAddressTextBox = new() { PlaceholderText = "Shipping address", Width = 420 };
    private readonly CheckBox _shippingDefaultCheckBox = new() { Text = "Set as default", AutoSize = true };
    private readonly Button _reloadShippingButton = new() { Text = "Reload", Width = 90, Height = 34 };
    private readonly Button _addShippingButton = new() { Text = "Add", Width = 90, Height = 34 };
    private readonly Button _removeShippingButton = new() { Text = "Remove", Width = 90, Height = 34 };

    private readonly ListView _topSoldListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly Button _reloadTopSoldButton = new() { Text = "Reload Top Sold", Width = 130, Height = 34 };
    private readonly Button _refreshCacheButton = new() { Text = "Refresh Recs Cache", Width = 150, Height = 34 };
    private readonly Button _generateDocsButton = new() { Text = "Generate Model Docs", Width = 150, Height = 34 };

    private readonly TextBox _adminSearchTextBox = new() { PlaceholderText = "Search users by email/name", Width = 260 };
    private readonly Button _adminSearchButton = new() { Text = "Search", Width = 90, Height = 34 };
    private readonly ListView _adminUsersListView = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, Dock = DockStyle.Fill };
    private readonly Button _adminLoadProfileButton = new() { Text = "Load Profile", Width = 110, Height = 34 };
    private readonly TextBox _adminProfileTextBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly NumericUpDown _discountPercentInput = new() { Minimum = 1, Maximum = 90, Value = 10, Width = 70 };
    private readonly NumericUpDown _discountMaxUsesInput = new() { Minimum = 1, Maximum = 1000000, Value = 100, Width = 90 };
    private readonly DateTimePicker _discountValidUntilPicker = new() { Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today.AddDays(30) };
    private readonly Button _createDiscountButton = new() { Text = "Create Discount", Width = 130, Height = 34 };

    private readonly Label _statusLabel = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft };

    private long _signedInUserId;
    private int _signedInUserRole;
    private string? _signedInUserEmail;
    private ProductDto? _selectedProduct;
    private readonly List<ProductDto> _catalogProducts = new();
    private int _featuredIndex;
    private readonly System.Windows.Forms.Timer _featuredTimer = new() { Interval = 4000 };

    private sealed record CategoryChoice(long? Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record AddressChoice(long? Id, string Display)
    {
        public override string ToString() => Display;
    }

    public MainForm()
    {
        Text = "WebShop Desktop - Full Frontend";
        MinimumSize = new Size(1320, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(247, 244, 235);
        Font = new Font("Trebuchet MS", 10f, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();
        ConfigureEvents();
        ApplyVisualTheme();

        Shown += async (_, _) => await InitializeDataAsync();
    }

    private void ApplyVisualTheme()
    {
        _tabControl.Appearance = TabAppearance.FlatButtons;
        _tabControl.ItemSize = new Size(120, 36);
        _tabControl.SizeMode = TabSizeMode.Fixed;

        _heroTitleLabel.Font = new Font("Trebuchet MS", 22f, FontStyle.Bold, GraphicsUnit.Point);
        _heroSubtitleLabel.ForeColor = Color.FromArgb(84, 92, 118);
        _heroSubtitleLabel.Font = new Font("Trebuchet MS", 10f, FontStyle.Italic, GraphicsUnit.Point);
        _shopResultsLabel.Font = new Font("Trebuchet MS", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _shopResultsLabel.ForeColor = Color.FromArgb(58, 82, 117);
        _featuredTitleLabel.Font = new Font("Trebuchet MS", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _featuredNameLabel.Font = new Font("Trebuchet MS", 13f, FontStyle.Bold, GraphicsUnit.Point);
        _featuredMetaLabel.Font = new Font("Trebuchet MS", 9f, FontStyle.Regular, GraphicsUnit.Point);
        _featuredMetaLabel.ForeColor = Color.FromArgb(73, 85, 110);
        _miniCartItemsLabel.Font = new Font("Trebuchet MS", 10f, FontStyle.Bold, GraphicsUnit.Point);
        _miniCartTotalLabel.Font = new Font("Trebuchet MS", 12f, FontStyle.Bold, GraphicsUnit.Point);
        _miniCartTotalLabel.ForeColor = Color.FromArgb(45, 76, 128);

        foreach (var button in AllButtons())
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(204, 211, 227);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(42, 53, 72);
        }

        _shopSearchButton.BackColor = Color.FromArgb(255, 195, 102);
        _shopReloadButton.BackColor = Color.FromArgb(236, 243, 255);
        _featuredOpenButton.BackColor = Color.FromArgb(255, 195, 102);
        _addToCartButton.BackColor = Color.FromArgb(255, 195, 102);
        _submitReviewButton.BackColor = Color.FromArgb(255, 195, 102);
        _checkoutButton.BackColor = Color.FromArgb(255, 195, 102);
        _createDiscountButton.BackColor = Color.FromArgb(255, 195, 102);
        _openCartTabButton.BackColor = Color.FromArgb(255, 234, 171);
    }

    private IEnumerable<Button> AllButtons()
    {
        yield return _registerButton;
        yield return _loginButton;
        yield return _logoutButton;
        yield return _shopSearchButton;
        yield return _shopReloadButton;
        yield return _featuredPrevButton;
        yield return _featuredNextButton;
        yield return _featuredOpenButton;
        yield return _addToCartButton;
        yield return _addFavoriteButton;
        yield return _refreshProductDataButton;
        yield return _submitReviewButton;
        yield return _openCartTabButton;
        yield return _reloadCartButton;
        yield return _removeCartItemButton;
        yield return _checkoutButton;
        yield return _reloadOrdersButton;
        yield return _reloadFavoritesButton;
        yield return _removeFavoriteButton;
        yield return _reloadShippingButton;
        yield return _addShippingButton;
        yield return _removeShippingButton;
        yield return _reloadTopSoldButton;
        yield return _refreshCacheButton;
        yield return _generateDocsButton;
        yield return _adminSearchButton;
        yield return _adminLoadProfileButton;
        yield return _createDiscountButton;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle,
            Color.FromArgb(255, 244, 214),
            Color.FromArgb(230, 238, 255),
            LinearGradientMode.ForwardDiagonal);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    private void BuildLayout()
    {
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 12, 12, 8)
        };
        topPanel.Controls.Add(_emailTextBox);
        topPanel.Controls.Add(_passwordTextBox);
        topPanel.Controls.Add(_registerButton);
        topPanel.Controls.Add(_loginButton);
        topPanel.Controls.Add(_logoutButton);
        topPanel.Controls.Add(_userLabel);

        BuildShopTab();
        BuildCartOrdersTab();
        BuildFavoritesTab();
        BuildShippingTab();
        BuildSystemTab();
        BuildAdminTab();

        _tabControl.TabPages.Add(_shopTab);
        _tabControl.TabPages.Add(_cartOrdersTab);
        _tabControl.TabPages.Add(_favoritesTab);
        _tabControl.TabPages.Add(_shippingTab);
        _tabControl.TabPages.Add(_systemTab);
        _tabControl.TabPages.Add(_adminTab);
        _adminTab.Enabled = false;

        Controls.Add(_tabControl);
        Controls.Add(topPanel);
        Controls.Add(_statusLabel);
        SetStatus("Ready. Register or sign in to use all features.");
    }

    private void BuildShopTab()
    {
        _reviewsListView.Columns.Add("User", 210);
        _reviewsListView.Columns.Add("Stars", 60);
        _reviewsListView.Columns.Add("Created", 150);
        _reviewsListView.Columns.Add("Explanation", 410);

        _recommendationsListView.Columns.Add("Product", 220);
        _recommendationsListView.Columns.Add("Price", 90);
        _recommendationsListView.Columns.Add("Stock", 70);
        _recommendationsListView.Columns.Add("BuyCount", 90);

        _productDetailsTextBox.BackColor = Color.White;
        _productDetailsTextBox.BorderStyle = BorderStyle.None;
        _productDetailsTextBox.Font = new Font("Trebuchet MS", 10f, FontStyle.Regular, GraphicsUnit.Point);

        var searchBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(12, 8, 12, 4),
            BackColor = Color.FromArgb(255, 252, 245)
        };
        searchBar.Controls.Add(_shopSearchTextBox);
        searchBar.Controls.Add(_categoryComboBox);
        searchBar.Controls.Add(_minPriceTextBox);
        searchBar.Controls.Add(_maxPriceTextBox);
        searchBar.Controls.Add(_shopSearchButton);
        searchBar.Controls.Add(_shopReloadButton);

        var heroPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.FromArgb(255, 252, 245),
            Padding = new Padding(14, 12, 14, 8)
        };
        _heroTitleLabel.Location = new Point(12, 8);
        _heroSubtitleLabel.Location = new Point(14, 50);
        _shopResultsLabel.Location = new Point(14, 78);
        heroPanel.Controls.Add(_heroTitleLabel);
        heroPanel.Controls.Add(_heroSubtitleLabel);
        heroPanel.Controls.Add(_shopResultsLabel);

        var featuredActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 260,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        featuredActions.Controls.Add(_featuredPrevButton);
        featuredActions.Controls.Add(_featuredNextButton);
        featuredActions.Controls.Add(_featuredOpenButton);

        var featuredInfo = new Panel { Dock = DockStyle.Fill };
        _featuredTitleLabel.Location = new Point(0, 4);
        _featuredNameLabel.Location = new Point(0, 26);
        _featuredMetaLabel.Location = new Point(0, 56);
        featuredInfo.Controls.Add(_featuredTitleLabel);
        featuredInfo.Controls.Add(_featuredNameLabel);
        featuredInfo.Controls.Add(_featuredMetaLabel);

        _featuredPanel.Controls.Add(featuredInfo);
        _featuredPanel.Controls.Add(featuredActions);

        var shopSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 730 };
        var rightSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 240 };

        var productActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 8, 8, 4),
            BackColor = Color.FromArgb(255, 252, 245)
        };
        productActions.Controls.Add(_addToCartButton);
        productActions.Controls.Add(_addFavoriteButton);
        productActions.Controls.Add(_refreshProductDataButton);

        var reviewInput = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 138,
            ColumnCount = 2
        };
        reviewInput.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        reviewInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        reviewInput.Controls.Add(new Label { Text = "Stars", AutoSize = true, Margin = new Padding(8, 10, 10, 0) }, 0, 0);
        reviewInput.Controls.Add(_starsInput, 1, 0);
        reviewInput.Controls.Add(new Label { Text = "Review", AutoSize = true, Margin = new Padding(8, 10, 10, 0) }, 0, 1);
        reviewInput.Controls.Add(_reviewExplanationTextBox, 1, 1);
        reviewInput.Controls.Add(_submitReviewButton, 1, 2);

        var detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
        detailPanel.Controls.Add(_productDetailsTextBox);

        rightSplit.Panel1.Controls.Add(detailPanel);
        rightSplit.Panel2.Controls.Add(_reviewsListView);
        rightSplit.Panel2.Controls.Add(reviewInput);

        var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(252, 248, 238) };
        leftPanel.Controls.Add(_productCardsPanel);

        var recommendationPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 220,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        var recommendationTitle = new Label
        {
            Text = "Customers Also Bought",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 30,
            Font = new Font("Trebuchet MS", 11f, FontStyle.Bold, GraphicsUnit.Point)
        };
        recommendationPanel.Controls.Add(_recommendationsListView);
        recommendationPanel.Controls.Add(recommendationTitle);

        shopSplit.Panel1.Controls.Add(leftPanel);
        shopSplit.Panel2.Controls.Add(rightSplit);
        shopSplit.Panel2.Controls.Add(_miniCartPanel);
        shopSplit.Panel2.Controls.Add(recommendationPanel);
        shopSplit.Panel2.Controls.Add(productActions);

        _miniCartPanel.Controls.Add(_openCartTabButton);
        _miniCartPanel.Controls.Add(_miniCartTotalLabel);
        _miniCartPanel.Controls.Add(_miniCartItemsLabel);
        _openCartTabButton.Dock = DockStyle.Right;
        _miniCartTotalLabel.Location = new Point(140, 18);
        _miniCartItemsLabel.Location = new Point(12, 20);

        _shopTab.Controls.Add(shopSplit);
        _shopTab.Controls.Add(_featuredPanel);
        _shopTab.Controls.Add(heroPanel);
        _shopTab.Controls.Add(searchBar);
    }

    private void BuildCartOrdersTab()
    {
        _cartListView.Columns.Add("ItemId", 70);
        _cartListView.Columns.Add("Product", 260);
        _cartListView.Columns.Add("Qty", 60);
        _cartListView.Columns.Add("Unit", 90);
        _cartListView.Columns.Add("Line", 90);

        _ordersListView.Columns.Add("OrderId", 70);
        _ordersListView.Columns.Add("OrderNumber", 180);
        _ordersListView.Columns.Add("Total", 90);
        _ordersListView.Columns.Add("ShippingAddress", 460);

        var root = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };

        var cartActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 2)
        };
        cartActions.Controls.Add(_reloadCartButton);
        cartActions.Controls.Add(_removeCartItemButton);
        cartActions.Controls.Add(_cartTotalLabel);

        var checkoutPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Padding = new Padding(8, 8, 8, 6)
        };
        checkoutPanel.Controls.Add(_checkoutAddressComboBox);
        checkoutPanel.Controls.Add(_checkoutManualAddressTextBox);
        checkoutPanel.Controls.Add(_checkoutDiscountCodeTextBox);
        checkoutPanel.Controls.Add(_checkoutButton);

        root.Panel1.Controls.Add(_cartListView);
        root.Panel1.Controls.Add(checkoutPanel);
        root.Panel1.Controls.Add(cartActions);

        var ordersActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 2)
        };
        ordersActions.Controls.Add(_reloadOrdersButton);

        root.Panel2.Controls.Add(_ordersListView);
        root.Panel2.Controls.Add(ordersActions);

        _cartOrdersTab.Controls.Add(root);
    }

    private void BuildFavoritesTab()
    {
        _favoritesListView.Columns.Add("FavoriteId", 80);
        _favoritesListView.Columns.Add("ProductId", 70);
        _favoritesListView.Columns.Add("Product", 320);
        _favoritesListView.Columns.Add("Price", 90);
        _favoritesListView.Columns.Add("Stock", 80);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 2)
        };
        actions.Controls.Add(_reloadFavoritesButton);
        actions.Controls.Add(_removeFavoriteButton);

        _favoritesTab.Controls.Add(_favoritesListView);
        _favoritesTab.Controls.Add(actions);
    }

    private void BuildShippingTab()
    {
        _shippingListView.Columns.Add("Id", 70);
        _shippingListView.Columns.Add("Label", 180);
        _shippingListView.Columns.Add("Address", 520);
        _shippingListView.Columns.Add("Default", 80);

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 74,
            Padding = new Padding(8, 8, 8, 6)
        };
        controls.Controls.Add(_shippingLabelTextBox);
        controls.Controls.Add(_shippingAddressTextBox);
        controls.Controls.Add(_shippingDefaultCheckBox);
        controls.Controls.Add(_reloadShippingButton);
        controls.Controls.Add(_addShippingButton);
        controls.Controls.Add(_removeShippingButton);

        _shippingTab.Controls.Add(_shippingListView);
        _shippingTab.Controls.Add(controls);
    }

    private void BuildSystemTab()
    {
        _topSoldListView.Columns.Add("ProductId", 90);
        _topSoldListView.Columns.Add("Product", 300);
        _topSoldListView.Columns.Add("SoldQty", 90);
        _topSoldListView.Columns.Add("Revenue", 110);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 2)
        };
        actions.Controls.Add(_reloadTopSoldButton);
        actions.Controls.Add(_refreshCacheButton);
        actions.Controls.Add(_generateDocsButton);

        _systemTab.Controls.Add(_topSoldListView);
        _systemTab.Controls.Add(actions);
    }

    private void BuildAdminTab()
    {
        _adminUsersListView.Columns.Add("UserId", 70);
        _adminUsersListView.Columns.Add("Email", 240);
        _adminUsersListView.Columns.Add("Name", 220);
        _adminUsersListView.Columns.Add("Role", 70);

        var root = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 280 };

        var searchBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 6, 8, 2)
        };
        searchBar.Controls.Add(_adminSearchTextBox);
        searchBar.Controls.Add(_adminSearchButton);
        searchBar.Controls.Add(_adminLoadProfileButton);

        root.Panel1.Controls.Add(_adminUsersListView);
        root.Panel1.Controls.Add(searchBar);

        var discountPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(8, 6, 8, 2)
        };
        discountPanel.Controls.Add(new Label { Text = "Discount %", AutoSize = true, Margin = new Padding(0, 9, 8, 0) });
        discountPanel.Controls.Add(_discountPercentInput);
        discountPanel.Controls.Add(new Label { Text = "Max Uses", AutoSize = true, Margin = new Padding(12, 9, 8, 0) });
        discountPanel.Controls.Add(_discountMaxUsesInput);
        discountPanel.Controls.Add(new Label { Text = "Valid Until", AutoSize = true, Margin = new Padding(12, 9, 8, 0) });
        discountPanel.Controls.Add(_discountValidUntilPicker);
        discountPanel.Controls.Add(_createDiscountButton);

        root.Panel2.Controls.Add(_adminProfileTextBox);
        root.Panel2.Controls.Add(discountPanel);

        _adminTab.Controls.Add(root);
    }

    private void ConfigureEvents()
    {
        _registerButton.Click += async (_, _) => await RegisterAsync();
        _loginButton.Click += async (_, _) => await LoginAsync();
        _logoutButton.Click += (_, _) => Logout();

        _shopSearchButton.Click += async (_, _) => await SearchProductsAsync();
        _shopReloadButton.Click += async (_, _) => await LoadProductsAsync();
        _submitReviewButton.Click += async (_, _) => await SubmitReviewAsync();
        _featuredPrevButton.Click += async (_, _) => await ShowRelativeFeaturedAsync(-1);
        _featuredNextButton.Click += async (_, _) => await ShowRelativeFeaturedAsync(1);
        _featuredOpenButton.Click += async (_, _) => await OpenFeaturedAsync();
        _openCartTabButton.Click += (_, _) => _tabControl.SelectedTab = _cartOrdersTab;
        _featuredTimer.Tick += async (_, _) => await ShowRelativeFeaturedAsync(1);

        _addToCartButton.Click += async (_, _) => await AddSelectedProductToCartAsync();
        _addFavoriteButton.Click += async (_, _) => await AddSelectedProductToFavoritesAsync();
        _refreshProductDataButton.Click += async (_, _) => await RefreshSelectedProductDataAsync();

        _reloadCartButton.Click += async (_, _) => await LoadCartAsync();
        _removeCartItemButton.Click += async (_, _) => await RemoveSelectedCartItemAsync();
        _checkoutButton.Click += async (_, _) => await CheckoutAsync();
        _reloadOrdersButton.Click += async (_, _) => await LoadOrdersAsync();

        _reloadFavoritesButton.Click += async (_, _) => await LoadFavoritesAsync();
        _removeFavoriteButton.Click += async (_, _) => await RemoveSelectedFavoriteAsync();

        _reloadShippingButton.Click += async (_, _) => await LoadShippingAddressesAsync();
        _addShippingButton.Click += async (_, _) => await AddShippingAddressAsync();
        _removeShippingButton.Click += async (_, _) => await RemoveSelectedShippingAddressAsync();

        _reloadTopSoldButton.Click += async (_, _) => await LoadTopSoldAsync();
        _refreshCacheButton.Click += async (_, _) => await RefreshCacheAsync();
        _generateDocsButton.Click += async (_, _) => await GenerateDocsAsync();

        _adminSearchButton.Click += async (_, _) => await AdminSearchUsersAsync();
        _adminLoadProfileButton.Click += async (_, _) => await AdminLoadSelectedProfileAsync();
        _createDiscountButton.Click += async (_, _) => await AdminCreateDiscountAsync();
    }

    private async Task InitializeDataAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
        await LoadTopSoldAsync();
    }

    private async Task RegisterAsync()
    {
        try
        {
            SetBusy(true);
            var email = _emailTextBox.Text.Trim();
            var password = _passwordTextBox.Text;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Email and password are required.");
                return;
            }

            await _apiClient.RegisterAsync(email, password);
            SetStatus("Registration successful. You can now sign in.");
        }
        catch (Exception ex)
        {
            SetStatus($"Registration failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoginAsync()
    {
        try
        {
            SetBusy(true);
            var email = _emailTextBox.Text.Trim();
            var password = _passwordTextBox.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Email and password are required.");
                return;
            }

            var auth = await _apiClient.LoginAsync(email, password);
            if (auth is null)
            {
                SetStatus("Login failed. Check your credentials.");
                return;
            }

            _signedInUserId = auth.UserId;
            _signedInUserRole = auth.Role;
            _signedInUserEmail = auth.Email;
            _userLabel.Text = $"Signed in: {auth.Email} (role {auth.Role})";
            _logoutButton.Enabled = true;
            _adminTab.Enabled = auth.Role == 1;

            await LoadShippingAddressesAsync();
            await LoadCartAsync();
            await LoadOrdersAsync();
            await LoadFavoritesAsync();

            SetStatus("Login successful.");
        }
        catch (Exception ex)
        {
            SetStatus($"Login error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Logout()
    {
        _signedInUserId = 0;
        _signedInUserRole = 0;
        _signedInUserEmail = null;
        _logoutButton.Enabled = false;
        _adminTab.Enabled = false;
        _userLabel.Text = "Not signed in";

        _cartListView.Items.Clear();
        _ordersListView.Items.Clear();
        _favoritesListView.Items.Clear();
        _shippingListView.Items.Clear();
        _checkoutAddressComboBox.Items.Clear();
        _checkoutAddressComboBox.Items.Add(new AddressChoice(null, "Manual one-time address"));
        _checkoutAddressComboBox.SelectedIndex = 0;
        _adminUsersListView.Items.Clear();
        _adminProfileTextBox.Clear();
        _miniCartItemsLabel.Text = "Cart: sign in";
        _miniCartTotalLabel.Text = "EUR 0.00";

        SetStatus("Logged out.");
    }

    private bool EnsureSignedIn(string action)
    {
        if (_signedInUserId > 0)
        {
            return true;
        }

        SetStatus($"Sign in first to {action}.");
        return false;
    }

    private bool EnsureAdmin()
    {
        if (_signedInUserId > 0 && _signedInUserRole == 1)
        {
            return true;
        }

        SetStatus("Admin access required.");
        return false;
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _apiClient.GetCategoriesAsync();
            _categoryComboBox.Items.Clear();
            _categoryComboBox.Items.Add(new CategoryChoice(null, "All categories"));
            foreach (var category in categories)
            {
                _categoryComboBox.Items.Add(new CategoryChoice(category.Id, category.Name));
            }
            _categoryComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load categories: {ex.Message}");
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            SetBusy(true);
            var products = await _apiClient.GetProductsAsync();
            PopulateProducts(products);
            SetStatus($"Loaded {products.Count} products.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load products: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SearchProductsAsync()
    {
        try
        {
            SetBusy(true);
            var category = _categoryComboBox.SelectedItem as CategoryChoice;
            var minPrice = ParseOptionalNonNegativeDouble(_minPriceTextBox.Text);
            var maxPrice = ParseOptionalNonNegativeDouble(_maxPriceTextBox.Text);
            var request = new ProductSearchRequest(
                string.IsNullOrWhiteSpace(_shopSearchTextBox.Text) ? null : _shopSearchTextBox.Text.Trim(),
                category?.Id,
                minPrice,
                maxPrice
            );

            var products = await _apiClient.SearchProductsAsync(request);
            PopulateProducts(products);
            SetStatus($"Found {products.Count} products.");
        }
        catch (Exception ex)
        {
            SetStatus($"Search failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PopulateProducts(List<ProductDto> products)
    {
        _catalogProducts.Clear();
        _catalogProducts.AddRange(products);

        _productCardsPanel.SuspendLayout();
        _productCardsPanel.Controls.Clear();

        foreach (var product in products)
        {
            _productCardsPanel.Controls.Add(BuildProductCard(product));
        }

        _productCardsPanel.ResumeLayout();
        _shopResultsLabel.Text = $"{products.Count} products";
        _selectedProduct = null;
        _productDetailsTextBox.Text = "Select a product to inspect details, recommendations and reviews.";
        _recommendationsListView.Items.Clear();
        _reviewsListView.Items.Clear();
        ShowFeaturedFromIndex(0);
        if (_catalogProducts.Count > 1)
        {
            _featuredTimer.Start();
        }
        else
        {
            _featuredTimer.Stop();
        }
    }

    private Panel BuildProductCard(ProductDto product)
    {
        var card = new Panel
        {
            Width = 222,
            Height = 236,
            Margin = new Padding(10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand,
            Tag = product
        };

        var thumb = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = BuildThumbnailColor(product)
        };
        var thumbText = new Label
        {
            Text = BuildThumbnailText(product),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Trebuchet MS", 18f, FontStyle.Bold, GraphicsUnit.Point)
        };
        thumb.Controls.Add(thumbText);

        var banner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 8,
            BackColor = product.Stock > 0 ? Color.FromArgb(255, 195, 102) : Color.FromArgb(219, 114, 101)
        };

        var name = new Label
        {
            Text = product.Name,
            Font = new Font("Trebuchet MS", 10f, FontStyle.Bold, GraphicsUnit.Point),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(8, 8, 8, 0)
        };

        var price = new Label
        {
            Text = $"EUR {product.Price:F2}",
            Font = new Font("Trebuchet MS", 11f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(38, 73, 126),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(8, 4, 8, 0)
        };

        var stock = new Label
        {
            Text = product.Stock > 0 ? $"In stock ({product.Stock})" : "Out of stock",
            ForeColor = product.Stock > 0 ? Color.FromArgb(36, 121, 67) : Color.FromArgb(162, 44, 44),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(8, 0, 8, 0)
        };

        var meta = new Label
        {
            Text = string.IsNullOrWhiteSpace(product.Brand) ? "WebShop" : product.Brand,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 0, 8, 0),
            ForeColor = Color.FromArgb(90, 98, 117)
        };

        var badgeRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(6, 4, 6, 0)
        };
        foreach (var badge in BuildBadges(product))
        {
            badgeRow.Controls.Add(badge);
        }

        card.Controls.Add(meta);
        card.Controls.Add(badgeRow);
        card.Controls.Add(stock);
        card.Controls.Add(price);
        card.Controls.Add(name);
        card.Controls.Add(banner);
        card.Controls.Add(thumb);

        card.MouseEnter += (_, _) => card.BackColor = Color.FromArgb(255, 252, 244);
        card.MouseLeave += (_, _) => card.BackColor = Color.White;

        async Task selectProduct()
        {
            await ShowProductAsync(product);
        }

        card.Click += async (_, _) => await selectProduct();
        thumb.Click += async (_, _) => await selectProduct();
        thumbText.Click += async (_, _) => await selectProduct();
        banner.Click += async (_, _) => await selectProduct();
        name.Click += async (_, _) => await selectProduct();
        price.Click += async (_, _) => await selectProduct();
        stock.Click += async (_, _) => await selectProduct();
        meta.Click += async (_, _) => await selectProduct();
        badgeRow.Click += async (_, _) => await selectProduct();

        return card;
    }

    private static string BuildThumbnailText(ProductDto product)
    {
        var parts = product.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
        }

        return product.Name.Length > 0
            ? product.Name[..Math.Min(2, product.Name.Length)].ToUpperInvariant()
            : "WS";
    }

    private static Color BuildThumbnailColor(ProductDto product)
    {
        var palette = new[]
        {
            Color.FromArgb(88, 122, 186),
            Color.FromArgb(77, 152, 120),
            Color.FromArgb(181, 121, 76),
            Color.FromArgb(148, 95, 158),
            Color.FromArgb(207, 105, 111),
            Color.FromArgb(101, 134, 146)
        };

        var index = (int)(product.CategoryId % palette.Length);
        return palette[index];
    }

    private IEnumerable<Label> BuildBadges(ProductDto product)
    {
        var badges = new List<Label>();

        if (product.ReleaseYear.HasValue && product.ReleaseYear.Value >= DateTime.Now.Year - 1)
        {
            badges.Add(CreateBadge("NEW", Color.FromArgb(216, 245, 221), Color.FromArgb(28, 96, 42)));
        }

        if (product.Price <= 39.99)
        {
            badges.Add(CreateBadge("DEAL", Color.FromArgb(255, 237, 206), Color.FromArgb(130, 78, 18)));
        }

        if (product.Stock > 0 && product.Stock <= 25)
        {
            badges.Add(CreateBadge("LOW STOCK", Color.FromArgb(254, 223, 223), Color.FromArgb(136, 34, 34)));
        }

        if (badges.Count == 0)
        {
            badges.Add(CreateBadge("POPULAR", Color.FromArgb(233, 239, 255), Color.FromArgb(47, 77, 141)));
        }

        return badges;
    }

    private static Label CreateBadge(string text, Color bg, Color fg)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            BackColor = bg,
            ForeColor = fg,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(6, 3, 6, 3),
            Font = new Font("Trebuchet MS", 8f, FontStyle.Bold, GraphicsUnit.Point)
        };
    }

    private void ShowFeaturedFromIndex(int index)
    {
        if (_catalogProducts.Count == 0)
        {
            _featuredNameLabel.Text = "No featured product";
            _featuredMetaLabel.Text = "Search or reload products to view highlights.";
            return;
        }

        if (index < 0)
        {
            _featuredIndex = _catalogProducts.Count - 1;
        }
        else
        {
            _featuredIndex = index % _catalogProducts.Count;
        }

        var product = _catalogProducts[_featuredIndex];
        _featuredNameLabel.Text = product.Name;
        _featuredMetaLabel.Text = $"EUR {product.Price:F2} | Stock {product.Stock} | {(string.IsNullOrWhiteSpace(product.Brand) ? "WebShop" : product.Brand)}";
    }

    private async Task ShowRelativeFeaturedAsync(int delta)
    {
        if (_catalogProducts.Count == 0)
        {
            return;
        }

        ShowFeaturedFromIndex(_featuredIndex + delta);
        await Task.CompletedTask;
    }

    private async Task OpenFeaturedAsync()
    {
        if (_catalogProducts.Count == 0)
        {
            SetStatus("No featured product available.");
            return;
        }

        await ShowProductAsync(_catalogProducts[_featuredIndex]);
    }

    private async Task ShowProductAsync(ProductDto product)
    {
        _selectedProduct = product;
        _productDetailsTextBox.Text =
            $"{product.Name}\n\n" +
            $"Price: EUR {product.Price:F2}\n" +
            $"Stock: {product.Stock}\n" +
            $"Brand: {product.Brand ?? "-"}\n" +
            $"Publisher: {product.Publisher ?? "-"}\n" +
            $"Release Year: {product.ReleaseYear?.ToString() ?? "-"}\n\n" +
            $"Description\n{product.Description ?? "No description"}";

        await LoadReviewsForProductAsync(product.Id);
        await LoadRecommendationsForProductAsync(product.Id);
        SetStatus($"Viewing: {product.Name}");
    }

    private async Task OnProductSelectedAsync()
    {
        if (_productsListView.SelectedItems.Count == 0)
        {
            return;
        }

        var item = _productsListView.SelectedItems[0];
        if (item.Tag is ProductDto product)
        {
            await ShowProductAsync(product);
        }
    }

    private async Task RefreshSelectedProductDataAsync()
    {
        if (_selectedProduct is null)
        {
            SetStatus("Select a product first.");
            return;
        }

        await LoadReviewsForProductAsync(_selectedProduct.Id);
        await LoadRecommendationsForProductAsync(_selectedProduct.Id);
    }

    private async Task LoadRecommendationsForProductAsync(long productId)
    {
        try
        {
            var payload = await _apiClient.GetRecommendationsAsync(productId);
            _recommendationsListView.BeginUpdate();
            _recommendationsListView.Items.Clear();

            if (payload is not null)
            {
                foreach (var rec in payload.Recommendations)
                {
                    var item = new ListViewItem(rec.ProductName);
                    item.SubItems.Add($"EUR {rec.Price:F2}");
                    item.SubItems.Add(rec.Stock.ToString());
                    item.SubItems.Add(rec.BuyCount.ToString());
                    _recommendationsListView.Items.Add(item);
                }
            }

            _recommendationsListView.EndUpdate();
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load recommendations: {ex.Message}");
        }
    }

    private async Task AddSelectedProductToCartAsync()
    {
        if (_selectedProduct is null)
        {
            SetStatus("Select a product first.");
            return;
        }
        if (!EnsureSignedIn("add items to cart"))
        {
            return;
        }

        var quantity = 1;
        if (_selectedProduct.Stock <= 0)
        {
            SetStatus("This product is out of stock.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.AddCartItemAsync(new AddCartItemRequest(_signedInUserId, _selectedProduct.Id, quantity));
            await LoadCartAsync();
            SetStatus($"Added {_selectedProduct.Name} to cart.");
        }
        catch (Exception ex)
        {
            SetStatus($"Add to cart failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AddSelectedProductToFavoritesAsync()
    {
        if (_selectedProduct is null)
        {
            SetStatus("Select a product first.");
            return;
        }
        if (!EnsureSignedIn("manage favorites"))
        {
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.AddFavoriteAsync(_signedInUserId, _selectedProduct.Id);
            await LoadFavoritesAsync();
            SetStatus($"Saved {_selectedProduct.Name} to favorites.");
        }
        catch (Exception ex)
        {
            SetStatus($"Add favorite failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadReviewsForProductAsync(long productId)
    {
        try
        {
            var reviews = await _apiClient.GetReviewsAsync(productId);
            _reviewsListView.BeginUpdate();
            _reviewsListView.Items.Clear();

            foreach (var review in reviews)
            {
                var item = new ListViewItem(review.UserEmail);
                item.SubItems.Add(review.Stars.ToString());
                item.SubItems.Add(review.CreatedAtUtc);
                item.SubItems.Add(review.Explanation);
                _reviewsListView.Items.Add(item);
            }

            _reviewsListView.EndUpdate();
            SetStatus($"Loaded {reviews.Count} reviews for product {productId}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load reviews: {ex.Message}");
        }
    }

    private async Task SubmitReviewAsync()
    {
        if (_selectedProduct is null)
        {
            SetStatus("Select a product first.");
            return;
        }

        if (!EnsureSignedIn("submit a review"))
        {
            return;
        }

        var explanation = _reviewExplanationTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(explanation))
        {
            SetStatus("Review explanation is required.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.SubmitReviewAsync(_selectedProduct.Id, new CreateProductReviewRequest(_signedInUserId, (int)_starsInput.Value, explanation));
            _reviewExplanationTextBox.Clear();
            await LoadReviewsForProductAsync(_selectedProduct.Id);
            SetStatus($"Review submitted as {_signedInUserEmail}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Submitting review failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadCartAsync()
    {
        if (!EnsureSignedIn("view cart"))
        {
            return;
        }

        try
        {
            var cart = await _apiClient.GetCartAsync(_signedInUserId);
            _cartListView.BeginUpdate();
            _cartListView.Items.Clear();

            double total = 0;
            if (cart is not null)
            {
                foreach (var item in cart.Items)
                {
                    var lineTotal = item.UnitPrice * item.Quantity;
                    total += lineTotal;

                    var row = new ListViewItem(item.ItemId.ToString());
                    row.SubItems.Add(item.ProductName);
                    row.SubItems.Add(item.Quantity.ToString());
                    row.SubItems.Add($"EUR {item.UnitPrice:F2}");
                    row.SubItems.Add($"EUR {lineTotal:F2}");
                    row.Tag = item;
                    _cartListView.Items.Add(row);
                }
            }

            _cartListView.EndUpdate();
            _cartTotalLabel.Text = $"Cart total: EUR {total:F2}";
            var cartCount = _cartListView.Items.Count;
            _miniCartItemsLabel.Text = $"Cart: {cartCount} item{(cartCount == 1 ? string.Empty : "s")}";
            _miniCartTotalLabel.Text = $"EUR {total:F2}";
            SetStatus($"Loaded cart with {_cartListView.Items.Count} items.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load cart: {ex.Message}");
        }
    }

    private async Task RemoveSelectedCartItemAsync()
    {
        if (!EnsureSignedIn("remove cart items"))
        {
            return;
        }
        if (_cartListView.SelectedItems.Count == 0)
        {
            SetStatus("Select a cart item first.");
            return;
        }

        if (_cartListView.SelectedItems[0].Tag is not CartItemDto item)
        {
            SetStatus("Selected cart item is invalid.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.RemoveCartItemAsync(item.ItemId, _signedInUserId);
            await LoadCartAsync();
            SetStatus("Cart item removed.");
        }
        catch (Exception ex)
        {
            SetStatus($"Remove item failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CheckoutAsync()
    {
        if (!EnsureSignedIn("checkout"))
        {
            return;
        }

        try
        {
            SetBusy(true);
            var selectedAddress = _checkoutAddressComboBox.SelectedItem as AddressChoice;
            var shippingAddressId = selectedAddress?.Id;
            var manualAddress = string.IsNullOrWhiteSpace(_checkoutManualAddressTextBox.Text)
                ? null
                : _checkoutManualAddressTextBox.Text.Trim();
            var discountCode = string.IsNullOrWhiteSpace(_checkoutDiscountCodeTextBox.Text)
                ? null
                : _checkoutDiscountCodeTextBox.Text.Trim();

            await _apiClient.CheckoutAsync(new CheckoutRequest(
                _signedInUserId,
                shippingAddressId.HasValue ? null : manualAddress,
                discountCode,
                shippingAddressId
            ));

            _checkoutDiscountCodeTextBox.Clear();
            await LoadCartAsync();
            await LoadOrdersAsync();
            SetStatus("Checkout completed.");
        }
        catch (Exception ex)
        {
            SetStatus($"Checkout failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadOrdersAsync()
    {
        if (!EnsureSignedIn("view orders"))
        {
            return;
        }

        try
        {
            var orders = await _apiClient.GetOrdersAsync(_signedInUserId);
            _ordersListView.BeginUpdate();
            _ordersListView.Items.Clear();

            foreach (var order in orders)
            {
                var row = new ListViewItem(order.OrderId.ToString());
                row.SubItems.Add(order.OrderNumber);
                row.SubItems.Add($"EUR {order.TotalPrice:F2}");
                row.SubItems.Add(order.ShippingAddress);
                _ordersListView.Items.Add(row);
            }

            _ordersListView.EndUpdate();
            SetStatus($"Loaded {orders.Count} orders.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load orders: {ex.Message}");
        }
    }

    private async Task LoadFavoritesAsync()
    {
        if (!EnsureSignedIn("view favorites"))
        {
            return;
        }

        try
        {
            var favorites = await _apiClient.GetFavoritesAsync(_signedInUserId);
            _favoritesListView.BeginUpdate();
            _favoritesListView.Items.Clear();

            foreach (var favorite in favorites)
            {
                var row = new ListViewItem(favorite.FavoriteId.ToString());
                row.SubItems.Add(favorite.ProductId.ToString());
                row.SubItems.Add(favorite.ProductName);
                row.SubItems.Add($"EUR {favorite.Price:F2}");
                row.SubItems.Add(favorite.Stock.ToString());
                row.Tag = favorite;
                _favoritesListView.Items.Add(row);
            }

            _favoritesListView.EndUpdate();
            SetStatus($"Loaded {favorites.Count} favorites.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load favorites: {ex.Message}");
        }
    }

    private async Task RemoveSelectedFavoriteAsync()
    {
        if (!EnsureSignedIn("remove favorites"))
        {
            return;
        }
        if (_favoritesListView.SelectedItems.Count == 0)
        {
            SetStatus("Select a favorite first.");
            return;
        }
        if (_favoritesListView.SelectedItems[0].Tag is not FavoriteProductDto favorite)
        {
            SetStatus("Selected favorite is invalid.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.RemoveFavoriteAsync(_signedInUserId, favorite.FavoriteId);
            await LoadFavoritesAsync();
            SetStatus("Favorite removed.");
        }
        catch (Exception ex)
        {
            SetStatus($"Remove favorite failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadShippingAddressesAsync()
    {
        if (!EnsureSignedIn("manage shipping addresses"))
        {
            return;
        }

        try
        {
            var addresses = await _apiClient.GetShippingAddressesAsync(_signedInUserId);

            _shippingListView.BeginUpdate();
            _shippingListView.Items.Clear();
            foreach (var address in addresses)
            {
                var row = new ListViewItem(address.Id.ToString());
                row.SubItems.Add(address.Label ?? "Saved");
                row.SubItems.Add(address.ShippingAddress);
                row.SubItems.Add(address.IsDefault ? "Yes" : "No");
                row.Tag = address;
                _shippingListView.Items.Add(row);
            }
            _shippingListView.EndUpdate();

            _checkoutAddressComboBox.Items.Clear();
            _checkoutAddressComboBox.Items.Add(new AddressChoice(null, "Manual one-time address"));
            foreach (var address in addresses)
            {
                var label = string.IsNullOrWhiteSpace(address.Label) ? "Saved" : address.Label;
                var defaultTag = address.IsDefault ? " [default]" : string.Empty;
                _checkoutAddressComboBox.Items.Add(new AddressChoice(address.Id, $"{label}: {address.ShippingAddress}{defaultTag}"));
            }
            _checkoutAddressComboBox.SelectedIndex = 0;

            SetStatus($"Loaded {addresses.Count} shipping addresses.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load addresses: {ex.Message}");
        }
    }

    private async Task AddShippingAddressAsync()
    {
        if (!EnsureSignedIn("add shipping addresses"))
        {
            return;
        }

        var address = _shippingAddressTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            SetStatus("Shipping address is required.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.AddShippingAddressAsync(_signedInUserId, new CreateShippingAddressRequest(
                string.IsNullOrWhiteSpace(_shippingLabelTextBox.Text) ? null : _shippingLabelTextBox.Text.Trim(),
                address,
                _shippingDefaultCheckBox.Checked
            ));

            _shippingLabelTextBox.Clear();
            _shippingAddressTextBox.Clear();
            _shippingDefaultCheckBox.Checked = false;
            await LoadShippingAddressesAsync();
            SetStatus("Shipping address saved.");
        }
        catch (Exception ex)
        {
            SetStatus($"Add address failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RemoveSelectedShippingAddressAsync()
    {
        if (!EnsureSignedIn("remove shipping addresses"))
        {
            return;
        }
        if (_shippingListView.SelectedItems.Count == 0)
        {
            SetStatus("Select a shipping address first.");
            return;
        }
        if (_shippingListView.SelectedItems[0].Tag is not ShippingAddressDto address)
        {
            SetStatus("Selected shipping address is invalid.");
            return;
        }

        try
        {
            SetBusy(true);
            await _apiClient.RemoveShippingAddressAsync(_signedInUserId, address.Id);
            await LoadShippingAddressesAsync();
            SetStatus("Shipping address removed.");
        }
        catch (Exception ex)
        {
            SetStatus($"Remove address failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadTopSoldAsync()
    {
        try
        {
            var topSold = await _apiClient.GetTopSoldAsync();
            _topSoldListView.BeginUpdate();
            _topSoldListView.Items.Clear();

            foreach (var item in topSold)
            {
                var row = new ListViewItem(item.ProductId.ToString());
                row.SubItems.Add(item.ProductName);
                row.SubItems.Add(item.SoldQuantity.ToString());
                row.SubItems.Add($"EUR {item.Revenue:F2}");
                _topSoldListView.Items.Add(row);
            }

            _topSoldListView.EndUpdate();
            SetStatus($"Loaded {topSold.Count} top-sold products.");
        }
        catch (Exception ex)
        {
            SetStatus($"Cannot load top sold: {ex.Message}");
        }
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            SetBusy(true);
            await _apiClient.RefreshRecommendationCacheAsync();
            SetStatus("Recommendations cache refreshed.");
        }
        catch (Exception ex)
        {
            SetStatus($"Refresh cache failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task GenerateDocsAsync()
    {
        try
        {
            SetBusy(true);
            await _apiClient.GenerateModelDocsAsync();
            SetStatus("Model documentation generated.");
        }
        catch (Exception ex)
        {
            SetStatus($"Generate docs failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AdminSearchUsersAsync()
    {
        if (!EnsureAdmin())
        {
            return;
        }

        try
        {
            SetBusy(true);
            var users = await _apiClient.AdminSearchUsersAsync(_signedInUserId, _adminSearchTextBox.Text.Trim());
            _adminUsersListView.BeginUpdate();
            _adminUsersListView.Items.Clear();

            foreach (var user in users)
            {
                var name = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "(no name)";
                }

                var row = new ListViewItem(user.UserId.ToString());
                row.SubItems.Add(user.Email);
                row.SubItems.Add(name);
                row.SubItems.Add(user.Role.ToString());
                row.Tag = user;
                _adminUsersListView.Items.Add(row);
            }

            _adminUsersListView.EndUpdate();
            SetStatus($"Admin user search returned {users.Count} users.");
        }
        catch (Exception ex)
        {
            SetStatus($"Admin search failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AdminLoadSelectedProfileAsync()
    {
        if (!EnsureAdmin())
        {
            return;
        }
        if (_adminUsersListView.SelectedItems.Count == 0)
        {
            SetStatus("Select a user from search results.");
            return;
        }

        if (_adminUsersListView.SelectedItems[0].Tag is not AdminUserSummaryDto user)
        {
            SetStatus("Selected admin user is invalid.");
            return;
        }

        try
        {
            SetBusy(true);
            var profile = await _apiClient.AdminGetUserProfileAsync(_signedInUserId, user.UserId);
            if (profile is null)
            {
                _adminProfileTextBox.Text = "No profile data returned.";
                SetStatus("No profile returned.");
                return;
            }

            var lines = new List<string>
            {
                $"User #{profile.UserId} | {profile.Email}",
                $"Name: {profile.FirstName} {profile.LastName}".Trim(),
                $"Role: {profile.Role}",
                $"IBAN: {profile.BankIban ?? "(not set)"}",
                $"Bank Account Name: {profile.BankAccountName ?? "(not set)"}",
                "",
                "Shipping addresses:"
            };

            if (profile.ShippingAddresses.Count == 0)
            {
                lines.Add("  (none)");
            }
            else
            {
                foreach (var address in profile.ShippingAddresses)
                {
                    var label = string.IsNullOrWhiteSpace(address.Label) ? "Saved" : address.Label;
                    var defaultTag = address.IsDefault ? " [default]" : string.Empty;
                    lines.Add($"  #{address.Id} {label}{defaultTag}: {address.ShippingAddress}");
                }
            }

            lines.Add("");
            lines.Add("Orders:");
            if (profile.Orders.Count == 0)
            {
                lines.Add("  (none)");
            }
            else
            {
                foreach (var order in profile.Orders)
                {
                    lines.Add($"  Order #{order.OrderId} ({order.OrderNumber}) EUR {order.TotalPrice:F2}");
                    lines.Add($"    Address: {order.ShippingAddress}");
                    lines.Add($"    Discount: {order.DiscountCode ?? "(none)"}");
                    foreach (var item in order.Items)
                    {
                        lines.Add($"      - {item.ProductName} ({item.Quantity} x EUR {item.UnitPrice:F2})");
                    }
                }
            }

            _adminProfileTextBox.Text = string.Join(Environment.NewLine, lines);
            SetStatus($"Loaded admin profile for user {user.UserId}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Load profile failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AdminCreateDiscountAsync()
    {
        if (!EnsureAdmin())
        {
            return;
        }

        try
        {
            SetBusy(true);
            var created = await _apiClient.AdminCreateDiscountCodeAsync(new CreateRandomDiscountCodeRequest(
                _signedInUserId,
                (int)_discountPercentInput.Value,
                (int)_discountMaxUsesInput.Value,
                _discountValidUntilPicker.Value.Date.ToString("yyyy-MM-dd")
            ));

            if (created is null)
            {
                SetStatus("Discount code creation returned no payload.");
                return;
            }

            _adminProfileTextBox.Text =
                $"Discount code created successfully:{Environment.NewLine}" +
                $"Code: {created.Code}{Environment.NewLine}" +
                $"Discount: {created.DiscountPercentage}%{Environment.NewLine}" +
                $"Usage: {created.UsesCount}/{created.MaxUses}{Environment.NewLine}" +
                $"Valid until: {created.ValidUntil}";

            SetStatus("Admin discount code created.");
        }
        catch (Exception ex)
        {
            SetStatus($"Create discount failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static double? ParseOptionalNonNegativeDouble(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (double.TryParse(input.Trim(), out var value) && value >= 0)
        {
            return value;
        }

        return null;
    }

    private void SetBusy(bool busy)
    {
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        _registerButton.Enabled = !busy;
        _loginButton.Enabled = !busy;
        _logoutButton.Enabled = !busy && _signedInUserId > 0;

        _shopSearchButton.Enabled = !busy;
        _shopReloadButton.Enabled = !busy;
        _addToCartButton.Enabled = !busy;
        _addFavoriteButton.Enabled = !busy;
        _refreshProductDataButton.Enabled = !busy;

        _reloadCartButton.Enabled = !busy;
        _removeCartItemButton.Enabled = !busy;
        _checkoutButton.Enabled = !busy;
        _reloadOrdersButton.Enabled = !busy;

        _reloadFavoritesButton.Enabled = !busy;
        _removeFavoriteButton.Enabled = !busy;

        _reloadShippingButton.Enabled = !busy;
        _addShippingButton.Enabled = !busy;
        _removeShippingButton.Enabled = !busy;

        _reloadTopSoldButton.Enabled = !busy;
        _refreshCacheButton.Enabled = !busy;
        _generateDocsButton.Enabled = !busy;

        _adminSearchButton.Enabled = !busy;
        _adminLoadProfileButton.Enabled = !busy;
        _createDiscountButton.Enabled = !busy;

        _submitReviewButton.Enabled = !busy;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
    }
}
