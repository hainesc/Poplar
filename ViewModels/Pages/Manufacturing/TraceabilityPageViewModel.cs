using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uniffi.stump;
using Poplar.Models;
using Poplar.Services;

namespace Poplar.ViewModels.Pages.Manufacturing;

public class CategoryBarItem
{
    public string Name { get; set; } = string.Empty;
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public int TotalCount => OkCount + NgCount;
    public double OkWidth { get; set; }
    public double NgWidth { get; set; }
}

public partial class TraceabilityPageViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly ManufacturingService _manufacturingService;
    private readonly BackendService _backend;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private Product? _selectedProduct;

    [ObservableProperty] private int? _workOrderId;
    [ObservableProperty] private string? _processId;

    [ObservableProperty] private DateTime _startTime = DateTime.Now.AddDays(-1);
    [ObservableProperty] private DateTime _endTime = DateTime.Now;

    [ObservableProperty] private ObservableCollection<TraceabilityRecord> _records = new();
    [ObservableProperty] private TraceabilityRecord? _selectedRecord;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _viewMode = "table"; // "table" or "grid"
    [ObservableProperty] private bool _isTableView = true;
    [ObservableProperty] private bool _isGridView = false;

    partial void OnViewModeChanged(string value)
    {
        IsTableView = value.Equals("table", StringComparison.OrdinalIgnoreCase);
        IsGridView = value.Equals("grid", StringComparison.OrdinalIgnoreCase);
    }

    // Summary Statistics
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private double _yieldRate = 100.0;
    [ObservableProperty] private int _okCount;
    [ObservableProperty] private int _ngCount;

    // Charts & Analytics Data
    [ObservableProperty] private ObservableCollection<string> _availableMetrics = new();
    [ObservableProperty] private string? _selectedMetric;

    [ObservableProperty] private ObservableCollection<string> _availableCategories = new();
    [ObservableProperty] private string? _selectedCategory;

    // Throughput Chart SVG Path strings (400x150 viewport)
    [ObservableProperty] private string _throughputAreaPathData = "M 0,150 L 400,150 Z";
    [ObservableProperty] private string _throughputLinePathData = "M 0,150 L 400,150";

    // Metric Trend Chart SVG Path strings (400x150 viewport)
    [ObservableProperty] private string _metricAreaPathData = "M 0,150 L 400,150 Z";
    [ObservableProperty] private string _metricLinePathData = "M 0,150 L 400,150";
    [ObservableProperty] private ObservableCollection<Point> _metricNgPoints = new();

    // Custom proportional horizontal bars for Category OK/NG stacked bar
    [ObservableProperty] private ObservableCollection<CategoryBarItem> _categoryBars = new();

    public TraceabilityPageViewModel(
        ProductService productService,
        ManufacturingService manufacturingService,
        BackendService backend)
    {
        _productService = productService;
        _manufacturingService = manufacturingService;
        _backend = backend;
    }

    [RelayCommand]
    public void SelectRecord(TraceabilityRecord record)
    {
        SelectedRecord = record;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var productList = await _productService.GetProductsAsync();
            Products = new ObservableCollection<Product>(productList);
            if (Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TraceabilityVM] Init error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadRecordsAsync(string? action = null)
    {
        if (action == "table" || action == "grid")
        {
            ViewMode = action;
            return;
        }
        if (action == "close_drawer")
        {
            SelectedRecord = null;
            return;
        }

        if (SelectedProduct == null) return;

        IsLoading = true;
        try
        {
            int targetProductId = SelectedProduct.id;

            // 1. Gather all "Hot" records from the authoritative queue in ManufacturingService
            var hotRecords = _manufacturingService.GetHotTraceRecords()
                .Where(r => r.ProductId == targetProductId)
                .ToList();

            var startTs = new DateTimeOffset(StartTime).ToUnixTimeMilliseconds();
            var endTs = new DateTimeOffset(EndTime).ToUnixTimeMilliseconds();

            // Filter hot records within selection criteria
            var hotMatches = hotRecords.Where(r =>
            {
                if (DateTime.TryParse(r.CreatedAt, out var dt))
                {
                    var ts = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                    bool timeOk = ts >= startTs && ts <= endTs;
                    bool woOk = !WorkOrderId.HasValue || r.WorkOrderId == WorkOrderId.Value;
                    bool procOk = string.IsNullOrEmpty(ProcessId) || r.ProcessId.Contains(ProcessId, StringComparison.OrdinalIgnoreCase);
                    return timeOk && woOk && procOk;
                }
                return false;
            }).ToList();

            // 2. Query FFI "Cold" database if time span exceeds earliest hot trace
            var oldestHotTs = hotRecords.Count > 0
                ? hotRecords.Min(r => DateTime.TryParse(r.CreatedAt, out var dt) ? new DateTimeOffset(dt).ToUnixTimeMilliseconds() : DateTimeOffset.Now.ToUnixTimeMilliseconds())
                : DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var finalRecordsList = new List<TraceabilityRecord>(hotMatches);

            if (startTs < oldestHotTs)
            {
                System.Diagnostics.Debug.WriteLine("[TraceabilityVM] Querying cold database via FFI...");
                var query = new TraceQuery(
                    targetProductId,
                    WorkOrderId,
                    ProcessId,
                    startTs / 1000,
                    endTs / 1000,
                    1000,
                    0
                );

                string jsonResult = await _backend.TraceabilityVm.QueryRecords(query);
                if (!string.IsNullOrEmpty(jsonResult))
                {
                    var coldRecords = JsonSerializer.Deserialize<List<TraceabilityRecord>>(jsonResult, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (coldRecords != null)
                    {
                        var seenIds = new HashSet<string>(finalRecordsList.Select(m => m.ProcessId));
                        foreach (var cr in coldRecords)
                        {
                            if (!seenIds.Contains(cr.ProcessId))
                            {
                                finalRecordsList.Add(cr);
                            }
                        }
                    }
                }
            }

            // 3. Sort descending by creation timestamp
            var sortedRecords = finalRecordsList.OrderByDescending(r =>
            {
                if (DateTime.TryParse(r.CreatedAt, out var dt)) return dt;
                return DateTime.MinValue;
            }).ToList();

            Records = new ObservableCollection<TraceabilityRecord>(sortedRecords);

            // 4. Compute Metrics, Analytics & Charts
            ComputeStatistics();
            ExtractDynamicKeys();
            UpdateAnalyticsCharts();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TraceabilityVM] Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeStatistics()
    {
        TotalCount = Records.Count;
        if (TotalCount == 0)
        {
            YieldRate = 100.0;
            OkCount = 0;
            NgCount = 0;
            return;
        }

        OkCount = Records.Count(r => r.Result.Equals("ok", StringComparison.OrdinalIgnoreCase));
        NgCount = TotalCount - OkCount;
        YieldRate = (OkCount / (double)TotalCount) * 100.0;
    }

    private void ExtractDynamicKeys()
    {
        var metrics = new HashSet<string>();
        var categories = new HashSet<string> { "result" };

        foreach (var record in Records)
        {
            if (string.IsNullOrEmpty(record.Data)) continue;
            try
            {
                using var doc = JsonDocument.Parse(record.Data);
                ExtractElement(doc.RootElement, metrics, categories, "");
            }
            catch {}
        }

        var prevMetric = SelectedMetric;
        AvailableMetrics = new ObservableCollection<string>(metrics.OrderBy(x => x));
        if (AvailableMetrics.Count > 0)
        {
            SelectedMetric = AvailableMetrics.Contains(prevMetric ?? "") ? prevMetric : AvailableMetrics[0];
        }
        else
        {
            SelectedMetric = null;
        }

        var prevCat = SelectedCategory;
        AvailableCategories = new ObservableCollection<string>(categories.OrderBy(x => x));
        if (AvailableCategories.Count > 0)
        {
            SelectedCategory = AvailableCategories.Contains(prevCat ?? "") ? prevCat : AvailableCategories[0];
        }
        else
        {
            SelectedCategory = null;
        }
    }

    private void ExtractElement(JsonElement element, HashSet<string> metrics, HashSet<string> categories, string prefix)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                // Check for GenericValue wrapper mapping {"I": 123}, {"F": 12.3}, {"S": "text"}, {"B": true}
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    bool unwrapped = false;
                    if (prop.Value.TryGetProperty("I", out var iProp) && iProp.ValueKind == JsonValueKind.Number)
                    {
                        metrics.Add(key);
                        unwrapped = true;
                    }
                    else if (prop.Value.TryGetProperty("F", out var fProp) && fProp.ValueKind == JsonValueKind.Number)
                    {
                        metrics.Add(key);
                        unwrapped = true;
                    }
                    else if (prop.Value.TryGetProperty("S", out var sProp) && sProp.ValueKind == JsonValueKind.String)
                    {
                        if (sProp.GetString()?.Length < 50)
                        {
                            categories.Add(key);
                        }
                        unwrapped = true;
                    }
                    else if (prop.Value.TryGetProperty("B", out var bProp) && (bProp.ValueKind == JsonValueKind.True || bProp.ValueKind == JsonValueKind.False))
                    {
                        categories.Add(key);
                        unwrapped = true;
                    }

                    if (!unwrapped)
                    {
                        ExtractElement(prop.Value, metrics, categories, key);
                    }
                }
                else if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    metrics.Add(key);
                }
                else if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    if (prop.Value.GetString()?.Length < 50)
                    {
                        categories.Add(key);
                    }
                }
                else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                {
                    categories.Add(key);
                }
            }
        }
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        _ = LoadRecordsAsync();
    }

    partial void OnSelectedMetricChanged(string? value)
    {
        UpdateMetricTrendChart();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        UpdateCategoryInsights();
    }

    public void UpdateAnalyticsCharts()
    {
        UpdateThroughputDensityChart();
        UpdateMetricTrendChart();
        UpdateCategoryInsights();
    }

    private void UpdateThroughputDensityChart()
    {
        if (Records == null || Records.Count == 0)
        {
            ThroughputAreaPathData = "M 0,150 L 400,150 Z";
            ThroughputLinePathData = "M 0,150 L 400,150";
            return;
        }

        // Group into 24 bins representing the last 24 hours
        var timeBins = Enumerable.Range(0, 24).Select(i => DateTime.Now.AddHours(i - 23)).ToList();
        var counts = new int[24];
        for (int i = 0; i < 24; i++)
        {
            var bin = timeBins[i];
            counts[i] = Records.Count(r =>
            {
                if (DateTime.TryParse(r.CreatedAt, out var dt))
                {
                    return dt.Hour == bin.Hour && dt.Date == bin.Date;
                }
                return false;
            });
        }

        int maxVal = counts.Max();
        if (maxVal == 0) maxVal = 1;

        double chartWidth = 400;
        double chartHeight = 110;
        double yOffset = 30;

        var points = new List<Point>();
        for (int i = 0; i < 24; i++)
        {
            double x = i * (chartWidth / 23.0);
            double y = yOffset + chartHeight - (counts[i] / (double)maxVal * chartHeight);
            points.Add(new Point(x, y));
        }

        var lineSegments = points.Select(p => $"{p.X:F1},{p.Y:F1}");
        ThroughputLinePathData = "M " + string.Join(" L ", lineSegments);
        ThroughputAreaPathData = $"M 0,150 L {ThroughputLinePathData.Substring(2)} L {chartWidth:F1},150 Z";
    }

    private void UpdateMetricTrendChart()
    {
        if (Records == null || Records.Count == 0 || string.IsNullOrEmpty(SelectedMetric))
        {
            MetricAreaPathData = "M 0,150 L 400,150 Z";
            MetricLinePathData = "M 0,150 L 400,150";
            MetricNgPoints = new ObservableCollection<Point>();
            return;
        }

        var chronoRecords = Records.Reverse().ToList();
        int count = chronoRecords.Count;
        var values = new double[count];

        for (int i = 0; i < count; i++)
        {
            values[i] = GetMetricValue(chronoRecords[i].Data, SelectedMetric) ?? 0.0;
        }

        double maxVal = values.Max();
        double minVal = values.Min();
        double range = maxVal - minVal;
        if (range < 0.001) range = 1.0;

        double chartWidth = 400;
        double chartHeight = 110;
        double yOffset = 30;

        var points = new List<Point>();
        var ngPoints = new List<Point>();

        for (int i = 0; i < count; i++)
        {
            double x = count > 1 ? i * (chartWidth / (count - 1)) : 0.0;
            double y = yOffset + chartHeight - ((values[i] - minVal) / range * chartHeight);
            points.Add(new Point(x, y));

            if (chronoRecords[i].Result.Equals("ng", StringComparison.OrdinalIgnoreCase))
            {
                ngPoints.Add(new Point(x, y));
            }
        }

        var lineSegments = points.Select(p => $"{p.X:F1},{p.Y:F1}");
        MetricLinePathData = "M " + string.Join(" L ", lineSegments);
        MetricAreaPathData = $"M 0,150 L {MetricLinePathData.Substring(2)} L {chartWidth:F1},150 Z";

        MetricNgPoints = new ObservableCollection<Point>(ngPoints);
    }

    private void UpdateCategoryInsights()
    {
        if (Records == null || Records.Count == 0 || string.IsNullOrEmpty(SelectedCategory))
        {
            CategoryBars = new ObservableCollection<CategoryBarItem>();
            return;
        }

        var grouping = new Dictionary<string, (int ok, int ng)>();
        foreach (var record in Records)
        {
            string val = "Unknown";
            if (SelectedCategory.Equals("result", StringComparison.OrdinalIgnoreCase))
            {
                val = record.Result;
            }
            else
            {
                val = GetCategoryValue(record.Data, SelectedCategory);
            }

            var limitVal = val.Length > 20 ? val.Substring(0, 17) + "..." : val;
            if (!grouping.ContainsKey(limitVal))
            {
                grouping[limitVal] = (0, 0);
            }

            var stats = grouping[limitVal];
            if (record.Result.Equals("ok", StringComparison.OrdinalIgnoreCase))
            {
                grouping[limitVal] = (stats.ok + 1, stats.ng);
            }
            else
            {
                grouping[limitVal] = (stats.ok, stats.ng + 1);
            }
        }

        var list = grouping.Select(g => new CategoryBarItem
        {
            Name = g.Key,
            OkCount = g.Value.ok,
            NgCount = g.Value.ng
        }).OrderByDescending(b => b.TotalCount).Take(6).ToList();

        int maxTotal = list.Count > 0 ? list.Max(b => b.TotalCount) : 1;
        double maxBarWidth = 190.0;

        foreach (var bar in list)
        {
            bar.OkWidth = (bar.OkCount / (double)maxTotal) * maxBarWidth;
            bar.NgWidth = (bar.NgCount / (double)maxTotal) * maxBarWidth;
        }

        CategoryBars = new ObservableCollection<CategoryBarItem>(list);
    }

    private double? GetMetricValue(string json, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var current = doc.RootElement;
            var parts = path.Split('.');
            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
                {
                    current = next;
                }
                else
                {
                    return null;
                }
            }

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.TryGetProperty("I", out var iProp) && iProp.ValueKind == JsonValueKind.Number)
                {
                    return iProp.GetDouble();
                }
                if (current.TryGetProperty("F", out var fProp) && fProp.ValueKind == JsonValueKind.Number)
                {
                    return fProp.GetDouble();
                }
            }
            else if (current.ValueKind == JsonValueKind.Number)
            {
                return current.GetDouble();
            }
        }
        catch {}
        return null;
    }

    private string GetCategoryValue(string json, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var current = doc.RootElement;
            var parts = path.Split('.');
            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
                {
                    current = next;
                }
                else
                {
                    return "Unknown";
                }
            }

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.TryGetProperty("S", out var sProp) && sProp.ValueKind == JsonValueKind.String)
                {
                    return sProp.GetString() ?? "Unknown";
                }
                if (current.TryGetProperty("B", out var bProp))
                {
                    return bProp.GetBoolean().ToString();
                }
                if (current.TryGetProperty("I", out var iProp))
                {
                    return iProp.GetInt64().ToString();
                }
            }
            else if (current.ValueKind == JsonValueKind.String)
            {
                return current.GetString() ?? "Unknown";
            }
            else if (current.ValueKind == JsonValueKind.True || current.ValueKind == JsonValueKind.False)
            {
                return current.GetBoolean().ToString();
            }
            else if (current.ValueKind == JsonValueKind.Number)
            {
                return current.GetDouble().ToString();
            }
        }
        catch {}
        return "Unknown";
    }
}
