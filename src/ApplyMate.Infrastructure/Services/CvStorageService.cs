using System.Text;
using ApplyMate.Core.Abstractions;
using ApplyMate.Infrastructure.Persistence.Pathing;

namespace ApplyMate.Infrastructure.Services;

public sealed class CvStorageService : ICvStorageService
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;

    public CvStorageService(ILocalStoragePathProvider localStoragePathProvider)
    {
        _localStoragePathProvider = localStoragePathProvider;
    }

    public async Task<(string storedPath, string originalFileName)> CopyCvIntoLocalAsync(
        Guid appId,
        string sourceFilePath,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source CV file does not exist.", sourceFilePath);
        }

        var rootPath = _localStoragePathProvider.GetApplyMateLocalFolderPath();
        var cvFolder = Path.Combine(rootPath, "cv", appId.ToString("N"));
        Directory.CreateDirectory(cvFolder);

        var originalFileName = Path.GetFileName(sourceFilePath);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = "cv.pdf";
        }

        var sanitizedFileName = SanitizeFileName(originalFileName);
        var destinationPath = ResolveUniquePath(cvFolder, sanitizedFileName);

        const int bufferSize = 81920;
        await using (var input = new FileStream(
                         sourceFilePath,
                         FileMode.Open,
                         FileAccess.Read,
                         FileShare.Read,
                         bufferSize,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        await using (var output = new FileStream(
                         destinationPath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize,
                         FileOptions.Asynchronous))
        {
            await input.CopyToAsync(output, bufferSize, ct);
        }

        return (destinationPath, originalFileName);
    }

    public Task RemoveCvAsync(string storedPath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedPath);
        ct.ThrowIfCancellationRequested();

        if (File.Exists(storedPath))
        {
            File.Delete(storedPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storedPath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedPath);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(storedPath));
    }

    private static string ResolveUniquePath(string targetDirectory, string fileName)
    {
        var path = Path.Combine(targetDirectory, fileName);
        if (!File.Exists(path))
        {
            return path;
        }

        var extension = Path.GetExtension(fileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var suffix = 1;

        while (true)
        {
            var candidate = Path.Combine(
                targetDirectory,
                $"{fileNameWithoutExtension}_{suffix}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);

        foreach (var c in fileName)
        {
            var isInvalid = false;
            foreach (var invalidCharacter in invalidCharacters)
            {
                if (c != invalidCharacter)
                {
                    continue;
                }

                isInvalid = true;
                break;
            }

            builder.Append(isInvalid ? '_' : c);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "cv.pdf" : sanitized;
    }
}
