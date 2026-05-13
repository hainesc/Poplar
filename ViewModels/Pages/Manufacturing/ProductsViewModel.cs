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
    private readonly DeviceService _deviceService;
    private readonly IContentDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ProductDetailsViewModel _detailsViewModel;
    private readonly SessionManager _session;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<ProductItemViewModel> _products = new();

    public ProductsViewModel(
        ProductService productService,
        DeviceService deviceService,
        IContentDialogService dialogService,
        INavigationService navigationService,
        ProductDetailsViewModel detailsViewModel,
        SessionManager session)
    {
        _productService = productService;
        _deviceService = deviceService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _detailsViewModel = detailsViewModel;
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
        var editVm = new ProductEditViewModel();
        await editVm.InitializeAsync(_deviceService);

        var content = new Poplar.Views.Pages.Manufacturing.ProductEditControl
        {
            DataContext = editVm
        };


        var result = await _dialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Add New Product",
                Content = content,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            }
        );

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(editVm.ProductName)) return;

                var devices = editVm.GetSelectedDeviceNames();
                await _productService.AddProductAsync(editVm.ProductName, devices.Length > 0 ? devices : null);

                // Refresh list
                _isInitialized = false;
                await InitializeAsync();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[ProductsViewModel] Add product failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task OnSelectProduct(ProductItemViewModel product)
    {
        if (product == null) return;
        
        await _detailsViewModel.InitializeAsync(product.Record.id);
        _navigationService.Navigate(typeof(Poplar.Views.Pages.Manufacturing.ProductDetailsPage));
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
