using Godot;
using System;

namespace Veilbound.Environments;

public enum RoomLayoutType
{
    OpenArena,
    CrossPillars,
    CentralRockCluster,
    CornerObstacles,
    PerimeterPits
}

/// <summary>
/// Generates varied dungeon room obstacle patterns (Rocks and Pots)
/// inspired by The Binding of Isaac room layouts.
/// </summary>
public static class RoomLayoutGenerator
{
    public const int Cols = 28;
    public const int Rows = 15;
    public const float TileSize = 36.0f;
    public const float OriginX = 120.0f;
    public const float OriginY = 90.0f;

    public static void GenerateLayout(Node2D roomNode, RoomLayoutType type)
    {
        char[,] grid = new char[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                grid[r, c] = '.';

        switch (type)
        {
            case RoomLayoutType.CrossPillars:
                // 4 stone pillars in the four quadrants
                PlaceBlock(grid, 3, 5, 'R');
                PlaceBlock(grid, 3, Cols - 7, 'R');
                PlaceBlock(grid, Rows - 5, 5, 'R');
                PlaceBlock(grid, Rows - 5, Cols - 7, 'R');
                break;

            case RoomLayoutType.CentralRockCluster:
                // Central cluster of rocks and pots
                for (int r = 5; r <= 8; r++)
                {
                    for (int c = 12; c <= 15; c++)
                    {
                        grid[r, c] = ((r + c) % 2 == 0) ? 'R' : 'P';
                    }
                }
                break;

            case RoomLayoutType.CornerObstacles:
                // L-shaped rock clusters in room corners
                BuildCornerL(grid, 1, 2);
                BuildCornerL(grid, 1, Cols - 4);
                BuildCornerL(grid, Rows - 3, 2);
                BuildCornerL(grid, Rows - 3, Cols - 4);
                break;

            case RoomLayoutType.PerimeterPits:
                // Inner perimeter border leaving center and door lines clear
                for (int c = 6; c <= Cols - 7; c++)
                {
                    if (Math.Abs(c - Cols / 2) > 2)
                    {
                        grid[3, c] = 'R';
                        grid[Rows - 4, c] = 'R';
                    }
                }
                break;

            case RoomLayoutType.OpenArena:
            default:
                // Scatter a few decorative pots in corners
                grid[2, 3] = 'P';
                grid[2, Cols - 4] = 'P';
                grid[Rows - 3, 3] = 'P';
                grid[Rows - 3, Cols - 4] = 'P';
                break;
        }

        InstantiateObstacles(roomNode, grid);
    }

    private static void PlaceBlock(char[,] grid, int row, int col, char ch)
    {
        if (row >= 0 && row + 1 < Rows && col >= 0 && col + 1 < Cols)
        {
            grid[row, col] = ch;
            grid[row, col + 1] = ch;
            grid[row + 1, col] = ch;
            grid[row + 1, col + 1] = ch;
        }
    }

    private static void BuildCornerL(char[,] grid, int startR, int startC)
    {
        if (startR >= 0 && startR + 1 < Rows && startC >= 0 && startC + 2 < Cols)
        {
            grid[startR, startC] = 'R';
            grid[startR, startC + 1] = 'R';
            grid[startR + 1, startC] = 'P';
        }
    }

    private static void InstantiateObstacles(Node2D roomNode, char[,] grid)
    {
        var obstaclesContainer = new Node2D { Name = "Obstacles" };
        roomNode.AddChild(obstaclesContainer);

        var obstacleScene = GD.Load<PackedScene>("res://scenes/environments/RoomObstacle.tscn");
        if (obstacleScene == null) return;

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                char tile = grid[r, c];
                if (tile == '.') continue;

                var obs = obstacleScene.Instantiate<RoomObstacle>();
                obs.Type = (tile == 'P') ? ObstacleType.Pot : ObstacleType.Rock;
                obs.Position = new Vector2(
                    OriginX + c * TileSize,
                    OriginY + r * TileSize
                );
                obstaclesContainer.AddChild(obs);
            }
        }
    }
}
