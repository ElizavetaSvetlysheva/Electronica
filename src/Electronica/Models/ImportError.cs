namespace Electronica.Models;

// Ошибка импорта. RowNumber = 0 для ошибок уровня файла
public class ImportError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
