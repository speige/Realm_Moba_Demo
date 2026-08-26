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
        // Only peel for fights near the current lane push — don't abandon the lane to chase.
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
                // Keep lane intent: attack-move toward the next waypoint while closing.
                IssueLanePush();
                return;
            }

            if (_attackCooldown <= 0)
            {
                var damage = _unit.Damage > 0 ? _unit.Damage : 18f;
                _unit.Attack(target);
                target.Health = MathF.Max(0f, target.Health - damage);
                _attackCooldown = DefaultAttackCooldown;
            }

            return;
        }

        AdvanceWaypointIfReached();

        // After a fight clears, always re-issue the lane push toward the main objective.
        if (!_hasMovementOrder || _wasFighting)
        {
            IssueLanePush();
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

    private void IssueLanePush()
    {
        if (_waypointIndex >= _waypoints.Count)
        {
            _unit.AttackMove(_finalDestination);
            _hasMovementOrder = true;
            return;
        }

        _unit.AttackMove(_waypoints[_waypointIndex]);
        _hasMovementOrder = true;
    }

    private static float HorizontalDistanceSquared(Vector3 from, Vector3 to)
    {
        var deltaX = from.X - to.X;
        var deltaZ = from.Z - to.Z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }
}
