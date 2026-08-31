namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

/// <summary>
/// Named coordinate centers using IGameAPI methods that exist on current WASM MapAPI
/// (HasCoordinate / GetCoordinateMin / GetCoordinateMax). Does not require CoordinateResolver.TryCenter.
/// </summary>
internal static class NamedPoints
{
    public static Vector3? Center(IGameAPI api, string name)
    {
        if (!api.HasCoordinate(name))
            return null;
        return (api.GetCoordinateMin(name) + api.GetCoordinateMax(name)) / 2f;
    }
}
