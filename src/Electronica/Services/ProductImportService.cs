using System.Globalization;
using System.Text;
using Electronica.Models;

namespace Electronica.Services;

// Импорт товаров (вариант «Электроника») из CSV в репозиторий
public class ProductImportService : IProductImportService
{
    public const string DefaultCurrency = "RUB";
    public const string DefaultCategory = "Без категории";
    public const int MinWarrantyMonths = 0;
    public const int MaxWarrantyMonths = 60;

    private const string MsgFileNotFound = "Файл не найден";
    private const string MsgNoData = "Данные для импорта отсутствуют";
    private const string MsgBadHeader = "Неверные заголовки CSV";
    private const string MsgBadFieldCount = "Неверное количество полей в строке";
    private const string MsgDuplicate = "Артикул товара уже существует";
    private const string MsgSku = "Поле sku не может быть пустым";
    private const string MsgName = "Поле name не может быть пустым";
    private const string MsgPrice = "Поле price должно быть числом больше 0";
    private const string MsgStock = "Остаток на складе должен быть целым числом не меньше 0";
    private const string MsgActive = "Поле is_active должно иметь значение true, false, 1 или 0";
    private const string MsgBrand = "Поле brand обязательно для варианта «Электроника»";
    private const string MsgWarranty = "Гарантия должна быть целым числом от 0 до 60 месяцев";

    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency", "category_name",
        "stock", "image_url", "is_active", "brand", "warranty_months", "model_number"
    };

    private static readonly Encoding Windows1251 = CreateWindows1251();

    private readonly IProductRepository _repository;

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // Читает файл с диска и запускает импорт. Не бросает исключений на ошибках ввода-вывода
    public ImportResult ImportFromCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return SingleError(MsgFileNotFound);

        try
        {
            return ImportFromLines(ReadLines(filePath));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return SingleError($"Не удалось прочитать файл: {ex.Message}");
        }
    }

    // Читает строки файла: сначала как UTF-8 (с BOM или без), а если файл не UTF-8 — как Windows-1251,
    // чтобы кириллица не превращалась в «кракозябры».
  
    private static IEnumerable<string> ReadLines(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Windows1251.GetString(bytes);
        }

        text = text.TrimStart('\uFEFF');
        return text.Length == 0 ? Array.Empty<string>() : text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    }

    // Основная логика импорта (не зависит от файловой системы)
    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var all = lines?.ToList() ?? new List<string>();

        var headerIndex = all.FindIndex(l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
            return SingleError(MsgNoData);

        var headerFields = CsvLineParser.Parse(all[headerIndex].TrimStart('\uFEFF'));
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerFields.Count; i++)
            columnMap.TryAdd(headerFields[i].Trim(), i);

        if (RequiredColumns.Any(c => !columnMap.ContainsKey(c)))
        {
            result.Errors.Add(new ImportError { RowNumber = headerIndex + 1, Message = MsgBadHeader });
            return result;
        }

        var expectedFields = headerFields.Count;
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = headerIndex + 1; index < all.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(all[index]))
                continue;

            result.TotalRows++;
            var rowNumber = index + 1;
            var values = CsvLineParser.Parse(all[index]);

            if (values.Count != expectedFields)
            {
                result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = MsgBadFieldCount });
                continue;
            }

            var rowErrors = new List<ImportError>();
            var product = ParseRow(columnMap, values, rowNumber, rowErrors);

            if (product == null)
            {
                result.Errors.AddRange(rowErrors);
                continue;
            }

            if (_repository.GetBySku(product.Sku) != null || importedSkus.Contains(product.Sku))
            {
                result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = MsgDuplicate });
                continue;
            }

            _repository.Add(product);
            importedSkus.Add(product.Sku);
            result.ImportedCount++;
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
            result.Errors.Add(new ImportError { RowNumber = 0, Message = MsgNoData });

        return result;
    }

    // Валидирует строку и создаёт Product; при ошибках возвращает null и заполняет errors
    private static Product? ParseRow(
        Dictionary<string, int> columnMap, IReadOnlyList<string> values, int rowNumber, List<ImportError> errors)
    {
        string Get(string column) =>
            columnMap.TryGetValue(column, out var i) && i < values.Count ? values[i].Trim() : string.Empty;

        void Fail(string message) => errors.Add(new ImportError { RowNumber = rowNumber, Message = message });

        var sku = Get("sku");
        if (string.IsNullOrWhiteSpace(sku)) Fail(MsgSku);

        var name = Get("name");
        if (string.IsNullOrWhiteSpace(name)) Fail(MsgName);

        if (!TryParseDecimal(Get("price"), out var price) || price <= 0) Fail(MsgPrice);

        if (!TryParseInt(Get("stock"), out var stock) || stock < 0) Fail(MsgStock);

        if (!TryParseBool(Get("is_active"), out var isActive)) Fail(MsgActive);

        var brand = Get("brand");
        if (string.IsNullOrWhiteSpace(brand)) Fail(MsgBrand);

        if (!TryParseInt(Get("warranty_months"), out var warranty)
            || warranty < MinWarrantyMonths || warranty > MaxWarrantyMonths) Fail(MsgWarranty);

        if (errors.Count > 0)
            return null;

        return new Product
        {
            Sku = sku,
            Name = name,
            Description = Get("description"),
            Price = price,
            Currency = NormalizeCurrency(Get("currency")),
            CategoryName = NormalizeCategory(Get("category_name")),
            StockQuantity = stock,
            ImageUrl = Get("image_url"),
            IsActive = isActive,
            Brand = brand,
            WarrantyMonths = warranty,
            ModelNumber = Get("model_number")
        };
    }

    private static Encoding CreateWindows1251()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251);
    }

    private static ImportResult SingleError(string message)
    {
        var result = new ImportResult();
        result.Errors.Add(new ImportError { RowNumber = 0, Message = message });
        return result;
    }

    private static string NormalizeCurrency(string value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultCurrency : value.Trim().ToUpperInvariant();

    private static string NormalizeCategory(string value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultCategory : value.Trim();

    private static bool TryParseDecimal(string value, out decimal result) =>
        decimal.TryParse(value.Replace(',', '.'),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out result);

    private static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
                result = true;
                return true;
            case "false":
            case "0":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }
}
