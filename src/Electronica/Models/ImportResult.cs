namespace Electronica.Models;

// Результат импорта CSV-файла
public class ImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public int ErrorCount => Errors.Count;
}
