using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Wpf.Ui.Controls;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class ProductDetailsViewModel : ObservableObject, INavigationAware
{
    private readonly ProductService _productService;
    private readonly DeviceService _deviceService;
    private readonly ISnackbarService _snackbarService;
    private int _productId;
    private bool _isInitialized;

    [ObservableProperty]
    private Product? _currentProduct;

    [ObservableProperty]
    private string _productName = string.Empty;

    // Hardware Tab
    [ObservableProperty]
    private ObservableCollection<DeviceSelectionItem> _availableDevices = new();

    // PLC Tags Tab
    [ObservableProperty]
    private ObservableCollection<PlcTagRecord> _plcTags = new();

    // Traceability Tab
    [ObservableProperty]
    private ObservableCollection<TraceabilityItem> _traceabilityItems = new();

    // Logic Flow Tab (JSON string for now)
    [ObservableProperty]
    private string _dagFlowJson = string.Empty;

    public ProductDetailsViewModel(
        ProductService productService,
        DeviceService deviceService,
        ISnackbarService snackbarService)
    {
        _productService = productService;
        _deviceService = deviceService;
        _snackbarService = snackbarService;
    }

    public async Task InitializeAsync(int productId)
    {
        if (_isInitialized && _productId == productId) return;
        _productId = productId;

        try
        {
            await LoadProductAsync();
            await LoadHardwareAsync();
            await LoadTagsAsync();
            await LoadTraceabilityAsync();
            await LoadDagAsync();

            _isInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[ProductDetailsViewModel] Init failed: {ex.Message}");
            ShowError("Failed to load product details.", ex.Message);
        }
    }

    private async Task LoadProductAsync()
    {
        var products = await _productService.GetProductsAsync();
        CurrentProduct = products.FirstOrDefault(p => p.id == _productId);
        if (CurrentProduct != null)
        {
            ProductName = CurrentProduct.name;
        }
    }

    private async Task LoadHardwareAsync()
    {
        var devices = await _deviceService.GetDevicesAsync();
        AvailableDevices.Clear();
        
        var associatedDeviceNames = CurrentProduct?.devices ?? Array.Empty<string>();

        foreach (var d in devices)
        {
            AvailableDevices.Add(new DeviceSelectionItem(d)
            {
                IsSelected = associatedDeviceNames.Contains(d.name)
            });
        }
    }

    private async Task LoadTagsAsync()
    {
        var tags = await _productService.GetProductTagsAsync(_productId);
        PlcTags.Clear();
        foreach (var tag in tags)
        {
            PlcTags.Add(tag);
        }
    }

    private async Task LoadTraceabilityAsync()
    {
        var items = await _productService.GetTraceabilitySchemaAsync(_productId);
        TraceabilityItems.Clear();
        foreach (var item in items)
        {
            TraceabilityItems.Add(item);
        }
    }

    private async Task LoadDagAsync()
    {
        var dag = await _productService.GetDagByProductAsync(_productId);
        if (dag != null)
        {
            // Simple JSON representation for now
            DagFlowJson = System.Text.Json.JsonSerializer.Serialize(dag, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            DagFlowJson = "{\n  \"nodes\": [],\n  \"edges\": []\n}";
        }
    }

    [RelayCommand]
    private async Task SaveHardwareAsync()
    {
        try
        {
            var selectedDevices = AvailableDevices
                .Where(x => x.IsSelected)
                .Select(x => x.Device.name)
                .ToArray();

            await _productService.UpdateProductAsync(_productId, ProductName, selectedDevices);
            ShowSuccess("Hardware Association Saved");
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveTagsAsync()
    {
        try
        {
            var tagsArray = PlcTags.ToArray();
            await _productService.SyncProductTagsAsync(_productId, tagsArray);
            ShowSuccess("PLC Tags Saved");
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveTraceabilityAsync()
    {
        try
        {
            var itemsArray = TraceabilityItems.ToArray();
            await _productService.UpdateTraceabilitySchemaAsync(_productId, itemsArray);
            ShowSuccess("Traceability Schema Saved");
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveDagAsync()
    {
        try
        {
            var dag = System.Text.Json.JsonSerializer.Deserialize<DagFlow>(DagFlowJson);
            if (dag != null)
            {
                await _productService.UpdateDagByProductAsync(_productId, dag);
                ShowSuccess("Logic Flow Saved");
            }
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", "Invalid JSON format or backend error.\n" + ex.Message);
        }
    }

    private void ShowSuccess(string message)
    {
        _snackbarService.Show("Success", message, ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
    }

    private void ShowError(string title, string message)
    {
        _snackbarService.Show(title, message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
    }

    public Task OnNavigatedToAsync() { return Task.CompletedTask; }
    public Task OnNavigatedFromAsync() { return Task.CompletedTask; }
}
