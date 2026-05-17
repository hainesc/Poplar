using uniffi.stump;
using Poplar.Models;
using System.Diagnostics;

namespace Poplar.Services;

/// <summary>
/// Service for managing products, PLC tags, and traceability schemas.
/// </summary>
public sealed class ProductService
{
    private readonly BackendService _backend;

    public ProductService(BackendService backend)
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

    public async Task<Product[]> GetProductsAsync()
    {
        await EnsureInitialized();
        return await _backend.ProductVm.ListProducts();
    }

    public async Task<int> AddProductAsync(string name, string[]? deviceNames = null)
    {
        await EnsureInitialized();
        return await _backend.ProductVm.AddProduct(null, name, deviceNames);
    }

    public async Task UpdateProductAsync(int id, string name, string[]? deviceNames = null)
    {
        await EnsureInitialized();
        await _backend.ProductVm.UpdateProduct(id, name, deviceNames);
    }

    public async Task LinkDeviceAsync(int productId, int deviceId)
    {
        await EnsureInitialized();
        await _backend.ProductVm.LinkDevice(productId, deviceId);
    }

    public async Task UnlinkDeviceAsync(int productId, int deviceId)
    {
        await EnsureInitialized();
        await _backend.ProductVm.UnlinkDevice(productId, deviceId);
    }

    public async Task DeleteProductAsync(int id)
    {
        await EnsureInitialized();
        await _backend.ProductVm.DeleteProduct(id);
    }

    // Tag Management
    public async Task<PlcTagRecord[]> GetProductTagsAsync(int productId)
    {
        await EnsureInitialized();
        return await _backend.ProductVm.GetTagsByProduct(productId);
    }

    public async Task SyncProductTagsAsync(int productId, PlcTagRecord[] tags)
    {
        await EnsureInitialized();
        await _backend.ProductVm.UpdateTagsForProduct(productId, tags);
    }

    // Traceability
    public async Task<TraceabilityItem[]> GetTraceabilitySchemaAsync(int productId)
    {
        await EnsureInitialized();
        return await _backend.TraceabilityVm.GetTraceabilitySchema(productId);
    }

    public async Task UpdateTraceabilitySchemaAsync(int productId, TraceabilityItem[] items)
    {
        await EnsureInitialized();
        await _backend.TraceabilityVm.UpdateTraceabilitySchema(productId, items);
    }

    // Process Flow (DAG)
    public async Task<DagFlow?> GetDagByProductAsync(int productId)
    {
        await EnsureInitialized();
        return await _backend.ProductVm.GetDagByProduct(productId);
    }

    public async Task UpdateDagByProductAsync(int productId, DagFlow dag)
    {
        await EnsureInitialized();
        await _backend.ProductVm.UpdateDagByProduct(productId, dag);
    }

    public async Task<StepMetadata[]> GetStepTypesAsync()
    {
        await EnsureInitialized();
        return await Task.Run(() => _backend.ManufacturingVm.GetStepTypes());
    }
}
