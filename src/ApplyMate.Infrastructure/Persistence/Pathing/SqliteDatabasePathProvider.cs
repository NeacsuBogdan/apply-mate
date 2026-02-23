namespace ApplyMate.Infrastructure.Persistence.Pathing;

public sealed class SqliteDatabasePathProvider : IDatabasePathProvider
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;

    public SqliteDatabasePathProvider(ILocalStoragePathProvider localStoragePathProvider)
    {
        _localStoragePathProvider = localStoragePathProvider;
    }

    public string GetDatabasePath()
    {
        var rootFolder = _localStoragePathProvider.GetApplyMateLocalFolderPath();
        return Path.Combine(rootFolder, "applymate.db");
    }
}
