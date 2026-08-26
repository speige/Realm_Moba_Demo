namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class LanePathfinder
{
    private readonly IUnit _unit;
    private readonly IReadOnlyList<Vector3> _waypoints;
    private int _waypointIndex = 1;
    private bool _hasMovementOrder;

    public const float ScanRadius = 8f;

    public LanePathfinder(IUnit unit, IReadOnlyList<Vector3> waypoints)
    {
        _unit = unit;
        _waypoints = waypoints;
    }

    public bool IsAlive => !_unit.IsDead;

    public void Update(IGameAPI api, float delta)
    {
        if (!IsAlive || _waypointIndex >= _waypoints.Count)
            return;

        var target = api.GetUnitsInRadius(_unit.Position, ScanRadius)
            .Where(candidate => !candidate.IsDead && candidate.Player != _unit.Player)
            .OrderBy(candidate => Vector3.DistanceSquared(candidate.Position, _unit.Position))
            .FirstOrDefault();

        if (target != null)
        {
            _unit.Stop();
            _unit.Attack(target);
            _hasMovementOrder = false;
            return;
        }

        var waypoint = _waypoints[_waypointIndex];
        if (HorizontalDistanceSquared(_unit.Position, waypoint) <= 4f)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Count)
                return;

            waypoint = _waypoints[_waypointIndex];
            _hasMovementOrder = false;
        }

        if (!_hasMovementOrder)
        {
            _unit.AttackMove(waypoint);
            _hasMovementOrder = true;
        }
    }

    private static float HorizontalDistanceSquared(Vector3 from, Vector3 to)
    {
        var deltaX = from.X - to.X;
        var deltaZ = from.Z - to.Z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }
}