using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;
using Poplar.Services;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class RuntimeViewModel : ObservableObject, IRecipient<ManufacturingEventMessage>
{
    private readonly WorkOrderService _workOrderService;
    private readonly ProductService _productService;
    private readonly ManufacturingService _manufacturingService;
    private readonly BackendService _backend;

    [ObservableProperty] private DagEditorViewModel _dagEditor = new();
    
    [ObservableProperty] private ObservableCollection<WorkOrder> _workOrders = new();
    [ObservableProperty] private WorkOrder? _selectedWorkOrder;

    [ObservableProperty] private ObservableCollection<string> _processLogs = new();
    [ObservableProperty] private ObservableCollection<TraceabilityRecord> _traceabilityLogs = new();

    // Execution state
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private bool _isStale;

    // Quality Stats
    [ObservableProperty] private int _processedCount;
    [ObservableProperty] private int _scrapCount;
    [ObservableProperty] private double _yieldRate = 100.0;
    [ObservableProperty] private string _yieldStatus = "OK";

    // Strategic Prompt Center
    [ObservableProperty] private string _currentInstruction = "Ready. Select a Work Order and click Start to begin production.";
    [ObservableProperty] private string _currentInstructionType = "Info";

    public RuntimeViewModel(
        WorkOrderService workOrderService,
        ProductService productService,
        ManufacturingService manufacturingService,
        BackendService backend)
    {
        _workOrderService = workOrderService;
        _productService = productService;
        _manufacturingService = manufacturingService;
        _backend = backend;

        // Register for real-time manufacturing event messages
        WeakReferenceMessenger.Default.Register<ManufacturingEventMessage>(this);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        // Start the background subscription
        await _manufacturingService.StartSubscriptionAsync();
        
        // Load active work orders
        await LoadWorkOrdersAsync();

        // Query current engine state to restore state if already running
        await SyncCurrentStatusAsync();
    }

    [RelayCommand]
    public async Task LoadWorkOrdersAsync()
    {
        try
        {
            var wos = await _workOrderService.GetWorkOrdersAsync();
            WorkOrders.Clear();
            foreach (var wo in wos)
            {
                WorkOrders.Add(wo);
            }

            // Auto-select first pending/active work order if available
            if (SelectedWorkOrder == null && WorkOrders.Count > 0)
            {
                SelectedWorkOrder = WorkOrders.FirstOrDefault(w => w.status == WorkOrderStatus.InProgress || w.status == WorkOrderStatus.Pending)
                                    ?? WorkOrders.First();
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Runtime] Load work orders failed: {ex.Message}");
        }
    }

    partial void OnSelectedWorkOrderChanged(WorkOrder? value)
    {
        if (value != null)
        {
            _ = LoadDagForProductAsync(value.productId);
            _ = LoadTraceabilityLogsAsync(value.productId, value.id);
            ProcessLogs.Clear();
            ProcessLogs.Add($"[{DateTime.Now:HH:mm:ss}] Selected Work Order: {value.title}");
        }
        else
        {
            DagEditor.Nodes.Clear();
            DagEditor.Connections.Clear();
            TraceabilityLogs.Clear();
        }
    }

    private async Task LoadDagForProductAsync(int productId)
    {
        try
        {
            var dag = await _productService.GetDagByProductAsync(productId);
            var metadata = await _productService.GetStepTypesAsync();

            if (dag != null)
            {
                DagEditor.Initialize(metadata);
                DagEditor.LoadFromDagFlow(dag);
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Runtime] Load DAG failed: {ex.Message}");
        }
    }

    private async Task LoadTraceabilityLogsAsync(int productId, int workOrderId)
    {
        try
        {
            var query = new TraceQuery(productId, workOrderId, null, null, null, 50, 0);
            var json = await _backend.TraceabilityVm.QueryRecords(query);

            if (!string.IsNullOrEmpty(json))
            {
                var records = JsonSerializer.Deserialize<TraceabilityRecord[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TraceabilityLogs.Clear();
                if (records != null)
                {
                    foreach (var rec in records.OrderByDescending(r => r.CreatedAt))
                    {
                        TraceabilityLogs.Add(rec);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Runtime] Load traceability failed: {ex.Message}");
        }
    }

    private async Task SyncCurrentStatusAsync()
    {
        try
        {
            var status = await _backend.ManufacturingVm.GetStatus();
            UpdateStatus(status);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Runtime] Sync status failed: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (SelectedWorkOrder == null) return;

        try
        {
            await _workOrderService.ActivateWorkOrderAsync(SelectedWorkOrder.id);
            await SyncCurrentStatusAsync();
            ProcessLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [Command] Sent Play command for Work Order #{SelectedWorkOrder.id}");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to start work order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task TogglePauseAsync()
    {
        if (SelectedWorkOrder == null) return;

        try
        {
            await _workOrderService.PauseWorkOrderAsync(SelectedWorkOrder.id);
            await SyncCurrentStatusAsync();
            ProcessLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [Command] Sent Pause/Resume toggle for Work Order #{SelectedWorkOrder.id}");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to pause work order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task StopAsync()
    {
        if (SelectedWorkOrder == null) return;

        try
        {
            await _workOrderService.StopWorkOrderAsync(SelectedWorkOrder.id);
            await SyncCurrentStatusAsync();
            ProcessLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [Command] Sent Stop command for Work Order #{SelectedWorkOrder.id}");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to stop work order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void Receive(ManufacturingEventMessage message)
    {
        // Safe dispatch to the UI thread
        Application.Current?.Dispatcher?.Invoke(() => HandleEvent(message.Event));
    }

    private void HandleEvent(ManufacturingEvent ev)
    {
        switch (ev)
        {
            case ManufacturingEvent.StepChanged stepChanged:
                foreach (var node in DagEditor.Nodes)
                {
                    node.IsActiveStep = (node.Id == stepChanged.stepId);
                }
                ProcessLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [Event] Process transitioned to step: {stepChanged.stepId}");
                break;

            case ManufacturingEvent.Message msg:
                var level = msg.v1.msgType.ToString();
                var text = msg.v1.content;
                var logTime = DateTimeOffset.FromUnixTimeMilliseconds(msg.v1.timestamp).LocalDateTime.ToString("HH:mm:ss");
                
                ProcessLogs.Insert(0, $"[{logTime}] [{level}] {text}");
                CurrentInstruction = text;
                CurrentInstructionType = level;
                break;

            case ManufacturingEvent.StatsUpdated stats:
                ProcessedCount = stats.processedCount;
                ScrapCount = stats.scrapCount;
                CalculateYield();
                break;

            case ManufacturingEvent.StatusChanged status:
                UpdateStatus(status.v1);
                break;

            case ManufacturingEvent.TraceabilityRecorded trace:
                var newRecord = new TraceabilityRecord(
                    trace.productId,
                    trace.workOrderId,
                    trace.processId,
                    trace.result,
                    trace.data,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                );
                TraceabilityLogs.Insert(0, newRecord);
                break;
        }
    }

    private void UpdateStatus(ManufacturingStatus status)
    {
        IsRunning = status.isRunning;
        IsPaused = status.isPaused;
        IsStale = status.isStale;

        ProcessedCount = status.stats.processedCount;
        ScrapCount = status.stats.scrapCount;
        CalculateYield();

        if (IsRunning && !string.IsNullOrEmpty(status.currentStepId))
        {
            foreach (var node in DagEditor.Nodes)
            {
                node.IsActiveStep = (node.Id == status.currentStepId);
            }
        }
        else
        {
            foreach (var node in DagEditor.Nodes)
            {
                node.IsActiveStep = false;
            }
        }
    }

    private void CalculateYield()
    {
        if (ProcessedCount == 0)
        {
            YieldRate = 100.0;
            YieldStatus = "OK";
            return;
        }

        double goodCount = ProcessedCount - ScrapCount;
        YieldRate = Math.Round((goodCount / ProcessedCount) * 100.0, 1);
        YieldStatus = YieldRate >= 98.0 ? "EXCELLENT" : (YieldRate >= 95.0 ? "OK" : "WARNING");
    }
}
