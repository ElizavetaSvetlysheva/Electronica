using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Electronica.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Кириллица в HTML выводится как есть, а не в виде &#x...; кодами.
builder.Services.AddWebEncoders(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Singleton: данные хранятся в памяти и должны жить между HTTP-запросами.
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IProductImportService, ProductImportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Products/Error");
}

// Единый формат чисел (45 990,50) независимо от региональных настроек сервера.
var culture = new CultureInfo("ru-RU");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
});

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
