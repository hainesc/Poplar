using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Wpf.Ui.Controls;
using Poplar.Views.Pages.Manufacturing;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class ProductDetailsViewModel : ObservableObject, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ProductService _productService;
    private readonly DeviceService _deviceService;
    private readonly ISnackbarService _snackbarService;
    private readonly IContentDialogService _dialogService;
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
    private ObservableCollection<PlcTagViewModel> _plcTags = new();

    // Traceability Tab
    [ObservableProperty]
    private ObservableCollection<TraceabilityItemViewModel> _traceabilityItems = new();

    // Logic Flow Tab (Visual Editor)
    public DagEditorViewModel DagEditor { get; } = new();

    [ObservableProperty] private ObservableCollection<DeviceRecord> _devices = new();
    [ObservableProperty] private PlcTagRecord[] _allTags = Array.Empty<PlcTagRecord>();

    private DagFlow? _originalDag;

    public ProductDetailsViewModel(
        INavigationService navigationService,
        ProductService productService,
        DeviceService deviceService,
        ISnackbarService snackbarService,
        IContentDialogService dialogService)
    {
        _navigationService = navigationService;
        _productService = productService;
        _deviceService = deviceService;
        _snackbarService = snackbarService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private void OnNavigateBack()
    {
        _navigationService.GoBack();
    }

    public async Task InitializeAsync(int productId)
    {
        _productId = productId;
        await LoadProductAsync();
        await LoadHardwareAsync();
        await LoadTagsAsync();
        await LoadTraceabilityAsync();
        await LoadDevicesAsync();
        await LoadDagAsync();
        _isInitialized = true;
    }

    private async Task LoadDevicesAsync()
    {
        var devices = await _deviceService.GetDevicesAsync();
        Devices.Clear();
        foreach (var d in devices) Devices.Add(d);
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
        AllTags = tags;
        PlcTags.Clear();
        foreach (var tag in tags)
        {
            PlcTags.Add(new PlcTagViewModel(tag));
        }
    }

    private async Task LoadTraceabilityAsync()
    {
        var items = await _productService.GetTraceabilitySchemaAsync(_productId);
        TraceabilityItems.Clear();
        foreach (var item in items)
        {
            TraceabilityItems.Add(new TraceabilityItemViewModel(item));
        }
    }

    private async Task LoadDagAsync()
    {
        var meta = await _productService.GetStepTypesAsync();
        DagEditor.Initialize(meta);

        var dag = await _productService.GetDagByProductAsync(_productId);
        DagEditor.LoadFromDagFlow(dag);
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
            var tagsArray = PlcTags.Select(t => t.ToRecord()).ToArray();
            await _productService.SyncProductTagsAsync(_productId, tagsArray);
            ShowSuccess("PLC Tags Saved");
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void AddTag()
    {
        PlcTags.Add(new PlcTagViewModel
        {
            Direction = PlcTagDirection.PlcToHmi,
            ValueType = "bool",
            ProductId = _productId
        });
    }

    [RelayCommand]
    private void DeleteTag(PlcTagViewModel tag)
    {
        if (tag != null && PlcTags.Contains(tag))
        {
            PlcTags.Remove(tag);
        }
    }

    [RelayCommand]
    private async Task BulkImportTagsAsync()
    {
        var control = new BulkImportTagsControl();
        var result = await _dialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
        {
            Title = "Bulk Tag Import",
            Content = control,
            PrimaryButtonText = "Confirm & Append",
            CloseButtonText = "Cancel"
        });

        if (result == ContentDialogResult.Primary)
        {
            var text = control.GetImportText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var currentDirection = PlcTagDirection.PlcToHmi;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed == "[PLC->PC]" || trimmed == "[PLC->HMI]")
                {
                    currentDirection = PlcTagDirection.PlcToHmi;
                    continue;
                }
                if (trimmed == "[PC->PLC]" || trimmed == "[HMI->PLC]")
                {
                    currentDirection = PlcTagDirection.HmiToPlc;
                    continue;
                }

                var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var addrPart = parts[0];
                    var namePart = parts[1];
                    var typePart = parts[2];

                    int byteOffset = 0;
                    int bitOffset = 0;

                    if (addrPart.Contains("."))
                    {
                        var split = addrPart.Split('.');
                        int.TryParse(split[0], out byteOffset);
                        int.TryParse(split[1], out bitOffset);
                    }
                    else
                    {
                        int.TryParse(addrPart, out byteOffset);
                    }

                    PlcTags.Add(new PlcTagViewModel
                    {
                        Name = namePart,
                        Direction = currentDirection,
                        ByteOffset = byteOffset,
                        BitOffset = bitOffset,
                        ValueType = typePart.ToLower(),
                        Description = string.Empty,
                        ProductId = _productId
                    });
                }
            }
        }
    }

    [RelayCommand]
    private async Task SaveTraceabilityAsync()
    {
        try
        {
            var itemsArray = TraceabilityItems.Select(t => t.ToRecord()).ToArray();
            await _productService.UpdateTraceabilitySchemaAsync(_productId, itemsArray);
            ShowSuccess("Traceability Schema Saved");
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void AddTraceabilityItem()
    {
        TraceabilityItems.Add(new TraceabilityItemViewModel
        {
            DataType = "string",
            ProductId = _productId
        });
    }

    [RelayCommand]
    private void DeleteTraceabilityItem(TraceabilityItemViewModel item)
    {
        if (item != null && TraceabilityItems.Contains(item))
        {
            TraceabilityItems.Remove(item);
        }
    }

    [RelayCommand]
    private async Task SaveDagAsync()
    {
        try
        {
            var dag = DagEditor.ExportToDagFlow(0, $"Flow for Product {_productId}");
            if (dag != null)
            {
                await _productService.UpdateDagByProductAsync(_productId, dag);
                ShowSuccess("Process Logic Saved");
            }
        }
        catch (System.Exception ex)
        {
            ShowError("Save Failed", ex.Message);
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
