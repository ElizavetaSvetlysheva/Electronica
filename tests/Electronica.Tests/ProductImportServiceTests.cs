using Electronica.Models;
using Electronica.Services;
using Xunit;

namespace Electronica.Tests;

public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

    private const string ValidLaptop =
        "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15";

    private const string ValidMouse =
        "1002,Мышь Logitech M185,Беспроводная мышь,1290,RUB,Периферия,10,/images/mouse.jpg,true,Logitech,12,M185";

    private static (ImportResult Result, InMemoryProductRepository Repo) Run(params string[] rows)
    {
        var repo = new InMemoryProductRepository();
        var result = new ProductImportService(repo).ImportFromLines(new[] { Header }.Concat(rows));
        return (result, repo);
    }ы

    private static void AssertSingleRejected(string row, string expectedMessage)
    {
        var (result, repo) = Run(row);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, repo.Count);
        Assert.Contains(result.Errors, e => e.Message.Contains(expectedMessage));
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");

    [Fact]
    public void Import_OneValidRow()
    {
        var (result, repo) = Run(ValidLaptop);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var p = repo.GetBySku("1001");
        Assert.NotNull(p);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", p!.Name);
        Assert.Equal(45990m, p.Price);
        Assert.Equal("Lenovo", p.Brand);
        Assert.Equal(24, p.WarrantyMonths);
        Assert.Equal("IdeaPad3-15", p.ModelNumber);
    }

    [Fact]
    public void Import_SeveralValidRows()
    {
        var (result, repo) = Run(ValidLaptop, ValidMouse);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repo.Count);
    }

    [Fact]
    public void ImportFromCsv_FileNotFound()
    {
        var result = new ProductImportService(new InMemoryProductRepository()).ImportFromCsv(TempPath());

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Файл не найден", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromCsv_EmptyFile()
    {
        var path = TempPath();
        File.WriteAllText(path, string.Empty);
        try
        {
            var result = new ProductImportService(new InMemoryProductRepository()).ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Import_HeaderOnly()
    {
        var (result, _) = Run();

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
    }

    [Fact]
    public void Import_WrongHeader()
    {
        var repo = new InMemoryProductRepository();
        var result = new ProductImportService(repo).ImportFromLines(new[]
        {
            "sku,name,description,price,currency,category_name,stock,image_url,is_active"
        });

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, e => e.Message.Contains("Неверные заголовки CSV"));
    }

    [Fact]
    public void Import_WrongFieldCount()
    {
        AssertSingleRejected("1001,Ноутбук Lenovo IdeaPad 3", "Неверное количество полей в строке");
    }

    [Fact]
    public void Import_EmptySku() =>
        AssertSingleRejected(",Ноутбук,Описание,45990,RUB,Компьютеры,5,/i.jpg,true,Lenovo,24,M", "Поле sku не может быть пустым");

    [Fact]
    public void Import_EmptyName() =>
        AssertSingleRejected("1001,,Описание,45990,RUB,Компьютеры,5,/i.jpg,true,Lenovo,24,M", "Поле name не может быть пустым");

    [Fact]
    public void Import_NegativePrice() =>
        AssertSingleRejected("1003,Клавиатура,Описание,-500,RUB,Периферия,7,/i.jpg,true,A4Tech,24,V3M", "Поле price должно быть числом больше 0");

    [Fact]
    public void Import_NonNumericPrice() =>
        AssertSingleRejected("1003,Клавиатура,Описание,abc,RUB,Периферия,7,/i.jpg,true,A4Tech,24,V3M", "Поле price должно быть числом больше 0");

    [Fact]
    public void Import_NegativeStock() =>
        AssertSingleRejected("1004,Монитор,Описание,29990,RUB,Компьютеры,-5,/i.jpg,true,Dell,24,S2722QC", "Остаток на складе должен быть целым числом не меньше 0");

    [Fact]
    public void Import_BadIsActive() =>
        AssertSingleRejected("1007,Webcam,Описание,4990,RUB,Периферия,6,/i.jpg,yes,Logitech,24,C920", "Поле is_active должно иметь значение true, false, 1 или 0");

    [Fact]
    public void Import_MissingBrand() =>
        AssertSingleRejected("1005,Видеокарта,,24990,RUB,Компьютеры,4,/i.jpg,true,,36,RTX3060", "Поле brand обязательно для варианта «Электроника»");

    [Fact]
    public void Import_WarrantyBelowZero() =>
        AssertSingleRejected("1006,SSD,Описание,5990,RUB,Компьютеры,12,/i.jpg,true,Samsung,-1,870", "Гарантия должна быть целым числом от 0 до 60 месяцев");

    [Fact]
    public void Import_WarrantyAbove60() =>
        AssertSingleRejected("1006,SSD,Описание,5990,RUB,Компьютеры,12,/i.jpg,true,Samsung,61,870", "Гарантия должна быть целым числом от 0 до 60 месяцев");

    [Fact]
    public void Import_DuplicateSkuInsideFile()
    {
        var (result, repo) = Run(
            ValidLaptop,
            "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
        Assert.Equal(3, result.Errors[0].RowNumber);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", repo.GetBySku("1001")!.Name);
    }

    [Fact]
    public void Import_DuplicateSkuWithRepository()
    {
        var repo = new InMemoryProductRepository();
        repo.Add(new Product { Sku = "1001", Name = "Старый", Brand = "X" });

        var result = new ProductImportService(repo).ImportFromLines(new[]
        {
            Header,
            "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook"
        });

        Assert.Equal(1, repo.Count);
        Assert.Equal(0, result.ImportedCount);
        Assert.Contains(result.Errors, e => e.Message.Contains("Артикул товара уже существует"));
    }

    [Fact]
    public void Import_SeveralErrorsInOneRow()
    {
        // 12 полей: sku, name, price, stock, is_active, brand, warranty — все некорректны
        var (result, repo) = Run(",,Некорректная строка,-10,RUB,Компьютеры,-5,/images/x.jpg,yes,,61,");

        Assert.Equal(0, result.ImportedCount);
        Assert.True(result.ErrorCount >= 4);
        Assert.Equal(0, repo.Count);
    }

    [Fact]
    public void Import_EmptyOptionalFieldsAllowed()
    {
        var (result, repo) = Run("1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550");

        Assert.Empty(result.Errors);
        var p = repo.GetBySku("1011")!;
        Assert.Equal(string.Empty, p.Description);
        Assert.Equal(string.Empty, p.ImageUrl);
    }

    [Fact]
    public void Import_EmptyCurrency_BecomesRub()
    {
        var (_, repo) = Run("1011,Блок питания,,4990,,Компьютеры,9,,true,Corsair,60,CV550");

        Assert.Equal("RUB", repo.GetBySku("1011")!.Currency);
    }

    [Fact]
    public void Import_EmptyCategory_BecomesDefault()
    {
        var (_, repo) = Run("1011,Блок питания,,4990,RUB,,9,,true,Corsair,60,CV550");

        Assert.Equal("Без категории", repo.GetBySku("1011")!.CategoryName);
    }

    [Fact]
    public void Import_CyrillicPreserved()
    {
        var (_, repo) = Run(ValidLaptop);
        var p = repo.GetBySku("1001")!;

        Assert.Equal("Ноутбук Lenovo IdeaPad 3", p.Name);
        Assert.Equal("Бюджетный ноутбук для работы и учебы", p.Description);
        Assert.Equal("Компьютеры", p.CategoryName);
    }
}
