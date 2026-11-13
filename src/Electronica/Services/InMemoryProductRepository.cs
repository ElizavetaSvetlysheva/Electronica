using Electronica.Models;

namespace Electronica.Services;

// Потокобезопасное хранилище товаров в памяти (регистрируется как singleton)
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _products.Count;
            }
        }
    }

    public void Add(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        lock (_lock)
        {
            _products.Add(product);
        }
    }

    // Возвращает снимок списка: внешние изменения не влияют на хранилище.
    public IReadOnlyList<Product> GetAll()
    {
        lock (_lock)
        {
            return _products.ToList();
        }
    }

    // Поиск по артикулу без учёта регистра и крайних пробелов
    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        lock (_lock)
        {
            return _products.FirstOrDefault(p =>
                string.Equals(p.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _products.Clear();
        }
    }
}
