namespace Realm.Maps;

using Realm.MapAPI;

public sealed class TowerAI
{
    private static readonly TowerDefenseConfig DefaultConfig = new(12f, 20f, 1.25f, "lightning");
    private readonly IUnit _tower;
    private float _cooldown;

    public TowerAI(IUnit tower)
    {
        _tower = tower;
        if (_tower.Range <= 0) _tower.Range = DefaultConfig.Range;
        if (_tower.Damage <= 0) _tower.Damage = DefaultConfig.Damage;
    }

    public bool IsAlive => !_tower.IsDead;

    public void Update(IGameAPI api, float delta) =>
        MapScriptHelpers.RunTowerDefenseTick(api, _tower, DefaultConfig, ref _cooldown, delta);
}
