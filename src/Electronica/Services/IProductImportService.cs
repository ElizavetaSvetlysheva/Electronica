using Electronica.Models;

namespace Electronica.Services;

public interface IProductImportService
{
    ImportResult ImportFromCsv(string filePath);
    ImportResult ImportFromLines(IEnumerable<string> lines);
}
