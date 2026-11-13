using System.Text;

namespace Electronica.Services;

// Разбор одной строки CSV (разделитель — запятая) с поддержкой кавычек
public static class CsvLineParser
{
    private const char Delimiter = ',';
    private const char Quote = '"';

    // Разбивает строку на поля; каждое поле обрезается по краям (trim)
    public static IReadOnlyList<string> Parse(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        line ??= string.Empty;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == Quote)
                {
                    if (i + 1 < line.Length && line[i + 1] == Quote)
                    {
                        current.Append(Quote); // "" внутри кавычек -> "
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == Quote)
            {
                inQuotes = true;
            }
            else if (c == Delimiter)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
