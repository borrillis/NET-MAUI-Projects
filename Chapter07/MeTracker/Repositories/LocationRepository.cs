using SQLite;

namespace MeTracker.Repositories;

public class LocationRepository : ILocationRepository
{
    public async Task SaveAsync(Models.Location location)
    {
        await CreateConnectionAsync();
        await connection.InsertAsync(location);
    }

    private SQLiteAsyncConnection connection;
    private async Task CreateConnectionAsync()
    {
        if (connection != null)
        {
            return;
        }

        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Locations.db");

        connection = new SQLiteAsyncConnection(databasePath);
        await connection.CreateTableAsync<Location>();
    }
}
