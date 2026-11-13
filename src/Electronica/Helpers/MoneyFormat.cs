using System.Globalization;

namespace Electronica.Helpers;

// Форматирование денежных сумм для отображения (не теряет копейки, например 1 290,50)
public static class MoneyFormat
{
    private static readonly CultureInfo Culture = new("ru-RU");

    public static string Amount(decimal value) =>
        value == decimal.Truncate(value)
            ? value.ToString("N0", Culture)
            : value.ToString("N2", Culture);

    public static string WithCurrency(decimal value, string currency)
    {
        var symbol = string.Equals(currency, "RUB", StringComparison.OrdinalIgnoreCase) ? "₽" : currency;
        return $"{Amount(value)}\u00A0{symbol}";
    }
}
