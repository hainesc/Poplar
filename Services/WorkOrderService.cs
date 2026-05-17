using uniffi.stump;
using System.Threading.Tasks;

namespace Poplar.Services;

/// <summary>
/// Service for managing manufacturing work orders and batch production.
/// </summary>
public sealed class WorkOrderService
{
    private readonly BackendService _backend;

    public WorkOrderService(BackendService backend)
    {
        _backend = backend;
    }

    private async Task EnsureInitialized()
    {
        if (!_backend.IsInitialized)
        {
            await _backend.InitializeAsync();
        }
    }

    /// <summary>
    /// Retrieves all historical and current work orders.
    /// </summary>
    public async Task<WorkOrder[]> GetWorkOrdersAsync()
    {
        await EnsureInitialized();
        return await _backend.WorkOrderVm.GetHistory();
    }

    /// <summary>
    /// Launches a new production batch (adds a new work order).
    /// </summary>
    public async Task<int> AddWorkOrderAsync(string title, int productId, int quantity)
    {
        await EnsureInitialized();
        return await _backend.WorkOrderVm.AddWorkOrder(title, productId, quantity);
    }
}
