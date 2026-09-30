namespace Bank.Data.Specs;

class TempFile : IDisposable
{
    public string Path { get; }

    public TempFile(string text)
    {
        Path = System.IO.Path.GetTempFileName();
        File.WriteAllText(Path, text);
    }

    public void Dispose()
    {
        File.Delete(Path);
    }
}
