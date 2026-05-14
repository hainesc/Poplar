using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class TraceabilityItemViewModel : ObservableObject
{
    [ObservableProperty] private int? _id;
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _dataType = "string";
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _workspaceId;

    public TraceabilityItemViewModel() { }

    public TraceabilityItemViewModel(TraceabilityItem record)
    {
        Id = record.id;
        ProductId = record.productId;
        Name = record.name;
        DataType = record.dataType;
        Description = record.description;
        WorkspaceId = record.workspaceId;
    }

    public TraceabilityItem ToRecord()
    {
        return new TraceabilityItem(
            Id,
            ProductId,
            Description,
            Name,
            DataType,
            WorkspaceId
        );
    }
}
