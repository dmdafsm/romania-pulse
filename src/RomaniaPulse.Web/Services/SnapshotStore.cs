using System.Text.Json;

using RomaniaPulse.Web.Domain;

namespace RomaniaPulse.Web.Services;

/// <summary>Last good copy of each indicator on disk, so a restart or an upstream outage does not blank the portal.</summary>
public interface ISnapshotStore
{
    Task<IndicatorData?> ReadAsync(string id, CancellationToken cancellationToken);

    Task WriteAsync(string id, IndicatorData data, CancellationToken cancellationToken);
}

public sealed class FileSnapshotStore(IWebHostEnvironment environment, ILogger<FileSnapshotStore> logger) : ISnapshotStore
{
    private readonly string _directory = Path.Combine(environment.ContentRootPath, "App_Data", "snapshots");

    public async Task<IndicatorData?> ReadAsync(string id, CancellationToken cancellationToken)
    {
        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<IndicatorData>(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            logger.LogWarning(ex, "Ignoring unreadable snapshot for {Indicator}", id);
            return null;
        }
    }

    public async Task WriteAsync(string id, IndicatorData data, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        var path = PathFor(id);
        var temp = path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, data, cancellationToken: cancellationToken);
        }

        File.Move(temp, path, overwrite: true);
    }

    // Ids come from the catalog, never from user input, but keep the path safe regardless.
    private string PathFor(string id) => Path.Combine(_directory, Path.GetFileName(id) + ".json");
}
