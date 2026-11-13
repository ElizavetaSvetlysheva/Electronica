namespace Electronica.Models;

public class ProductsIndexViewModel
{
    // Товары после применения поиска, фильтра и сортировки
    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();

    // Сколько товаров всего хранится в памяти (без учёта фильтра)
    public int AllProductsCount { get; set; }

    public IReadOnlyList<string> Categories { get; set; } = new List<string>();
    public string Query { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty;

    public int TotalCount => Products.Count;
    public decimal TotalValue => Products.Sum(p => p.Price);
    public decimal StockValue => Products.Sum(p => p.Price * p.StockQuantity);
    public bool IsFiltered => Query.Length > 0 || Category.Length > 0;
}
