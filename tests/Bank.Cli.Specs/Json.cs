using System.Text.Json;

namespace Bank.Cli.Specs;

static class Json
{
    public static List<int> LinesOf(JsonElement list)
    {
        var lines = new List<int>();
        foreach (var item in list.EnumerateArray())
            lines.Add(item.GetProperty("line").GetInt32());

        return lines;
    }

    public static string Compact(JsonElement element) => JsonSerializer.Serialize(element);
}
