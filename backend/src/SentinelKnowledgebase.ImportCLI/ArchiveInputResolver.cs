using System.IO.Compression;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class ArchiveInputResolver : IArchiveInputResolver
{
    public Task<IArchiveDataSource> ResolveAsync(string inputPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(inputPath);
        if (File.Exists(fullPath))
        {
            if (!string.Equals(Path.GetExtension(fullPath), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Input file '{fullPath}' is not a .zip archive.");
            }

            return Task.FromResult<IArchiveDataSource>(ZipArchiveDataSource.Open(fullPath));
        }

        if (!Directory.Exists(fullPath))
        {
            throw new InvalidOperationException($"Input path '{fullPath}' does not exist.");
        }

        if (DirectoryContainsManifest(fullPath))
        {
            return Task.FromResult<IArchiveDataSource>(new DirectoryArchiveDataSource(fullPath));
        }

        var candidateDirectories = Directory.GetDirectories(fullPath)
            .Where(DirectoryContainsManifest)
            .ToList();

        return candidateDirectories.Count switch
        {
            1 => Task.FromResult<IArchiveDataSource>(new DirectoryArchiveDataSource(candidateDirectories[0])),
            > 1 => throw new InvalidOperationException(
                $"Input directory '{fullPath}' contains multiple Twitter archive roots. Pass the archive directory itself or a specific .zip file."),
            _ => throw new InvalidOperationException(
                $"Could not find data/manifest.js under '{fullPath}'. Pass the archive root directory or a .zip file.")
        };
    }

    private static bool DirectoryContainsManifest(string directoryPath)
    {
        return File.Exists(Path.Combine(directoryPath, "data", "manifest.js"));
    }
}

internal sealed class DirectoryArchiveDataSource : IArchiveDataSource
{
    private readonly string _rootPath;

    public DirectoryArchiveDataSource(string rootPath)
    {
        _rootPath = rootPath;
    }

    public string DisplayName => _rootPath;

    public async Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Archive file '{relativePath}' was not found in '{_rootPath}'.");
        }

        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

internal sealed class ZipArchiveDataSource : IArchiveDataSource
{
    private const string ManifestRelativePath = "data/manifest.js";
    private readonly ZipArchive _archive;
    private readonly string _entryPrefix;
    private readonly string _sourcePath;

    private ZipArchiveDataSource(ZipArchive archive, string entryPrefix, string sourcePath)
    {
        _archive = archive;
        _entryPrefix = entryPrefix;
        _sourcePath = sourcePath;
    }

    public string DisplayName => _sourcePath;

    public static ZipArchiveDataSource Open(string zipPath)
    {
        var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.Entries.FirstOrDefault(entry =>
            NormalizeEntryName(entry.FullName).EndsWith(ManifestRelativePath, StringComparison.OrdinalIgnoreCase));

        if (manifestEntry == null)
        {
            archive.Dispose();
            throw new InvalidOperationException($"Zip archive '{zipPath}' does not contain {ManifestRelativePath}.");
        }

        var normalizedName = NormalizeEntryName(manifestEntry.FullName);
        var prefix = normalizedName[..^ManifestRelativePath.Length];
        return new ZipArchiveDataSource(archive, prefix, zipPath);
    }

    public async Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targetName = _entryPrefix + NormalizeEntryName(relativePath);
        var entry = _archive.GetEntry(targetName) ?? _archive.Entries.FirstOrDefault(candidate =>
            string.Equals(NormalizeEntryName(candidate.FullName), targetName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new InvalidOperationException($"Archive file '{relativePath}' was not found in '{_sourcePath}'.");
        }

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _archive.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string NormalizeEntryName(string value)
    {
        return value.Replace('\\', '/');
    }
}
