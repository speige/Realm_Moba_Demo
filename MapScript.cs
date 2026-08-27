namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

internal static class MapScriptUnits
{
    public static IEnumerable<IUnit> All(IGameAPI api) => api.GetAllUnits() ?? [];
}

public class MapScript : IWasmModule
{
    private readonly List<TowerAI> _towers = [];
    private readonly MinionSpawner _minionSpawner = new();
    private IUnit? _hero;
    private HeroProgression? _progression;
    private bool _gameEnded;

    public void Initialize(IGameAPI api)
    {
        api.SetPlayerTeam(0, 0);
        api.SetPlayerTeam(1, 1);
        api.SetPlayersAllied(0, 1, false);
        api.SetPlayerMaxPopulation(0, 200);
        api.SetPlayerMaxPopulation(1, 200);

        SpawnPlayerHero(api);
        _minionSpawner.SpawnInitialWave(api);
        ConfigureTowers(api);
        api.SendMessageToPlayer(0, "First minion wave deployed.");
        // Shop: call TryRegisterShop(api) only when running Realm core with feat/lol-like-shop-items.
    }

    /// <summary>
    /// Shop APIs require a game build with feat/lol-like-shop-items (or newer main).
    /// Runs after hero + waves so a missing host shop does not break the demo loop.
    /// </summary>
    private static void TryRegisterShop(IGameAPI api)
    {
        api.RegisterItem("demo_blade", "Demo Blade", "+12 damage", 150f, 12f, 0f, 0f, 0f);
        api.RegisterItem("demo_armor", "Demo Armor", "+120 max health, +3 armor", 175f, 0f, 120f, 3f, 0f);
        api.RegisterItem("demo_boots", "Demo Boots", "+0.75 movement speed", 125f, 0f, 0f, 0f, 0.75f);
        api.SetShopBuyZone(0, "Base_Team1");
        api.SetShopBuyZone(1, "Base_Team2");
        api.SendMessageToPlayer(0, "Shop ready: buy at your base (Shop button).");
    }

    private void SpawnPlayerHero(IGameAPI api)
    {
        if (!api.TryGetCoordinateCenter("Spawn_Team1", out Vector3 spawn) &&
            !api.TryGetCoordinateCenter("Base_Team1", out spawn))
        {
            api.BroadcastMessage("Hero spawn failed: Spawn_Team1/Base_Team1 missing");
            return;
        }

        _hero = api.SpawnUnitForPlayer("fantasy_warrior_unit_1", spawn, 0)
                ?? api.SpawnUnit("fantasy_warrior_unit_1", spawn, false, true);

        if (_hero == null)
        {
            api.BroadcastMessage("Hero spawn returned null for fantasy_warrior_unit_1");
            return;
        }

        api.SetUnitOwner(_hero, 0);
        api.SelectUnit(_hero);
        api.PanCameraTo(_hero.Position, 0.35f);
        _progression = new HeroProgression(_hero);
        _progression.Attach(api);
        api.SendMessageToPlayer(0, "Kevin ready. Waves every 30s.");
    }

    public void Update(IGameAPI api, float delta)
    {
        CheckBaseWinLose(api);
        if (_gameEnded)
            return;

        foreach (var tower in _towers)
            tower.Update(api, delta);

        _minionSpawner.Update(api, delta);
        _progression?.Update(api, delta);
    }

    private void CheckBaseWinLose(IGameAPI api)
    {
        if (_gameEnded)
            return;

        IUnit? team1Base = null;
        IUnit? team2Base = null;

        foreach (var unit in MapScriptUnits.All(api))
        {
            if (unit.UnitId != "ice_castle_1" || !unit.IsBuilding || unit.IsDead)
                continue;

            if (api.IsPositionInCoordinate(unit.Position, "Base_Team1"))
                team1Base = unit;
            else if (api.IsPositionInCoordinate(unit.Position, "Base_Team2"))
                team2Base = unit;
        }

        if (team1Base != null && team1Base.IsDead)
        {
            _gameEnded = true;
            api.TriggerDefeat();
            return;
        }

        if (team2Base != null && team2Base.IsDead)
        {
            _gameEnded = true;
            api.TriggerVictory();
        }
    }

    private void ConfigureTowers(IGameAPI api)
    {
        foreach (var unit in MapScriptUnits.All(api))
        {
            if (unit.UnitId != "ice_castle_1" || !unit.IsBuilding)
                continue;

            var owner = GetTowerOwner(unit, api);
            var expectedEnemy = owner != 0;
            if (unit.IsEnemy != expectedEnemy)
                api.SetUnitOwner(unit, owner);

            _towers.Add(new TowerAI(unit));
        }
    }

    private static int GetTowerOwner(IUnit unit, IGameAPI api)
    {
        var position = unit.Position;
        if (api.IsPositionInCoordinate(position, "Base_Team1") ||
            api.IsPositionInCoordinate(position, "Mid_Team1_Tower") ||
            api.IsPositionInCoordinate(position, "Side_Team1_Tower") ||
            api.IsPositionInCoordinate(position, "Bot_Team1_Tower"))
            return 0;

        if (api.IsPositionInCoordinate(position, "Base_Team2") ||
            api.IsPositionInCoordinate(position, "Mid_Team2_Tower") ||
            api.IsPositionInCoordinate(position, "Side_Team2_Tower") ||
            api.IsPositionInCoordinate(position, "Top_Team2_Tower"))
            return 1;

        return position.Z > 0 || position.Z == 0 && position.X < 0 ? 0 : 1;
    }
}
