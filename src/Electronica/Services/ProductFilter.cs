using Electronica.Models;

namespace Electronica.Services;

// Поиск, фильтрация по категории и сортировка списка товаров (необязательные функции задания)
public static class ProductFilter
{
    public const string SortDefault = "";
    public const string SortPriceAsc = "price_asc";
    public const string SortPriceDesc = "price_desc";
    public const string SortStockDesc = "stock_desc";
    public const string SortName = "name";

    // Применяет поиск по названию/артикулу/бренду, категорию и сортировку
    public static IReadOnlyList<Product> Apply(
        IEnumerable<Product> products, string? query, string? category, string? sort)
    {
        var items = products ?? Enumerable.Empty<Product>();

        var text = query?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            items = items.Where(p =>
                Contains(p.Name, text) || Contains(p.Sku, text) ||
                Contains(p.Brand, text) || Contains(p.ModelNumber, text));
        }

        var cat = category?.Trim();
        if (!string.IsNullOrEmpty(cat))
            items = items.Where(p => string.Equals(p.CategoryName, cat, StringComparison.OrdinalIgnoreCase));

        items = sort switch
        {
            SortPriceAsc => items.OrderBy(p => p.Price),
            SortPriceDesc => items.OrderByDescending(p => p.Price),
            SortStockDesc => items.OrderByDescending(p => p.StockQuantity),
            SortName => items.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => items
        };

        return items.ToList();
    }

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.CurrentCultureIgnoreCase);
}
