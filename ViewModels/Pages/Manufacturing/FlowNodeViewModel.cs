using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class FlowNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _stepType = string.Empty;

    [ObservableProperty]
    private Point _location;

    // We store the original FFI record so we don't lose any data
    // when converting back (e.g. staticParams, mappings, retryPolicy).
    public DagNode OriginalRecord { get; set; }

    public FlowNodeViewModel(DagNode record)
    {
        OriginalRecord = record;
        Id = record.id;
        StepType = record.stepType;
    }
}

public partial class FlowConnectionViewModel : ObservableObject
{
    [ObservableProperty]
    private FlowNodeViewModel? _source;

    [ObservableProperty]
    private FlowNodeViewModel? _target;

    [ObservableProperty]
    private EdgeCondition _condition;

    public FlowConnectionViewModel(FlowNodeViewModel source, FlowNodeViewModel target, EdgeCondition condition)
    {
        Source = source;
        Target = target;
        Condition = condition;
    }
}
