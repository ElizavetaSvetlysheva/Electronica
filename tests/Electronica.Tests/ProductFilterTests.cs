using Electronica.Helpers;
using Electronica.Models;
using Electronica.Services;
using Xunit;

namespace Electronica.Tests;

public class ProductFilterTests
{
    private static List<Product> Sample() => new()
    {
        new Product { Sku = "1", Name = "Мышь Logitech", Brand = "Logitech", CategoryName = "Периферия", Price = 1290, StockQuantity = 10 },
        new Product { Sku = "2", Name = "Ноутбук Lenovo", Brand = "Lenovo", CategoryName = "Компьютеры", Price = 45990, StockQuantity = 5 },
        new Product { Sku = "3", Name = "Клавиатура", Brand = "A4Tech", CategoryName = "Периферия", Price = 3490, StockQuantity = 7 }
    };

    [Fact]
    public void NoFilters_ReturnsAllInOriginalOrder()
    {
        var result = ProductFilter.Apply(Sample(), null, null, null);

        Assert.Equal(new[] { "1", "2", "3" }, result.Select(p => p.Sku).ToArray());
    }

    [Fact]
    public void Query_SearchesByNameIgnoringCase()
    {
        var result = ProductFilter.Apply(Sample(), "  ЛОГИТЕХ ".Replace("ЛОГИТЕХ", "logitech"), null, null);

        Assert.Single(result);
        Assert.Equal("1", result[0].Sku);
    }

    [Fact]
    public void Category_FiltersProducts()
    {
        var result = ProductFilter.Apply(Sample(), null, "Периферия", null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Sort_ByPriceDescending()
    {
        var result = ProductFilter.Apply(Sample(), null, null, ProductFilter.SortPriceDesc);

        Assert.Equal(new[] { "2", "3", "1" }, result.Select(p => p.Sku).ToArray());
    }

    [Fact]
    public void Sort_ByPriceAscending()
    {
        var result = ProductFilter.Apply(Sample(), null, null, ProductFilter.SortPriceAsc);

        Assert.Equal("1", result[0].Sku);
    }

    [Fact]
    public void Query_WithNoMatches_ReturnsEmpty()
    {
        Assert.Empty(ProductFilter.Apply(Sample(), "нет такого", null, null));
    }

    [Fact]
    public void MoneyFormat_KeepsKopecksOnlyWhenNeeded()
    {
        Assert.Equal("1" + "\u00A0" + "290", MoneyFormat.Amount(1290m));
        Assert.Equal("1" + "\u00A0" + "290,50", MoneyFormat.Amount(1290.5m));
    }
}
