using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;
using Poplar.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class ProductsViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly IContentDialogService _dialogService;
    private readonly SessionManager _session;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<ProductItemViewModel> _products = new();

    public ProductsViewModel(
        ProductService productService,
        IContentDialogService dialogService,
        SessionManager session)
    {
        _productService = productService;
        _dialogService = dialogService;
        _session = session;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var products = await _productService.GetProductsAsync();
            Products.Clear();
            foreach (var p in products)
            {
                Products.Add(new ProductItemViewModel(p));
            }
            _isInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[ProductsViewModel] Failed to load products: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OnAddProduct()
    {
        // Implementation for adding product
    }

    [RelayCommand]
    private void OnSelectProduct(ProductItemViewModel product)
    {
        if (product == null) return;
        // Navigation to Product Details with Tabs
    }
}

public partial class ProductItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Product _record;

    public ProductItemViewModel(Product record)
    {
        _record = record;
    }
}
