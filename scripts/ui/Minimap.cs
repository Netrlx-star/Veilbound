using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// An Isaac-style minimap displaying discovered rooms, current room location,
/// and special room types (Boss, Treasure).
/// </summary>
public partial class Minimap : Control
{
    private const int GridSize = 9;
    private const float CellWidth = 16.0f;
    private const float CellHeight = 11.0f;
    private const float CellSpacing = 3.0f;

    private int[,] _grid;
    private HashSet<Vector2I> _visited = new();
    private Vector2I _currentPos;

    // Palette
    private static readonly Color BgColor = new(0.08f, 0.08f, 0.12f, 0.75f);
    private static readonly Color BorderColor = new(0.3f, 0.3f, 0.45f, 0.8f);
    private static readonly Color NormalRoomColor = new(0.6f, 0.6f, 0.65f, 0.9f);
    private static readonly Color AdjacentRoomColor = new(0.25f, 0.25f, 0.32f, 0.7f);
    private static readonly Color CurrentRoomColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Color BossRoomColor = new(0.9f, 0.25f, 0.25f, 1.0f);
    private static readonly Color TreasureRoomColor = new(1.0f, 0.85f, 0.2f, 1.0f);

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            GridSize * (CellWidth + CellSpacing) + 16,
            GridSize * (CellHeight + CellSpacing) + 16
        );
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

        // Draw background box
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, BgColor, filled: true);
        DrawRect(rect, BorderColor, filled: false, width: 1.5f);

        float originX = 8.0f;
        float originY = 8.0f;

        // Collect all adjacent known rooms to show subtle hints
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

        // Draw adjacent (discovered door but unvisited) rooms
        foreach (var pos in adjacent)
        {
            var cellRect = new Rect2(
                originX + pos.X * (CellWidth + CellSpacing),
                originY + pos.Y * (CellHeight + CellSpacing),
                CellWidth,
                CellHeight
            );
            DrawRect(cellRect, AdjacentRoomColor, filled: true);
            DrawRect(cellRect, BorderColor, filled: false, width: 1.0f);
        }

        // Draw visited rooms
        foreach (var pos in _visited)
        {
            var cellRect = new Rect2(
                originX + pos.X * (CellWidth + CellSpacing),
                originY + pos.Y * (CellHeight + CellSpacing),
                CellWidth,
                CellHeight
            );

            int type = _grid[pos.X, pos.Y];
            Color cellColor = type switch
            {
                2 => BossRoomColor,       // Boss Room (Red)
                3 => TreasureRoomColor,   // Treasure Room (Gold)
                _ => NormalRoomColor      // Normal Room
            };

            DrawRect(cellRect, cellColor, filled: true);

            // Highlight current room
            if (pos == _currentPos)
            {
                DrawRect(cellRect, CurrentRoomColor, filled: false, width: 2.0f);

                // Small center dot for player position
                Vector2 center = cellRect.GetCenter();
                DrawCircle(center, 2.0f, Colors.White);
            }
        }
    }
}
