namespace GameClientWorld;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Realm.MapAPI;
using Realm.Maps;

public class WasmEntryPoint
{
    private static IMapScript? _mapScript;
    private static IGameAPI? _gameApi;

    private static unsafe string ReadString(nint ptr, int len)
    {
        if (ptr == IntPtr.Zero || len <= 0) return string.Empty;
        return System.Text.Encoding.UTF8.GetString((byte*)ptr, len);
    }

    [UnmanagedCallersOnly(EntryPoint = "initialize")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Map scripts are discovered via reflection and kept by TrimmerRootAssembly.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Activator.CreateInstance is used for IMapScript/IWasmModule types rooted for trimming.")]
    public static void Initialize()
    {
        try
        {
            _gameApi = new GameAPI_WasmModule();
            _gameApi.BroadcastMessage("Guest Initialize started");

            // Direct reference survives WASM trimming better than reflection-only discovery.
            _mapScript = new MapScript();

            try
            {
                _mapScript.Initialize(_gameApi);
                _gameApi.BroadcastMessage("Guest WasmModule initialized successfully");
            }
            catch (Exception ex)
            {
                _gameApi.BroadcastMessage($"MapScript.Initialize failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _gameApi?.BroadcastMessage($"WasmEntryPoint.Initialize failed: {ex.Message}");
            Console.WriteLine($"Error in initialize: {ex}");
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "update")]
    public static void Update(float delta)
    {
        try
        {
            if (_mapScript != null && _gameApi != null)
                _mapScript.Update(_gameApi, delta);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in update: {ex}");
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "on-unit-created")]
    public static void OnUnitCreated(int unitId)
    {
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnUnitCreated(unitId);
    }

    [UnmanagedCallersOnly(EntryPoint = "on-unit-died")]
    public static void OnUnitDied(int unitId, int killerId)
    {
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnUnitDied(unitId, killerId);
    }

    [UnmanagedCallersOnly(EntryPoint = "on-unit-damaged")]
    public static void OnUnitDamaged(int victimId, int attackerId, float damage)
    {
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnUnitDamaged(victimId, attackerId, damage);
    }

    [UnmanagedCallersOnly(EntryPoint = "on-spell-cast")]
    public static void OnSpellCast(int casterId, nint spellIdPtr, int spellIdLen, float tx, float ty, float tz)
    {
        string spellId = ReadString(spellIdPtr, spellIdLen);
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnSpellCast(casterId, spellId, new Vector3(tx, ty, tz));
    }

    [UnmanagedCallersOnly(EntryPoint = "on-player-chat-message")]
    public static void OnPlayerChatMessage(nint msgPtr, int msgLen, int senderId)
    {
        string msg = ReadString(msgPtr, msgLen);
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnPlayerChatMessage(msg, senderId);
    }

    [UnmanagedCallersOnly(EntryPoint = "on-player-left")]
    public static void OnPlayerLeft(int playerIndex)
    {
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnPlayerLeft(playerIndex);
    }

    [UnmanagedCallersOnly(EntryPoint = "on-timer-expired")]
    public static void OnTimerExpired(int timerHandle)
    {
        if (_gameApi is GameAPI_WasmModule api) api.TriggerOnTimerExpired(timerHandle);
    }

    [UnmanagedCallersOnly(EntryPoint = "cabi_realloc")]
    public static unsafe nint CabiRealloc(nint ptr, int oldSize, int alignment, int newSize)
    {
        if (ptr == IntPtr.Zero)
            return (nint)NativeMemory.Alloc((nuint)newSize);
        return (nint)NativeMemory.Realloc((void*)ptr, (nuint)newSize);
    }
}
