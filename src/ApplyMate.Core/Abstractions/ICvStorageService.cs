namespace ApplyMate.Core.Abstractions;

public interface ICvStorageService
{
    Task<(string storedPath, string originalFileName)> CopyCvIntoLocalAsync(
        Guid appId,
        string sourceFilePath,
        CancellationToken ct);

    Task RemoveCvAsync(string storedPath, CancellationToken ct);

    Task<bool> ExistsAsync(string storedPath, CancellationToken ct);
}
