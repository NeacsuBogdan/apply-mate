using System.Reflection;

namespace ApplyMate.Infrastructure.Persistence.Pathing;

public sealed class WindowsLocalStoragePathProvider : ILocalStoragePathProvider
{
    private readonly string _productFolderName;

    public WindowsLocalStoragePathProvider(string productFolderName = "ApplyMate")
    {
        _productFolderName = productFolderName;
    }

    public string GetApplyMateLocalFolderPath()
    {
        var basePath = TryGetWindowsStorageLocalFolderPath()
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var targetPath = Path.Combine(basePath, _productFolderName);
        Directory.CreateDirectory(targetPath);
        return targetPath;
    }

    private static string? TryGetWindowsStorageLocalFolderPath()
    {
        try
        {
            var appDataType = Type.GetType(
                "Windows.Storage.ApplicationData, Windows, ContentType=WindowsRuntime");

            if (appDataType is null)
            {
                return null;
            }

            var current = appDataType.GetProperty(
                    "Current",
                    BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null);

            if (current is null)
            {
                return null;
            }

            var localFolder = appDataType.GetProperty("LocalFolder")?.GetValue(current);
            if (localFolder is null)
            {
                return null;
            }

            return localFolder.GetType().GetProperty("Path")?.GetValue(localFolder) as string;
        }
        catch
        {
            return null;
        }
    }
}
