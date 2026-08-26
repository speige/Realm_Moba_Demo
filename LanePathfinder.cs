namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class LanePathfinder
{
    private readonly IUnit _unit;
    private readonly IReadOnlyList<Vector3> _waypoints;
    private readonly Vector3 _finalDestination;
    private int _waypointIndex = 1;
    private bool _hasMovementOrder;
    private bool _wasFighting;
    private float _attackCooldown;

    public const float ScanRadius = 8f;
    private const float DefaultAttackCooldown = 1.2f;

    public LanePathfinder(IUnit unit, IReadOnlyList<Vector3> waypoints)
    {
        _unit = unit;
        _waypoints = waypoints;
        _finalDestination = waypoints.Count > 0 ? waypoints[^1] : unit.Position;
    }

    public bool IsAlive => !_unit.IsDead;

    public void Update(IGameAPI api, float delta)
    {
        if (!IsAlive || _waypointIndex >= _waypoints.Count)
            return;

        _attackCooldown = MathF.Max(0, _attackCooldown - delta);

        var attackRange = _unit.Range > 0 ? _unit.Range : 1.5f;
        var target = api.GetUnitsInRadius(_unit.Position, attackRange + 1.25f)
            .Where(candidate => !candidate.IsDead && candidate.Player != _unit.Player)
            .OrderBy(candidate => HorizontalDistanceSquared(candidate.Position, _unit.Position))
            .FirstOrDefault();

        if (target != null)
        {
            _wasFighting = true;
            _hasMovementOrder = false;

            var distSq = HorizontalDistanceSquared(_unit.Position, target.Position);
            if (distSq > attackRange * attackRange)
            {
                IssueLanePush(api);
                return;
            }

            if (_attackCooldown <= 0)
            {
                _unit.Attack(target);
                _attackCooldown = DefaultAttackCooldown;
            }

            return;
        }

        AdvanceWaypointIfReached();

        if (!_hasMovementOrder || _wasFighting)
        {
            IssueLanePush(api);
            _wasFighting = false;
        }
    }

    private void AdvanceWaypointIfReached()
    {
        var waypoint = _waypoints[_waypointIndex];
        if (HorizontalDistanceSquared(_unit.Position, waypoint) > 4f)
            return;

        _waypointIndex++;
        _hasMovementOrder = false;
    }

    private void IssueLanePush(IGameAPI api)
    {
        if (_waypointIndex >= _waypoints.Count)
        {
            api.IssueAttackMoveOrder(_unit, _finalDestination);
            _hasMovementOrder = true;
            return;
        }

        api.IssueAttackMoveOrder(_unit, _waypoints[_waypointIndex]);
        _hasMovementOrder = true;
    }

    private static float HorizontalDistanceSquared(Vector3 from, Vector3 to)
    {
        var deltaX = from.X - to.X;
        var deltaZ = from.Z - to.Z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }
}
