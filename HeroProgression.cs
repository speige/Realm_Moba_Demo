namespace Realm.Maps;

using Realm.MapAPI;

public sealed class HeroProgression
{
    private const float XpPerLevel = 100f;
    private const float KillGold = 25f;
    private const float KillXp = 40f;

    private readonly IUnit _hero;
    private readonly HashSet<int> _rewardedDeaths = [];
    private IGameAPI? _api;
    private float _trackedXp;
    private bool _attached;

    public HeroProgression(IUnit hero)
    {
        _hero = hero;
    }

    public void Attach(IGameAPI api)
    {
        if (_attached)
            return;

        _attached = true;
        _api = api;
        api.SetPlayerGold(0, MathF.Max(api.GetPlayerGold(0), 300f));
        api.OnUnitDied += HandleUnitDied;
    }

    public void Update(IGameAPI api, float delta)
    {
        // Reserved for polling-based UI results after core handoff lands.
    }

    private void HandleUnitDied(IUnit dead, IUnit? killer)
    {
        if (_api == null || !_rewardedDeaths.Add(dead.UniqueId))
            return;
        if (dead.Player == _hero.Player || _hero.IsDead)
            return;

        // Killer attribution is currently informational; the demo rewards any enemy death.
        _ = killer?.UniqueId;
        _api.SetPlayerGold(0, _api.GetPlayerGold(0) + KillGold);
        _trackedXp += KillXp;
        _hero.Experience = _trackedXp;
        var level = 1 + (int)(_trackedXp / XpPerLevel);
        _api.SetUnitLevel(_hero, level);
    }
}
