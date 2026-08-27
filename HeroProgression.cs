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
    private float _trackedGold;
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
        _trackedGold = MathF.Max(api.GetPlayerGold(0), 300f);
        api.SetPlayerGold(0, _trackedGold);
        api.SetLeaderboardVisible("MOBA", true);
        api.ClearLeaderboard();
        api.AddLeaderboardRow("Gold", $"{(int)_trackedGold}", null);
        api.AddLeaderboardRow("Level", "1", null);
        api.SendMessageToPlayer(0, $"Starting gold: {(int)_trackedGold}");

        // Flattened RegisterItem overload (WASM-safe); order: id, displayName, description, costGold, damageBonus, maxHealthBonus, armorBonus, speedBonus
        api.RegisterItem("demo_blade", "Demo Blade", "+12 damage", 150f, 12f, 0f, 0f, 0f);
        api.RegisterItem("demo_armor", "Demo Armor", "+120 max health, +3 armor", 175f, 0f, 120f, 3f, 0f);
        api.RegisterItem("demo_boots", "Demo Boots", "+0.75 movement speed", 125f, 0f, 0f, 0f, 0.75f);
        api.SetShopBuyZone(0, "Base_Team1");
        api.SetShopBuyZone(1, "Base_Team2");
        api.SendMessageToPlayer(0, "Shop ready: buy at your base (Shop button).");

        api.OnUnitDied += HandleUnitDied;
    }

    public void Update(IGameAPI api, float delta)
    {
        _trackedGold = api.GetPlayerGold(0);
        api.SetLeaderboardValue("Gold", $"{(int)_trackedGold}");
    }

    private void HandleUnitDied(IUnit dead, IUnit? killer)
    {
        if (_api == null || dead.Player == _hero.Player || _hero.IsDead)
            return;
        if (!_rewardedDeaths.Add(dead.UniqueId))
            return;

        _ = killer?.UniqueId;
        _trackedGold += KillGold;
        _api.SetPlayerGold(0, _trackedGold);
        _trackedXp += KillXp;
        var level = 1 + (int)(_trackedXp / XpPerLevel);
        _api.SetUnitLevel(_hero, level);
        _api.SetLeaderboardValue("Gold", $"{(int)_trackedGold}");
        _api.SetLeaderboardValue("Level", $"{level}");
        _api.SendMessageToPlayer(0, $"+{(int)KillGold} gold (total {(int)_trackedGold}) | Level {level}");
    }
}
