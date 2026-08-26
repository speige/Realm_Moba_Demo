namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public class MapScript : IWasmModule
{
    private readonly List<TowerAI> _towers = [];
    private readonly MinionSpawner _minionSpawner = new();
    private IUnit? _hero;

    public void Initialize(IGameAPI api)
    {
        api.SetPlayerTeam(0, 0);
        api.SetPlayerTeam(1, 1);
        api.SetPlayersAllied(0, 1, false);
        api.SetPlayerMaxPopulation(0, 200);
        api.SetPlayerMaxPopulation(1, 200);

        foreach (var unit in api.GetAllUnits().Where(unit => unit.UnitId == "ice_castle_1" && unit.IsBuilding))
        {
            var owner = GetTowerOwner(unit.Position, api);
            if (unit.Player != owner)
                api.SetUnitOwner(unit, owner);

            _towers.Add(new TowerAI(unit));
        }

        if (api.HasCoordinate("Base_Team1"))
        {
            var heroSpawn = api.GetCoordinate("Base_Team1").Center + new Vector3(4f, 0f, -4f);
            _hero = api.SpawnUnit("fantasy_warrior_unit_1", heroSpawn, false, true);
            if (_hero != null)
                api.SetUnitOwner(_hero, 0);

            if (_hero != null)
                api.SelectUnit(_hero);
        }
    }

    public void Update(IGameAPI api, float delta)
    {
        foreach (var tower in _towers)
            tower.Update(api, delta);

        _minionSpawner.Update(api, delta);
    }

    private static int GetTowerOwner(Vector3 position, IGameAPI api)
    {
        if (api.IsPositionInCoordinate(position, "Base_Team1") ||
            api.IsPositionInCoordinate(position, "Mid_Team1_Tower"))
            return 0;

        if (api.IsPositionInCoordinate(position, "Base_Team2") ||
            api.IsPositionInCoordinate(position, "Mid_Team2_Tower"))
            return 1;

        return position.Z > 0 || position.Z == 0 && position.X < 0 ? 0 : 1;
    }
}
