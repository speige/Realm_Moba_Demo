namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class TowerAI
{
    private const float DefaultRange = 12f;
    private const float DefaultDamage = 20f;
    private const float AttackCooldown = 1.25f;

    private readonly IUnit _tower;
    private float _cooldown;

    public TowerAI(IUnit tower)
    {
        _tower = tower;
        _tower.Range = _tower.Range > 0 ? _tower.Range : DefaultRange;
        _tower.Damage = _tower.Damage > 0 ? _tower.Damage : DefaultDamage;
    }

    public bool IsAlive => !_tower.IsDead;

    public void Update(IGameAPI api, float delta)
    {
        if (!IsAlive)
            return;

        _cooldown = MathF.Max(0, _cooldown - delta);
        var target = api.GetUnitsInRadius(_tower.Position, _tower.Range)
            .Where(unit => !unit.IsDead && unit.Player != _tower.Player)
            .OrderBy(unit => Vector3.DistanceSquared(unit.Position, _tower.Position))
            .FirstOrDefault();

        if (target == null || _cooldown > 0)
            return;

        _tower.Attack(target);
        api.SpawnVisualEffect("lightning", target.Position, 0.35f);
        _cooldown = AttackCooldown;
    }
}