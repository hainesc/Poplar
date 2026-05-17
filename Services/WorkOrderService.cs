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

    /// <summary>
    /// Activates a work order for execution.
    /// </summary>
    public async Task ActivateWorkOrderAsync(int id)
    {
        await EnsureInitialized();
        await _backend.WorkOrderVm.ActivateWorkOrder(id);
    }

    /// <summary>
    /// Pauses an active work order.
    /// </summary>
    public async Task PauseWorkOrderAsync(int id)
    {
        await EnsureInitialized();
        await _backend.WorkOrderVm.PauseWorkOrder(id);
    }

    /// <summary>
    /// Stops / cancels a work order.
    /// </summary>
    public async Task StopWorkOrderAsync(int id)
    {
        await EnsureInitialized();
        await _backend.WorkOrderVm.StopWorkOrder(id);
    }
}
