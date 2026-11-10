namespace RomaniaPulse.Tests;

internal static class Fixture
{
    public static Stream Open(string name) => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static Stream FromText(string text) => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

    /// <summary>Walks up from the test binaries to the repository root (where the .slnx lives).</summary>
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("*.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
