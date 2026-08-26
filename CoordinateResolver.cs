namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public static class CoordinateResolver
{
    public static bool TryGetCenter(IGameAPI api, string name, out Vector3 center)
    {
        if (api.HasCoordinate(name))
        {
            center = api.GetCoordinate(name).Center;
            return true;
        }

        center = default;
        return false;
    }

    public static Vector3 RequireCenterOrFallback(IGameAPI api, string name, Vector3 fallback)
    {
        return TryGetCenter(api, name, out var center) ? center : fallback;
    }

    public static bool TryBuildLaneWaypoints(
        IGameAPI api,
        Vector3 start,
        string cornerName,
        Vector3 destination,
        out IReadOnlyList<Vector3> waypoints)
    {
        if (!TryGetCenter(api, cornerName, out var corner))
        {
            waypoints = Array.Empty<Vector3>();
            return false;
        }

        waypoints = new[] { start, corner, destination };
        return true;
    }
}
