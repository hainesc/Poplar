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
        return await _backend.ProductVm.GetTraceabilitySchema(productId);
    }

    public async Task UpdateTraceabilitySchemaAsync(int productId, TraceabilityItem[] items)
    {
        await EnsureInitialized();
        await _backend.ProductVm.UpdateTraceabilitySchema(productId, items);
    }
}
