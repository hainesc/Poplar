// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Poplar.Models;

namespace Poplar.ViewModels.Pages.Collections;

public partial class DataGridViewModel : ViewModel
{
    [ObservableProperty]
    private ObservableCollection<SampleProduct> _productsCollection = GenerateProducts();

    private static ObservableCollection<SampleProduct> GenerateProducts()
    {
        var random = new Random();
        var products = new ObservableCollection<SampleProduct> { };

        var adjectives = new[] { "Red", "Blueberry" };
        var names = new[] { "Marmalade", "Dumplings", "Soup" };
        Unit[] units = [Unit.Grams, Unit.Kilograms, Unit.Milliliters];

        for (int i = 0; i < 50; i++)
        {
            products.Add(
                new SampleProduct
                {
                    ProductId = i,
                    ProductCode = i,
                    ProductName =
                        adjectives[random.Next(0, adjectives.Length)]
                        + " "
                        + names[random.Next(0, names.Length)],
                    Unit = units[random.Next(0, units.Length)],
                    UnitPrice = Math.Round(random.NextDouble() * 20.0, 3),
                    UnitsInStock = random.Next(0, 100),
                    IsVirtual = random.Next(0, 2) == 1,
                }
            );
        }

        return products;
    }
}

public class SampleProduct
{
    public int ProductId { get; set; }
    public int ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? QuantityPerUnit { get; set; }
    public Unit Unit { get; set; }
    public double UnitPrice { get; set; }
    public string UnitPriceString => UnitPrice.ToString("F2");
    public int UnitsInStock { get; set; }
    public bool IsVirtual { get; set; }
}
