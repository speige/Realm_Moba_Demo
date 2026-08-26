namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public class MapScript : IWasmModule
{
    private readonly List<TowerAI> _towers = [];
    private readonly MinionSpawner _minionSpawner = new();
    private IUnit? _hero;
    private HeroProgression? _progression;

    public void Initialize(IGameAPI api)
    {
        api.SetPlayerTeam(0, 0);
        api.SetPlayerTeam(1, 1);
        api.SetPlayersAllied(0, 1, false);
        api.SetPlayerMaxPopulation(0, 200);
        api.SetPlayerMaxPopulation(1, 200);

        foreach (var unit in api.GetAllUnits().Where(unit => unit.UnitId == "ice_castle_1" && unit.IsBuilding))
        {
            var owner = GetTowerOwner(unit, api);
            if (unit.Player != owner)
                api.SetUnitOwner(unit, owner);

            _towers.Add(new TowerAI(unit));
        }

        SpawnPlayerHero(api);
    }

    private void SpawnPlayerHero(IGameAPI api)
    {
        if (!CoordinateResolver.TryGetCenter(api, "Spawn_Team1", out var spawn) &&
            !CoordinateResolver.TryGetCenter(api, "Base_Team1", out spawn))
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
        foreach (var tower in _towers)
            tower.Update(api, delta);

        _minionSpawner.Update(api, delta);
        _progression?.Update(api, delta);
    }

    private static int GetTowerOwner(IUnit unit, IGameAPI api)
    {
        if (unit.Player is 0 or 1)
            return unit.Player;

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
