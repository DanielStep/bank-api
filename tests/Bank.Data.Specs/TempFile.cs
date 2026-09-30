namespace Bank.Data.Specs;

// A balances file in the temp directory, deleted when the spec is done with it.
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
