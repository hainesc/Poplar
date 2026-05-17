using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using uniffi.stump;
using Poplar.Services;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class WorkOrderCreateViewModel : ObservableObject
{
    [ObservableProperty]
    private string _orderTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private int _quantity = 100;

    public async Task InitializeAsync(ProductService productService)
    {
        try
        {
            var prods = await productService.GetProductsAsync();
            Products.Clear();
            foreach (var p in prods)
            {
                Products.Add(p);
            }

            if (Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WorkOrderCreateViewModel] Failed to load products: {ex.Message}");
        }
    }
}
