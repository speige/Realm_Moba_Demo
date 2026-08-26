namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class LanePathfinder
{
    private readonly IUnit _unit;
    private readonly IReadOnlyList<Vector3> _waypoints;
    private int _waypointIndex = 1;
    private bool _hasMovementOrder;
    private float _attackCooldown;

    public const float ScanRadius = 8f;
    private const float DefaultAttackCooldown = 1.2f;

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

        _attackCooldown = MathF.Max(0, _attackCooldown - delta);

        var attackRange = _unit.Range > 0 ? _unit.Range : 1.5f;
        var target = api.GetUnitsInRadius(_unit.Position, ScanRadius)
            .Where(candidate => !candidate.IsDead && candidate.Player != _unit.Player)
            .OrderBy(candidate => HorizontalDistanceSquared(candidate.Position, _unit.Position))
            .FirstOrDefault();

        if (target != null)
        {
            var distSq = HorizontalDistanceSquared(_unit.Position, target.Position);
            if (distSq > attackRange * attackRange)
            {
                // Walk into melee/ranged attack distance instead of stopping out of range.
                _unit.AttackMove(target.Position);
                _hasMovementOrder = false;
                return;
            }

            if (_attackCooldown <= 0)
            {
                var damage = _unit.Damage > 0 ? _unit.Damage : 18f;
                _unit.Attack(target);
                // Prefer Health write over IGameAPI.DealDamage — not all MapAPI builds expose DealDamage (CS1061).
                target.Health = MathF.Max(0f, target.Health - damage);
                _attackCooldown = DefaultAttackCooldown;
            }

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
