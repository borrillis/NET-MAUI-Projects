namespace MeTracker.Repositories;

public interface ILocationRepository
{
    Task SaveAsync(Models.Location location);
}
