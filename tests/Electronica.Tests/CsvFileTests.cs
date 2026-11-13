using System.Text;
using Electronica.Services;
using Xunit;

namespace Electronica.Tests;

// Тесты на готовых CSV-файлах из папки testdata
public class CsvFileTests
{
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "testdata", name);

    [Theory]
    [InlineData("valid_electronics.csv", 2, 2, 0, "")]
    [InlineData("invalid_price.csv", 1, 0, 1, "Поле price должно быть числом больше 0")]
    [InlineData("duplicate_sku.csv", 2, 1, 1, "Артикул товара уже существует")]
    [InlineData("missing_brand.csv", 1, 0, 1, "Поле brand обязательно для варианта «Электроника»")]
    [InlineData("warranty_out_of_range.csv", 1, 0, 1, "Гарантия должна быть целым числом от 0 до 60 месяцев")]
    [InlineData("bad_active.csv", 1, 0, 1, "Поле is_active должно иметь значение true, false, 1 или 0")]
    [InlineData("empty_file.csv", 0, 0, 1, "Данные для импорта отсутствуют")]
    public void ImportFromCsv_TestDataFiles(string file, int total, int imported, int errors, string message)
    {
        var result = new ProductImportService(new InMemoryProductRepository()).ImportFromCsv(Data(file));

        Assert.Equal(total, result.TotalRows);
        Assert.Equal(imported, result.ImportedCount);
        Assert.Equal(errors, result.ErrorCount);
        if (message.Length > 0)
            Assert.Contains(message, result.Errors[0].Message);
    }

    [Fact]
    public void InvalidPriceFile_ErrorIsOnRow2()
    {
        var result = new ProductImportService(new InMemoryProductRepository()).ImportFromCsv(Data("invalid_price.csv"));

        Assert.Equal(2, result.Errors[0].RowNumber);
    }

    [Fact]
    public void Windows1251File_IsDecodedCorrectly()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var text = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number\r\n" +
                   "1,Ноутбук,Описание,100,RUB,Компьютеры,1,,true,Lenovo,12,M\r\n";
        File.WriteAllBytes(path, Encoding.GetEncoding(1251).GetBytes(text));
        try
        {
            var repo = new InMemoryProductRepository();
            var result = new ProductImportService(repo).ImportFromCsv(path);

            Assert.Equal(1, result.ImportedCount);
            Assert.Equal("Ноутбук", repo.GetBySku("1")!.Name);
            Assert.Equal("Компьютеры", repo.GetBySku("1")!.CategoryName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Utf8FileWithBom_IsImported()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var text = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number\r\n" +
                   "1,Ноутбук,Описание,100,RUB,Компьютеры,1,,true,Lenovo,12,M\r\n";
        File.WriteAllText(path, text, new UTF8Encoding(true));
        try
        {
            var repo = new InMemoryProductRepository();
            var result = new ProductImportService(repo).ImportFromCsv(path);

            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
        }
        finally { File.Delete(path); }
    }
}
