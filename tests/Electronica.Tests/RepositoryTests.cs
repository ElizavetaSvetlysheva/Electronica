using Electronica.Models;
using Electronica.Services;
using Xunit;

namespace Electronica.Tests;

public class RepositoryTests
{
    private static Product Make(string sku = "1001") =>
        new() { Sku = sku, Name = "Ноутбук Lenovo IdeaPad 3", Price = 45990m, Brand = "Lenovo" };

    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(Make());

        var all = repository.GetAll();

        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void GetBySku_ReturnsProduct()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(Make());

        Assert.NotNull(repository.GetBySku("1001"));
    }

    [Theory]
    [InlineData(" 1001 ")]
    [InlineData("1001")]
    public void GetBySku_IgnoresSpaces(string sku)
    {
        var repository = new InMemoryProductRepository();
        repository.Add(Make());

        Assert.NotNull(repository.GetBySku(sku));
    }

    [Fact]
    public void GetBySku_IgnoresCase()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(Make("ABC-1"));

        Assert.NotNull(repository.GetBySku("abc-1"));
    }

    [Fact]
    public void EmptyRepository_HasNoProducts()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
        Assert.Null(repository.GetBySku("1001"));
    }

    [Fact]
    public void GetAll_ReturnsSnapshot()
    {
        var repository = new InMemoryProductRepository();
        var snapshot = repository.GetAll();
        repository.Add(Make());

        Assert.Empty(snapshot);
    }

    [Fact]
    public void Add_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new InMemoryProductRepository().Add(null!));
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(Make());
        repository.Clear();

        Assert.Equal(0, repository.Count);
    }
}
