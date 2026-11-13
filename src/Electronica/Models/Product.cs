namespace Electronica.Models;

// Товар интернет-магазина (вариант 1 «Электроника»)
public class Product
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "RUB";
    public string CategoryName { get; set; } = "Без категории";
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Вариант 1: Электроника
    public string Brand { get; set; } = string.Empty;
    public int WarrantyMonths { get; set; }
    public string ModelNumber { get; set; } = string.Empty;
}
