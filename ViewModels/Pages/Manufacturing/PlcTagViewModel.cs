using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class PlcTagViewModel : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private int? _productId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private PlcTagDirection _direction = PlcTagDirection.PlcToHmi;
    [ObservableProperty] private int _byteOffset;
    [ObservableProperty] private int _bitOffset;
    [ObservableProperty] private string _valueType = "bool";
    [ObservableProperty] private string? _description;
    [ObservableProperty] private long _createdAt;
    [ObservableProperty] private int _workspaceId;

    public PlcTagViewModel() { }

    public PlcTagViewModel(PlcTagRecord record)
    {
        Id = record.id;
        ProductId = record.productId;
        Name = record.name;
        Direction = record.direction;
        ByteOffset = record.byteOffset;
        BitOffset = record.bitOffset;
        ValueType = record.valueType;
        Description = record.description;
        CreatedAt = record.createdAt;
        WorkspaceId = record.workspaceId;
    }

    public PlcTagRecord ToRecord()
    {
        return new PlcTagRecord(
            Id,
            ProductId,
            Name,
            Direction,
            ByteOffset,
            BitOffset,
            ValueType,
            Description,
            CreatedAt,
            WorkspaceId
        );
    }
}
