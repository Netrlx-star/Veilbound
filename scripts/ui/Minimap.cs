using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// An authentic Binding of Isaac-style minimap with animated player pulse,
/// door connection bridges, and pixel iconography for special rooms (Boss skull, Treasure star).
/// </summary>
public partial class Minimap : Control
{
    private const int GridSize = 9;
    private const float CellWidth = 15.0f;
    private const float CellHeight = 11.0f;
    private const float CellSpacing = 3.0f;

    private int[,] _grid;
    private HashSet<Vector2I> _visited = new();
    private Vector2I _currentPos;

    // Palette
    private static readonly Color BgColor = new(0.05f, 0.04f, 0.07f, 0.88f);
    private static readonly Color FrameBorder = new(0.35f, 0.30f, 0.45f, 0.90f);
    private static readonly Color NormalRoomColor = new(0.48f, 0.48f, 0.58f, 0.95f);
    private static readonly Color NormalRoomBorder = new(0.24f, 0.24f, 0.32f, 1.0f);
    private static readonly Color AdjacentRoomColor = new(0.18f, 0.18f, 0.25f, 0.65f);
    private static readonly Color AdjacentBorder = new(0.38f, 0.38f, 0.50f, 0.60f);
    private static readonly Color CurrentRoomColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Color BossRoomColor = new(0.85f, 0.20f, 0.25f, 0.95f);
    private static readonly Color TreasureRoomColor = new(0.95f, 0.80f, 0.20f, 0.95f);
    private static readonly Color DoorBridgeColor = new(0.70f, 0.70f, 0.85f, 0.80f);

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            GridSize * (CellWidth + CellSpacing) + 16,
            GridSize * (CellHeight + CellSpacing) + 16
        );
    }

    public override void _Process(double delta)
    {
        // Animate player pulse if visible
        if (_grid != null && _visited.Count > 0)
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Update the minimap state and redraw.
    /// </summary>
    public void Refresh(int[,] grid, HashSet<Vector2I> visited, Vector2I currentPos)
    {
        _grid = grid;
        _visited = visited;
        _currentPos = currentPos;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_grid == null) return;

        // Draw styled parchment/stone background frame
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, BgColor, filled: true);
        DrawRect(rect, FrameBorder, filled: false, width: 2.0f);

        // Corner rivets
        DrawCircle(new Vector2(4, 4), 1.5f, FrameBorder);
        DrawCircle(new Vector2(Size.X - 4, 4), 1.5f, FrameBorder);
        DrawCircle(new Vector2(4, Size.Y - 4), 1.5f, FrameBorder);
        DrawCircle(new Vector2(Size.X - 4, Size.Y - 4), 1.5f, FrameBorder);

        float totalGridW = GridSize * CellWidth + (GridSize - 1) * CellSpacing;
        float totalGridH = GridSize * CellHeight + (GridSize - 1) * CellSpacing;
        float originX = (Size.X - totalGridW) / 2.0f;
        float originY = (Size.Y - totalGridH) / 2.0f;

        // Collect all adjacent rooms (discovered doors leading from visited rooms)
        var adjacent = new HashSet<Vector2I>();
        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        foreach (var v in _visited)
        {
            foreach (var d in dirs)
            {
                var n = v + d;
                if (n.X >= 0 && n.X < GridSize && n.Y >= 0 && n.Y < GridSize)
                {
                    if (_grid[n.X, n.Y] > 0 && !_visited.Contains(n))
                    {
                        adjacent.Add(n);
                    }
                }
            }
        }

        // 1. Draw Door connection bridges between neighboring rooms
        foreach (var pos in _visited)
        {
            var cellRect = GetCellRect(originX, originY, pos);
            foreach (var d in dirs)
            {
                var n = pos + d;
                if (n.X >= 0 && n.X < GridSize && n.Y >= 0 && n.Y < GridSize && _grid[n.X, n.Y] > 0)
                {
                    // Draw bridge if neighbor is visited or discovered adjacent
                    if (_visited.Contains(n) || adjacent.Contains(n))
                    {
                        DrawDoorBridge(cellRect, d);
                    }
                }
            }
        }

        // 2. Draw adjacent (discovered door but unvisited) rooms
        foreach (var pos in adjacent)
        {
            var cellRect = GetCellRect(originX, originY, pos);
            DrawRect(cellRect, AdjacentRoomColor, filled: true);
            DrawRect(cellRect, AdjacentBorder, filled: false, width: 1.0f);

            // Subtle question mark or special room hint if known
            int type = _grid[pos.X, pos.Y];
            if (type == 2) // Boss room discovered
            {
                DrawSkullIcon(cellRect.GetCenter(), new Color(0.9f, 0.3f, 0.3f, 0.7f));
            }
            else if (type == 3) // Treasure room discovered
            {
                DrawStarIcon(cellRect.GetCenter(), new Color(0.95f, 0.85f, 0.3f, 0.7f));
            }
        }

        // 3. Draw visited rooms
        foreach (var pos in _visited)
        {
            var cellRect = GetCellRect(originX, originY, pos);

            int type = _grid[pos.X, pos.Y];
            Color cellColor = type switch
            {
                2 => BossRoomColor,       // Boss Room (Red)
                3 => TreasureRoomColor,   // Treasure Room (Gold)
                _ => NormalRoomColor      // Normal Room
            };

            DrawRect(cellRect, cellColor, filled: true);
            DrawRect(cellRect, NormalRoomBorder, filled: false, width: 1.2f);

            // Icons
            if (type == 2)
            {
                DrawSkullIcon(cellRect.GetCenter(), Colors.White);
            }
            else if (type == 3)
            {
                DrawStarIcon(cellRect.GetCenter(), new Color(0.2f, 0.15f, 0.05f));
            }

            // Highlight current room
            if (pos == _currentPos)
            {
                // Pulsing glowing frame
                float pulse = (Mathf.Sin(Time.GetTicksMsec() * 0.008f) + 1.0f) * 0.5f;
                Color glowColor = new Color(1.0f, 1.0f, 1.0f, 0.7f + pulse * 0.3f);
                DrawRect(cellRect, glowColor, filled: false, width: 2.2f);

                // Small center blip for player position
                Vector2 center = cellRect.GetCenter();
                float blipRadius = 2.0f + pulse * 1.0f;
                DrawCircle(center, blipRadius, Colors.White);
                DrawCircle(center, 1.0f, new Color(0.1f, 0.1f, 0.15f));
            }
        }
    }

    private Rect2 GetCellRect(float originX, float originY, Vector2I pos)
    {
        return new Rect2(
            originX + pos.X * (CellWidth + CellSpacing),
            originY + pos.Y * (CellHeight + CellSpacing),
            CellWidth,
            CellHeight
        );
    }

    private void DrawDoorBridge(Rect2 cellRect, Vector2I dir)
    {
        Vector2 center = cellRect.GetCenter();
        if (dir == Vector2I.Up)
        {
            var bridgeRect = new Rect2(center.X - 2.0f, cellRect.Position.Y - CellSpacing - 0.5f, 4.0f, CellSpacing + 1.0f);
            DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Down)
        {
            var bridgeRect = new Rect2(center.X - 2.0f, cellRect.End.Y - 0.5f, 4.0f, CellSpacing + 1.0f);
            DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Left)
        {
            var bridgeRect = new Rect2(cellRect.Position.X - CellSpacing - 0.5f, center.Y - 2.0f, CellSpacing + 1.0f, 4.0f);
            DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Right)
        {
            var bridgeRect = new Rect2(cellRect.End.X - 0.5f, center.Y - 2.0f, CellSpacing + 1.0f, 4.0f);
            DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
    }

    private void DrawSkullIcon(Vector2 center, Color color)
    {
        // Simple 5x5 retro pixel skull
        DrawCircle(center + new Vector2(0, -1), 2.5f, color);
        DrawRect(new Rect2(center.X - 1.5f, center.Y, 3.0f, 2.0f), color, filled: true);
        // Eye sockets
        Color eyeHole = new Color(0.1f, 0.05f, 0.05f);
        DrawCircle(center + new Vector2(-1, -1), 0.8f, eyeHole);
        DrawCircle(center + new Vector2(1, -1), 0.8f, eyeHole);
    }

    private void DrawStarIcon(Vector2 center, Color color)
    {
        // 5x5 retro star / key emblem
        DrawLine(center + new Vector2(-3, 0), center + new Vector2(3, 0), color, 1.2f);
        DrawLine(center + new Vector2(0, -3), center + new Vector2(0, 3), color, 1.2f);
        DrawCircle(center, 1.2f, color);
    }
}
