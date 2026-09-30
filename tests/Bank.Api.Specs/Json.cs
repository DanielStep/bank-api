using System.Text.Json;

namespace Bank.Api.Specs;

static class Json
{
    public static List<int> LinesOf(JsonElement list)
    {
        var lines = new List<int>();
        foreach (var item in list.EnumerateArray())
            lines.Add(item.GetProperty("line").GetInt32());

        return lines;
    }
}
