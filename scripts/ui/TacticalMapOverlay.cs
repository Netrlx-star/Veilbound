using Godot;
using System.Collections.Generic;

namespace Veilbound.UI;

/// <summary>
/// Full-Screen Tactical Map Overlay (accessed via [TAB] or [M]).
/// Provides high-resolution cartography, room type annotations (Boss, Treasure, Shop),
/// door connection corridors, clearing statistics, and legend telemetry.
/// Inspired by Hollow Knight, Dead Cells, and Blasphemous.
/// </summary>
public partial class TacticalMapOverlay : Control
{
    private const int GridSize = 9;
    private const float CellWidth = 44.0f;
    private const float CellHeight = 32.0f;
    private const float CellSpacing = 6.0f;

    private int[,] _grid;
    private HashSet<Vector2I> _visited = new();
    private HashSet<Vector2I> _cleared = new();
    private Vector2I _currentPos;
    private int _floorNumber = 1;

    // Palette
    private static readonly Color BackdropColor = new(0.03f, 0.02f, 0.05f, 0.88f);
    private static readonly Color PanelBgColor = new(0.08f, 0.06f, 0.12f, 0.98f);
    private static readonly Color FrameBorder = new(0.80f, 0.65f, 0.30f, 0.95f);
    private static readonly Color NormalRoomColor = new(0.24f, 0.22f, 0.32f, 0.98f);
    private static readonly Color NormalRoomBorder = new(0.42f, 0.38f, 0.52f, 1.0f);
    private static readonly Color AdjacentRoomColor = new(0.12f, 0.11f, 0.18f, 0.70f);
    private static readonly Color AdjacentBorder = new(0.35f, 0.32f, 0.45f, 0.75f);
    private static readonly Color CurrentRoomGlow = new(1.0f, 0.92f, 0.45f, 1.0f);
    private static readonly Color BossRoomColor = new(0.68f, 0.16f, 0.20f, 0.98f);
    private static readonly Color TreasureRoomColor = new(0.78f, 0.62f, 0.18f, 0.98f);
    private static readonly Color ShopRoomColor = new(0.16f, 0.58f, 0.32f, 0.98f);
    private static readonly Color DoorBridgeColor = new(0.60f, 0.55f, 0.72f, 0.90f);

    private Control _modalRoot;
    private Control _mapCanvas;
    private Label _subtitleLabel;
    private Label _statsFooterLabel;
    private Button _returnButton;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // Allow interaction while game is paused
        Visible = false;

        AnchorsPreset = (int)LayoutPreset.FullRect;

        // 1. Semi-transparent dark background dimmer
        var backdrop = new ColorRect
        {
            Color = BackdropColor,
            LayoutMode = 1
        };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        // 2. Centered Modal Container
        var centerContainer = new CenterContainer
        {
            LayoutMode = 1
        };
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        _modalRoot = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(620, 560)
        };
        _modalRoot.AddThemeConstantOverride("separation", 10);
        centerContainer.AddChild(_modalRoot);

        // Modal Frame Stylebox
        var panelBox = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = PanelBgColor,
            BorderColor = FrameBorder,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0, 0, 0, 0.85f),
            ShadowSize = 16
        };
        panelBox.AddThemeStyleboxOverride("panel", style);
        _modalRoot.AddChild(panelBox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        panelBox.AddChild(margin);

        var contentVBox = new VBoxContainer();
        contentVBox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(contentVBox);

        // Header Box
        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", 2);
        contentVBox.AddChild(headerVBox);

        var runeLabel = new Label
        {
            Text = "— ❖ —",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        runeLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.60f, 0.85f, 0.85f));
        runeLabel.AddThemeFontSizeOverride("font_size", 14);
        headerVBox.AddChild(runeLabel);

        var titleLabel = new Label
        {
            Text = "TACTICAL VEIL CARTOGRAPHY",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.40f));
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        headerVBox.AddChild(titleLabel);

        _subtitleLabel = new Label
        {
            Text = "Depth I · Chapter 1",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _subtitleLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.60f, 0.75f));
        _subtitleLabel.AddThemeFontSizeOverride("font_size", 13);
        headerVBox.AddChild(_subtitleLabel);

        // Map Canvas (Custom drawing surface)
        _mapCanvas = new Control
        {
            CustomMinimumSize = new Vector2(
                GridSize * (CellWidth + CellSpacing) + 20,
                GridSize * (CellHeight + CellSpacing) + 20
            ),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        _mapCanvas.Draw += DrawTacticalMap;
        contentVBox.AddChild(_mapCanvas);

        // Legend Row
        var legendHBox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        legendHBox.AddThemeConstantOverride("separation", 16);
        contentVBox.AddChild(legendHBox);

        AddLegendItem(legendHBox, "❖", "Player", CurrentRoomGlow);
        AddLegendItem(legendHBox, "👑", "Treasure", TreasureRoomColor);
        AddLegendItem(legendHBox, "⚖️", "Shop", ShopRoomColor);
        AddLegendItem(legendHBox, "💀", "Boss", BossRoomColor);
        AddLegendItem(legendHBox, "■", "Explored", NormalRoomColor);
        AddLegendItem(legendHBox, "░", "Pathway", AdjacentBorder);

        // Footer Telemetry & Close Button
        var footerHBox = new HBoxContainer();
        footerHBox.AddThemeConstantOverride("separation", 14);
        contentVBox.AddChild(footerHBox);

        _statsFooterLabel = new Label
        {
            Text = "Rooms Explored: 0 / 0",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statsFooterLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.70f, 0.85f));
        _statsFooterLabel.AddThemeFontSizeOverride("font_size", 13);
        footerHBox.AddChild(_statsFooterLabel);

        _returnButton = new Button
        {
            Text = "▸ RETURN TO DESCENT [TAB]",
            CustomMinimumSize = new Vector2(210, 36)
        };
        _returnButton.AddThemeFontSizeOverride("font_size", 14);
        _returnButton.Pressed += Close;
        footerHBox.AddChild(_returnButton);

        // Style return button with gothic theme
        var btnNormal = Systems.AtmosphereGenerator.CreateGothicStyleBox(
            new Color(0.14f, 0.11f, 0.20f, 0.95f),
            new Color(0.40f, 0.35f, 0.52f, 0.85f),
            1.5f,
            4.0f
        );
        var btnHover = Systems.AtmosphereGenerator.CreateGothicStyleBox(
            new Color(0.22f, 0.16f, 0.32f, 0.98f),
            new Color(1.0f, 0.88f, 0.40f, 1.0f),
            2.0f,
            4.0f
        );
        _returnButton.AddThemeStyleboxOverride("normal", btnNormal);
        _returnButton.AddThemeStyleboxOverride("hover", btnHover);
        _returnButton.AddThemeStyleboxOverride("pressed", btnHover);
        _returnButton.AddThemeStyleboxOverride("focus", btnHover);
        _returnButton.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.98f));
        _returnButton.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.92f, 0.45f));
    }

    private void AddLegendItem(HBoxContainer container, string icon, string label, Color color)
    {
        var itemBox = new HBoxContainer();
        itemBox.AddThemeConstantOverride("separation", 4);

        var iconLabel = new Label { Text = icon };
        iconLabel.AddThemeColorOverride("font_color", color);
        iconLabel.AddThemeFontSizeOverride("font_size", 12);
        itemBox.AddChild(iconLabel);

        var textLabel = new Label { Text = label };
        textLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.65f, 0.78f));
        textLabel.AddThemeFontSizeOverride("font_size", 11);
        itemBox.AddChild(textLabel);

        container.AddChild(itemBox);
    }

    public override void _Process(double delta)
    {
        if (Visible && _grid != null)
        {
            _mapCanvas?.QueueRedraw();
        }
    }

    public void UpdateData(int[,] grid, HashSet<Vector2I> visited, Vector2I currentPos, int floor = 1, HashSet<Vector2I> cleared = null)
    {
        _grid = grid;
        _visited = visited ?? new();
        _cleared = cleared ?? new();
        _currentPos = currentPos;
        _floorNumber = floor;

        if (_subtitleLabel != null)
        {
            _subtitleLabel.Text = $"Depth I · Floor {_floorNumber}";
        }

        UpdateStatsLabel();
        _mapCanvas?.QueueRedraw();
    }

    private void UpdateStatsLabel()
    {
        if (_statsFooterLabel == null || _grid == null) return;

        int totalRooms = 0;
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                if (_grid[x, y] > 0) totalRooms++;
            }
        }

        int visitedCount = _visited.Count;
        _statsFooterLabel.Text = $"Chambers Explored: {visitedCount} / {totalRooms} ({(totalRooms > 0 ? (visitedCount * 100 / totalRooms) : 0)}%)";
    }

    public void Open()
    {
        if (Systems.RoomManager.Instance != null)
        {
            var rm = Systems.RoomManager.Instance;
            int floor = Systems.GameManager.Instance?.CurrentFloor ?? 1;
            UpdateData(rm.Grid, rm.VisitedRooms, rm.CurrentRoomPos, floor, rm.ClearedRooms);
        }

        Visible = true;
        GetTree().Paused = true;

        if (_modalRoot != null)
        {
            _modalRoot.PivotOffset = _modalRoot.Size / 2.0f;
            _modalRoot.Scale = new Vector2(0.92f, 0.92f);
        }

        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process).SetParallel(true);
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.16);

        if (_modalRoot != null)
        {
            tween.TweenProperty(_modalRoot, "scale", Vector2.One, 0.20)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        _returnButton?.GrabFocus();
    }

    public void Close()
    {
        GetTree().Paused = false;

        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.14);
        tween.TweenCallback(Callable.From(() => Visible = false));
    }

    private void DrawTacticalMap()
    {
        if (_grid == null || _mapCanvas == null) return;

        var canvasSize = _mapCanvas.Size;

        // Draw inner slate backing frame with subtle grid lines
        var bgRect = new Rect2(Vector2.Zero, canvasSize);
        _mapCanvas.DrawRect(bgRect, new Color(0.05f, 0.04f, 0.08f, 0.95f), filled: true);
        _mapCanvas.DrawRect(bgRect, new Color(0.28f, 0.24f, 0.38f, 0.85f), filled: false, width: 1.5f);

        // Corner rivets
        _mapCanvas.DrawCircle(new Vector2(6, 6), 2.0f, FrameBorder);
        _mapCanvas.DrawCircle(new Vector2(canvasSize.X - 6, 6), 2.0f, FrameBorder);
        _mapCanvas.DrawCircle(new Vector2(6, canvasSize.Y - 6), 2.0f, FrameBorder);
        _mapCanvas.DrawCircle(new Vector2(canvasSize.X - 6, canvasSize.Y - 6), 2.0f, FrameBorder);

        float totalGridW = GridSize * CellWidth + (GridSize - 1) * CellSpacing;
        float totalGridH = GridSize * CellHeight + (GridSize - 1) * CellSpacing;
        float originX = (canvasSize.X - totalGridW) / 2.0f;
        float originY = (canvasSize.Y - totalGridH) / 2.0f;

        // Collect adjacent unvisited rooms
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
            _mapCanvas.DrawRect(cellRect, AdjacentRoomColor, filled: true);
            _mapCanvas.DrawRect(cellRect, AdjacentBorder, filled: false, width: 1.5f);

            int type = _grid[pos.X, pos.Y];
            Vector2 center = cellRect.GetCenter();

            if (type == 2)
            {
                DrawSkullIcon(center, new Color(0.95f, 0.35f, 0.35f, 0.90f));
            }
            else if (type == 3)
            {
                DrawTreasureIcon(center, new Color(1.0f, 0.88f, 0.35f, 0.90f));
            }
            else if (type == 4)
            {
                DrawShopIcon(center, new Color(0.35f, 0.95f, 0.55f, 0.90f));
            }
            else
            {
                // Question mark hint for undiscovered standard room
                _mapCanvas.DrawCircle(center, 2.0f, new Color(0.50f, 0.45f, 0.60f, 0.70f));
            }
        }

        // 3. Draw visited rooms
        foreach (var pos in _visited)
        {
            var cellRect = GetCellRect(originX, originY, pos);
            int type = _grid[pos.X, pos.Y];

            Color cellColor = type switch
            {
                2 => BossRoomColor,       // Boss Room
                3 => TreasureRoomColor,   // Treasure Room
                4 => ShopRoomColor,       // Shop Room
                _ => NormalRoomColor      // Normal Room
            };

            _mapCanvas.DrawRect(cellRect, cellColor, filled: true);
            _mapCanvas.DrawRect(cellRect, NormalRoomBorder, filled: false, width: 1.5f);

            Vector2 center = cellRect.GetCenter();

            // Icons & Labels
            if (type == 2)
            {
                DrawSkullIcon(center, Colors.White);
            }
            else if (type == 3)
            {
                DrawTreasureIcon(center, new Color(0.15f, 0.10f, 0.05f));
            }
            else if (type == 4)
            {
                DrawShopIcon(center, new Color(0.05f, 0.20f, 0.10f));
            }

            // Current player location highlight
            if (pos == _currentPos)
            {
                float pulse = (Mathf.Sin(Time.GetTicksMsec() * 0.007f) + 1.0f) * 0.5f;
                Color glowColor = new(1.0f, 0.95f, 0.50f, 0.75f + pulse * 0.25f);
                _mapCanvas.DrawRect(cellRect, glowColor, filled: false, width: 2.8f);

                // Pulsing player soul blip
                float blipRadius = 4.0f + pulse * 1.5f;
                _mapCanvas.DrawCircle(center, blipRadius, Colors.White);
                _mapCanvas.DrawCircle(center, 2.0f, new Color(0.12f, 0.10f, 0.18f));
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
            var bridgeRect = new Rect2(center.X - 3.5f, cellRect.Position.Y - CellSpacing - 1.0f, 7.0f, CellSpacing + 2.0f);
            _mapCanvas.DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Down)
        {
            var bridgeRect = new Rect2(center.X - 3.5f, cellRect.End.Y - 1.0f, 7.0f, CellSpacing + 2.0f);
            _mapCanvas.DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Left)
        {
            var bridgeRect = new Rect2(cellRect.Position.X - CellSpacing - 1.0f, center.Y - 3.5f, CellSpacing + 2.0f, 7.0f);
            _mapCanvas.DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
        else if (dir == Vector2I.Right)
        {
            var bridgeRect = new Rect2(cellRect.End.X - 1.0f, center.Y - 3.5f, CellSpacing + 2.0f, 7.0f);
            _mapCanvas.DrawRect(bridgeRect, DoorBridgeColor, filled: true);
        }
    }

    private void DrawSkullIcon(Vector2 center, Color color)
    {
        // High-res pixel skull
        _mapCanvas.DrawCircle(center + new Vector2(0, -3), 5.0f, color);
        _mapCanvas.DrawRect(new Rect2(center.X - 3.5f, center.Y - 1, 7.0f, 5.0f), color, filled: true);
        // Eye sockets
        Color eye = new(0.08f, 0.05f, 0.10f);
        _mapCanvas.DrawCircle(center + new Vector2(-2, -3), 1.5f, eye);
        _mapCanvas.DrawCircle(center + new Vector2(2, -3), 1.5f, eye);
    }

    private void DrawTreasureIcon(Vector2 center, Color color)
    {
        // Royal Crown chalice
        _mapCanvas.DrawLine(center + new Vector2(-6, 3), center + new Vector2(6, 3), color, 2.0f);
        _mapCanvas.DrawLine(center + new Vector2(-5, 3), center + new Vector2(-6, -4), color, 2.0f);
        _mapCanvas.DrawLine(center + new Vector2(5, 3), center + new Vector2(6, -4), color, 2.0f);
        _mapCanvas.DrawLine(center + new Vector2(-6, -4), center + new Vector2(0, 0), color, 2.0f);
        _mapCanvas.DrawLine(center + new Vector2(6, -4), center + new Vector2(0, 0), color, 2.0f);
        _mapCanvas.DrawCircle(center + new Vector2(0, -5), 2.0f, color);
    }

    private void DrawShopIcon(Vector2 center, Color color)
    {
        // Merchant scales / coin emblem
        _mapCanvas.DrawCircle(center, 5.5f, color);
        _mapCanvas.DrawLine(center + new Vector2(0, -7.0f), center + new Vector2(0, 7.0f), Colors.White, 1.8f);
        _mapCanvas.DrawLine(center + new Vector2(-4.0f, 0), center + new Vector2(4.0f, 0), Colors.White, 1.8f);
    }
}
