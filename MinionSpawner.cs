namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class MinionSpawner
{
    private const float WaveInterval = 30f;
    private const float SpawnOffset = 1.5f;
    private readonly List<LanePathfinder> _minions = [];
    private float _elapsed = WaveInterval;

    public void Update(IGameAPI api, float delta)
    {
        _elapsed += delta;
        if (_elapsed >= WaveInterval)
        {
            _elapsed -= WaveInterval;
            SpawnWave(api);
        }

        foreach (var minion in _minions.ToArray())
            minion.Update(api, delta);

        _minions.RemoveAll(minion => !minion.IsAlive);
    }

    private void SpawnWave(IGameAPI api)
    {
        var baseTeam1 = GetCenter(api, "Base_Team1");
        var baseTeam2 = GetCenter(api, "Base_Team2");

        SpawnTeam(api, 0, baseTeam1, baseTeam2);
        SpawnTeam(api, 1, baseTeam2, baseTeam1);
    }

    private void SpawnTeam(IGameAPI api, int player, Vector3 start, Vector3 destination)
    {
        var spawnStart = start + (player == 0 ? new Vector3(5f, 0f, -5f) : new Vector3(-5f, 0f, 5f));
        var top = GetCenter(api, "Top_Corner");
        var bot = GetCenter(api, "Bot_Corner");
        var middle = GetCenter(api, "Middle");
        var lanes = new IReadOnlyList<Vector3>[]
        {
            [start, top, destination],
            [start, middle, destination],
            [start, bot, destination]
        };

        for (var lane = 0; lane < lanes.Length; lane++)
        {
            for (var member = 0; member < 3; member++)
            {
                var offset = new Vector3((member - 1) * SpawnOffset, 0, (lane - 1) * SpawnOffset);
                var unit = api.SpawnUnit(
                    player == 0 ? "fantasy_warrior_unit_1" : "orc_warrior_7",
                    spawnStart + offset,
                    player == 1,
                    true);
                if (unit != null)
                {
                    api.SetUnitOwner(unit, player);
                    _minions.Add(new LanePathfinder(unit, lanes[lane]));
                }
            }
        }
    }

    private static Vector3 GetCenter(IGameAPI api, string name)
    {
        return api.HasCoordinate(name) ? api.GetCoordinate(name).Center : Vector3.Zero;
    }
}