using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Veilbound.Systems;

public class SavedRunData
{
    public int Version { get; set; } = 1;
    public string Timestamp { get; set; } = "";
    public string PlayerClass { get; set; } = "Wanderer";
    public int CurrentFloor { get; set; } = 1;
    public int RoomsCleared { get; set; } = 0;

    // Grid coordinates
    public int CurrentRoomX { get; set; } = 4;
    public int CurrentRoomY { get; set; } = 4;
    public int[] GridFlat { get; set; } = new int[81]; // 9x9 flattened
    public List<int[]> VisitedRooms { get; set; } = new();
    public List<int[]> ClearedRooms { get; set; } = new();
    public Dictionary<string, int> RoomThemes { get; set; } = new();

    // Stats
    public int MaxHealth { get; set; } = 6;
    public int CurrentHealth { get; set; } = 6;
    public float Damage { get; set; } = 3.5f;
    public float FireRate { get; set; } = 0.4f;
    public float MoveSpeed { get; set; } = 200.0f;
    public float ProjectileSpeed { get; set; } = 350.0f;
    public float ProjectileRange { get; set; } = 300.0f;
    public int Multishot { get; set; } = 1;
    public int Coins { get; set; } = 0;
    public int Bombs { get; set; } = 1;
    public int Keys { get; set; } = 1;
    public int EnemiesKilled { get; set; } = 0;
    public int ItemsCollected { get; set; } = 0;
}

/// <summary>
/// Handles local device persistent run saving and auto-saving (including Alt+F4 / window close).
/// Permadeath removes the saved run when the player dies.
/// </summary>
public static class SaveManager
{
    private const string SavePath = "user://veilbound_run.json";

    public static bool HasSavedRun()
    {
        return FileAccess.FileExists(SavePath);
    }

    public static void SaveCurrentRun()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsRunActive) return;

        var roomMgr = RoomManager.Instance;
        var tree = (SceneTree)Engine.GetMainLoop();
        var player = tree?.GetFirstNodeInGroup("player") as Player.PlayerController;

        if (roomMgr == null || player == null || roomMgr.Grid == null) return;

        var data = new SavedRunData
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            PlayerClass = gm.SelectedClass.ToString(),
            CurrentFloor = gm.CurrentFloor,
            RoomsCleared = gm.RoomsCleared,
            CurrentRoomX = roomMgr.CurrentRoomPos.X,
            CurrentRoomY = roomMgr.CurrentRoomPos.Y,
            MaxHealth = player.Stats.MaxHealth,
            CurrentHealth = player.Stats.CurrentHealth,
            Damage = player.Stats.Damage,
            FireRate = player.Stats.FireRate,
            MoveSpeed = player.Stats.MoveSpeed,
            ProjectileSpeed = player.Stats.ProjectileSpeed,
            ProjectileRange = player.Stats.ProjectileRange,
            Multishot = player.Stats.Multishot,
            Coins = player.Stats.Coins,
            Bombs = player.Stats.Bombs,
            Keys = player.Stats.Keys,
            EnemiesKilled = player.Stats.EnemiesKilled,
            ItemsCollected = player.Stats.ItemsCollected
        };

        // Flatten grid
        var grid = roomMgr.Grid;
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                data.GridFlat[y * 9 + x] = grid[x, y];
            }
        }

        // Visited
        foreach (var pos in roomMgr.VisitedRooms)
        {
            data.VisitedRooms.Add(new int[] { pos.X, pos.Y });
        }

        // Cleared
        foreach (var pos in roomMgr.ClearedRooms)
        {
            data.ClearedRooms.Add(new int[] { pos.X, pos.Y });
        }

        // Themes
        foreach (var kvp in roomMgr.RoomThemes)
        {
            data.RoomThemes[$"{kvp.Key.X},{kvp.Key.Y}"] = (int)kvp.Value;
        }

        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
                GD.Print($"[SaveManager] Run auto-saved at Floor {data.CurrentFloor}, Room ({data.CurrentRoomX},{data.CurrentRoomY})");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveManager] Failed to save run: {ex.Message}");
        }
    }

    public static SavedRunData LoadRun()
    {
        if (!HasSavedRun()) return null;

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (file == null) return null;
            string json = file.GetAsText();
            return JsonSerializer.Deserialize<SavedRunData>(json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveManager] Failed to load run: {ex.Message}");
            return null;
        }
    }

    public static void ClearSavedRun()
    {
        if (HasSavedRun())
        {
            DirAccess.RemoveAbsolute(SavePath);
            GD.Print("[SaveManager] Saved run cleared (permadeath/completion).");
        }
    }
}
