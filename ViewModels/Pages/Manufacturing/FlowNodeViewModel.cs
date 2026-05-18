using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;
using System.Linq;
using System.Collections.Generic;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class FlowNodeViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _stepType = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isEntry;
    [ObservableProperty] private bool _isActiveStep;
    [ObservableProperty] private Point _location;
    [ObservableProperty] private Point _inputAnchor;
    [ObservableProperty] private Point _outputAnchor;

    // Rich collections for editing
    public ObservableCollection<StepParamViewModel> Parameters { get; } = new();
    public ObservableCollection<DataMappingViewModel> OutputToContext { get; } = new();
    public ObservableCollection<DataMappingViewModel> OutputToTrace { get; } = new();
    public List<string> AvailableOutputs { get; } = new();
    
    [ObservableProperty] private StepRetryPolicyViewModel? _retryPolicy;

    public DagNode OriginalRecord { get; set; }

    public FlowNodeViewModel(DagNode record, StepMetadata? metadata = null, bool isEntry = false)
    {
        OriginalRecord = record;
        Id = record.id;
        StepType = record.stepType;
        DisplayName = metadata?.displayName ?? record.stepType;
        IsEntry = isEntry;

        if (metadata?.outputData != null)
        {
            AvailableOutputs = metadata.outputData.Select(o => o.name).ToList();
        }

        // Initialize Parameters from metadata and record
        if (metadata != null)
        {
            foreach (var paramInfo in metadata.inputParams)
            {
                var pvm = new StepParamViewModel(paramInfo);
                
                // If there's an input mapping, use it
                if (record.inputMappings != null && record.inputMappings.TryGetValue(paramInfo.name, out var mapping))
                {
                    pvm.IsMapped = true;
                    pvm.MappingKey = mapping;
                }
                // Otherwise check for static param
                else if (record.staticParams != null && record.staticParams.TryGetValue(paramInfo.name, out var staticVal))
                {
                    pvm.IsMapped = false;
                    pvm.Value = GenericValueHelper.Unwrap(staticVal);
                }

                Parameters.Add(pvm);
            }
        }

        // Initialize Mappings
        if (record.outputToContext != null)
        {
            foreach (var m in record.outputToContext)
                OutputToContext.Add(new DataMappingViewModel(m.sourceKey, m.targetKey));
        }

        if (record.outputToTrace != null)
        {
            foreach (var m in record.outputToTrace)
                OutputToTrace.Add(new DataMappingViewModel(m.sourceKey, m.targetKey));
        }

        if (record.retryPolicy != null)
        {
            RetryPolicy = new StepRetryPolicyViewModel(record.retryPolicy);
        }
    }

    [RelayCommand]
    private void AddOutputMapping() => OutputToContext.Add(new DataMappingViewModel());

    [RelayCommand]
    private void DeleteOutputMapping(DataMappingViewModel m) => OutputToContext.Remove(m);

    [RelayCommand]
    private void AddTraceMapping() => OutputToTrace.Add(new DataMappingViewModel());

    [RelayCommand]
    private void DeleteTraceMapping(DataMappingViewModel m) => OutputToTrace.Remove(m);

    public DagNode ToRecord()
    {
        var staticParams = new Dictionary<string, GenericValue>();
        var inputMappings = new Dictionary<string, string>();

        foreach (var p in Parameters)
        {
            if (p.IsMapped)
            {
                inputMappings[p.Name] = p.MappingKey;
            }
            else
            {
                staticParams[p.Name] = GenericValueHelper.Wrap(p.Value, p.Type);
            }
        }

        return new DagNode(
            Id,
            StepType,
            staticParams,
            inputMappings,
            OutputToContext.Select(m => m.ToRecord()).ToArray(),
            OutputToTrace.Select(m => m.ToRecord()).ToArray(),
            OriginalRecord.edges, // Edges are managed by DagEditorViewModel
            RetryPolicy?.ToRecord()
        );
    }
}

public partial class StepParamViewModel : ObservableObject
{
    public string Name { get; }
    public string DataType { get; }
    public string ParamType { get; }
    public string Type { get; }
    public string Description { get; }

    [ObservableProperty] private bool _isMapped;
    [ObservableProperty] private string _mappingKey = string.Empty;
    [ObservableProperty] private object? _value;

    [RelayCommand]
    private void ToggleMapping()
    {
        IsMapped = !IsMapped;
    }

    public StepParamViewModel(ParamInfo info)
    {
        Name = info.name;
        DataType = info.dataType;
        ParamType = info.paramType.ToString();
        Type = info.paramType.ToString(); // Backwards compatibility for GenericValueHelper wrap
        Description = info.description;
    }
}

public partial class DataMappingViewModel : ObservableObject
{
    [ObservableProperty] private string _sourceKey = string.Empty;
    [ObservableProperty] private string _targetKey = string.Empty;

    public DataMappingViewModel() { }
    public DataMappingViewModel(string source, string target)
    {
        SourceKey = source;
        TargetKey = target;
    }

    public DataMapping ToRecord() => new DataMapping(SourceKey, TargetKey);
}

public partial class StepRetryPolicyViewModel : ObservableObject
{
    [ObservableProperty] private int _maxRetries = 3;
    [ObservableProperty] private int _intervalMs = 1000;

    public StepRetryPolicyViewModel() { }
    public StepRetryPolicyViewModel(RetryPolicy policy)
    {
        MaxRetries = policy.maxRetries;
        IntervalMs = policy.intervalMs;
    }

    public RetryPolicy ToRecord() => new RetryPolicy(MaxRetries, IntervalMs, new RetryCondition[0]); // Simplified conditions for now
}

public static class GenericValueHelper
{
    public static object? Unwrap(GenericValue gv)
    {
        return gv switch
        {
            GenericValue.S s => s.v1,
            GenericValue.I i => i.v1,
            GenericValue.F f => f.v1,
            GenericValue.B b => b.v1,
            GenericValue.A a => a.v1.bit > 0 ? $"{a.v1.offset}.{a.v1.bit}" : $"{a.v1.offset}",
            _ => null
        };
    }

    public static GenericValue Wrap(object? val, string type)
    {
        return type.ToLower() switch
        {
            "int" => new GenericValue.I(int.TryParse(val?.ToString(), out var i) ? i : 0),
            "float" => new GenericValue.F(float.TryParse(val?.ToString(), out var f) ? f : 0f),
            "bool" => new GenericValue.B(val is bool b && b),
            "plcaddress" => ParseAddress(val?.ToString()),
            _ => new GenericValue.S(val?.ToString() ?? string.Empty)
        };
    }

    private static GenericValue.A ParseAddress(string? addr)
    {
        if (string.IsNullOrEmpty(addr)) return new GenericValue.A(new PlcAddress(0, 0));
        var parts = addr.Split('.');
        int offset = int.TryParse(parts[0], out var o) ? o : 0;
        int bit = parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 0;
        return new GenericValue.A(new PlcAddress(offset, (byte)bit));
    }
}

public partial class FlowConnectionViewModel : ObservableObject
{
    [ObservableProperty] private FlowNodeViewModel? _source;
    [ObservableProperty] private FlowNodeViewModel? _target;
    [ObservableProperty] private bool _isSelected;
    
    // UI Properties for Edge Condition
    [ObservableProperty] private string _conditionType = "Fallback";
    
    [ObservableProperty] private int _statusValue = 200;
    
    [ObservableProperty] private string _variableKey = string.Empty;
    [ObservableProperty] private string _variableOperator = "==";
    [ObservableProperty] private string _variableExpectedValue = "0";

    public FlowConnectionViewModel(FlowNodeViewModel source, FlowNodeViewModel target, EdgeCondition condition)
    {
        Source = source;
        Target = target;
        InitializeFromCondition(condition);
    }
    
    private void InitializeFromCondition(EdgeCondition condition)
    {
        if (condition is EdgeCondition.Status status)
        {
            ConditionType = "Status";
            StatusValue = status.v1;
        }
        else if (condition is EdgeCondition.Variable variable)
        {
            ConditionType = "Variable";
            VariableKey = variable.key;
            VariableOperator = variable.@operator;
            VariableExpectedValue = GenericValueHelper.Unwrap(variable.expected)?.ToString() ?? "0";
        }
        else
        {
            ConditionType = "Fallback";
        }
    }

    public EdgeCondition ToEdgeCondition()
    {
        return ConditionType switch
        {
            "Status" => new EdgeCondition.Status(StatusValue),
            "Variable" => new EdgeCondition.Variable(VariableKey, VariableOperator, GenericValueHelper.Wrap(VariableExpectedValue, "int")),
            _ => new EdgeCondition.Fallback()
        };
    }
}
