using Electronica.Models;
using Electronica.Services;
using Microsoft.AspNetCore.Mvc;

namespace Electronica.Controllers;

// Тонкий контроллер: вся логика импорта — в IProductImportService
public class ProductsController : Controller
{
    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    // Список товаров с необязательным поиском, фильтром по категории и сортировкой
    [HttpGet]
    public ActionResult Index(string? q, string? category, string? sort)
    {
        var all = _repository.GetAll();
        var viewModel = new ProductsIndexViewModel
        {
            Products = ProductFilter.Apply(all, q, category, sort),
            AllProductsCount = all.Count,
            Categories = all.Select(p => p.CategoryName).Distinct().OrderBy(c => c).ToList(),
            Query = q?.Trim() ?? string.Empty,
            Category = category?.Trim() ?? string.Empty,
            Sort = sort ?? string.Empty
        };
        return View(viewModel);
    }

    [HttpGet]
    public ActionResult Import()
    {
        return View(new ImportViewModel());
    }

    // Импорт по пути к файлу (абсолютному или относительно wwwroot)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Import(string? filePath)
    {
        var resolvedPath = ResolveFilePath(filePath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
            return View("Import", ErrorModel(filePath, "Укажите путь к CSV-файлу"));

        var result = _importService.ImportFromCsv(resolvedPath);
        return View("Import", new ImportViewModel { FilePath = filePath ?? string.Empty, Result = result });
    }

    // Очищает список товаров в памяти, чтобы импорт можно было запустить заново
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Clear()
    {
        _repository.Clear();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public ActionResult Error()
    {
        return View();
    }

   
    // Путь вида /csv/file.csv ищется в wwwroot; полный путь (C:\data\file.csv или /home/user/file.csv) берётся как есть.
   
    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();
        var inWebRoot = Path.Combine(_environment.WebRootPath ?? string.Empty, trimmed.TrimStart('/', '\\'));

        if (!Path.IsPathRooted(trimmed))
            return inWebRoot;

        // «/csv/sample.csv» формально считается абсолютным путём, поэтому сначала проверяем wwwroot.
        return System.IO.File.Exists(inWebRoot) ? inWebRoot : trimmed;
    }

    private static ImportViewModel ErrorModel(string? filePath, string message)
    {
        var result = new ImportResult();
        result.Errors.Add(new ImportError { RowNumber = 0, Message = message });
        return new ImportViewModel { FilePath = filePath ?? string.Empty, Result = result };
    }
}
