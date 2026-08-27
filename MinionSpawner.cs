namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class MinionSpawner
{
    private const float WaveInterval = 30f;
    private const float SpawnOffset = 1.5f;
    private static readonly string[] LaneCorners = ["Top_Corner", "Middle", "Bot_Corner"];
    private readonly List<LanePathfinder> _minions = [];
    private float _elapsed;
    private bool _reportedSpawnFailure;

    public void SpawnInitialWave(IGameAPI api)
    {
        _elapsed = 0f;
        SpawnWave(api);
    }

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
        if (!api.TryGetCoordinateCenter("Base_Team1", out var baseTeam1) ||
            !api.TryGetCoordinateCenter("Base_Team2", out var baseTeam2))
        {
            api.BroadcastMessage("MinionSpawner: Base_Team1/Base_Team2 missing; skipping wave");
            return;
        }

        var spawned = 0;
        spawned += SpawnTeam(api, 0, baseTeam1, baseTeam2);
        spawned += SpawnTeam(api, 1, baseTeam2, baseTeam1);
        api.BroadcastMessage($"Minion wave deployed: {spawned} units across 3 lanes per team.");
    }

    private int SpawnTeam(IGameAPI api, int player, Vector3 start, Vector3 destination)
    {
        var spawnPad = player == 0 ? "Spawn_Team1" : "Spawn_Team2";
        var spawnStart = api.TryGetCoordinateCenter(spawnPad, out var padCenter)
            ? padCenter
            : start + (player == 0 ? new Vector3(5f, 0f, -5f) : new Vector3(-5f, 0f, 5f));

        var spawned = 0;
        foreach (var cornerName in LaneCorners)
        {
            if (!TryBuildLanePath(api, spawnStart, cornerName, destination, out var waypoints))
            {
                api.BroadcastMessage($"MinionSpawner: skipping lane {cornerName}");
                continue;
            }

            for (var member = 0; member < 3; member++)
            {
                var offset = new Vector3((member - 1) * SpawnOffset, 0, 0);
                var unit = api.SpawnUnit(
                    player == 0 ? "moba_minion_team1" : "moba_minion_team2",
                    spawnStart + offset,
                    player == 1,
                    true);
                if (unit == null)
                {
                    if (!_reportedSpawnFailure)
                    {
                        api.BroadcastMessage("MinionSpawner: SpawnUnit returned null; some minions were skipped");
                        _reportedSpawnFailure = true;
                    }
                    continue;
                }

                api.SetUnitOwner(unit, player);
                _minions.Add(new LanePathfinder(unit, waypoints));
                spawned++;
            }
        }

        return spawned;
    }

    private static bool TryBuildLanePath(
        IGameAPI api,
        Vector3 spawnStart,
        string cornerName,
        Vector3 destination,
        out Vector3[] waypoints)
    {
        if (!api.TryGetCoordinateCenter(cornerName, out var corner))
        {
            waypoints = [];
            return false;
        }

        waypoints = [spawnStart, corner, destination];
        return true;
    }
}
