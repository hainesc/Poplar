using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uniffi.stump;
using Poplar.Services;
using Wpf.Ui.Controls;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class WorkOrdersViewModel : ObservableObject
{
    private readonly WorkOrderService _workOrderService;
    private readonly ProductService _productService;
    private readonly IContentDialogService _dialogService;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<WorkOrderItemViewModel> _workOrders = new();

    public WorkOrdersViewModel(
        WorkOrderService workOrderService,
        ProductService productService,
        IContentDialogService dialogService)
    {
        _workOrderService = workOrderService;
        _productService = productService;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        // We reload every time the page is navigated to, keeping the data perfectly fresh.
        try
        {
            var ordersTask = _workOrderService.GetWorkOrdersAsync();
            var productsTask = _productService.GetProductsAsync();

            await Task.WhenAll(ordersTask, productsTask);

            var orders = await ordersTask;
            var products = await productsTask;

            WorkOrders.Clear();
            foreach (var order in orders)
            {
                var product = products.FirstOrDefault(p => p.id == order.productId);
                WorkOrders.Add(new WorkOrderItemViewModel(order, product));
            }

            _isInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[WorkOrdersViewModel] Failed to load data: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OnAddWorkOrder()
    {
        var createVm = new WorkOrderCreateViewModel();
        await createVm.InitializeAsync(_productService);

        var content = new Poplar.Views.Pages.Manufacturing.WorkOrderCreateControl
        {
            DataContext = createVm
        };

        var result = await _dialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Launch New Batch Orchestration",
                Content = content,
                PrimaryButtonText = "Launch Batch",
                CloseButtonText = "Cancel"
            }
        );

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(createVm.OrderTitle) || createVm.SelectedProduct == null)
                {
                    return;
                }

                await _workOrderService.AddWorkOrderAsync(
                    createVm.OrderTitle,
                    createVm.SelectedProduct.id,
                    createVm.Quantity
                );

                // Refresh the list immediately
                await InitializeAsync();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[WorkOrdersViewModel] Launch batch failed: {ex.Message}");
            }
        }
    }
}

public partial class WorkOrderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private WorkOrder _record;

    [ObservableProperty]
    private Product? _product;

    public WorkOrderItemViewModel(WorkOrder record, Product? product)
    {
        _record = record;
        _product = product;
    }

    public string ProductName => Product?.name ?? $"Product ID: {Record.productId}";

    public double ProgressPercent
    {
        get
        {
            if (Record.quantity <= 0) return 0.0;
            double percent = ((double)Record.processedCount / Record.quantity) * 100.0;
            return Math.Min(100.0, Math.Max(0.0, percent));
        }
    }

    public string StatusText => Record.status switch
    {
        WorkOrderStatus.InProgress => "In Progress",
        _ => Record.status.ToString()
    };

    public string StatusColor => Record.status switch
    {
        WorkOrderStatus.InProgress => "#10B981", // Emerald/Green
        WorkOrderStatus.Paused => "#F59E0B",     // Amber/Gold
        WorkOrderStatus.Completed => "#3B82F6",  // Blue
        WorkOrderStatus.Stopped => "#EF4444",    // Rose/Red
        _ => "#9CA3AF"                           // Gray/Pending
    };

    public bool IsActive => Record.status == WorkOrderStatus.InProgress;
}
