using Godot;

namespace Veilbound.Systems;

/// <summary>
/// Generates placeholder textures for sprites that don't have art yet.
/// Attach this to any Node2D that has a Sprite2D child called "Sprite2D".
/// It creates a simple colored rectangle texture at runtime.
/// 
/// This is temporary — remove once real art assets are in place.
/// </summary>
public partial class PlaceholderSprite : Node
{
    [Export] public Color SpriteColor { get; set; } = Colors.White;
    [Export] public Vector2I SpriteSize { get; set; } = new(24, 28);

    public override void _Ready()
    {
        var sprite = GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite == null || sprite.Texture != null) return;

        // Create a simple colored rectangle texture
        var image = Image.CreateEmpty(SpriteSize.X, SpriteSize.Y, false, Image.Format.Rgba8);
        image.Fill(SpriteColor);

        // Add a darker border (1px)
        var borderColor = SpriteColor.Darkened(0.4f);
        for (int x = 0; x < SpriteSize.X; x++)
        {
            image.SetPixel(x, 0, borderColor);
            image.SetPixel(x, SpriteSize.Y - 1, borderColor);
        }
        for (int y = 0; y < SpriteSize.Y; y++)
        {
            image.SetPixel(0, y, borderColor);
            image.SetPixel(SpriteSize.X - 1, y, borderColor);
        }

        // Add simple eyes (2 dots) for characters
        if (SpriteSize.X >= 16 && SpriteSize.Y >= 16)
        {
            var eyeColor = Colors.White;
            int eyeY = SpriteSize.Y / 3;
            int eyeLeftX = SpriteSize.X / 3;
            int eyeRightX = (SpriteSize.X * 2) / 3;

            // Left eye
            image.SetPixel(eyeLeftX, eyeY, eyeColor);
            image.SetPixel(eyeLeftX + 1, eyeY, eyeColor);
            image.SetPixel(eyeLeftX, eyeY + 1, eyeColor);
            image.SetPixel(eyeLeftX + 1, eyeY + 1, eyeColor);

            // Right eye
            image.SetPixel(eyeRightX, eyeY, eyeColor);
            image.SetPixel(eyeRightX + 1, eyeY, eyeColor);
            image.SetPixel(eyeRightX, eyeY + 1, eyeColor);
            image.SetPixel(eyeRightX + 1, eyeY + 1, eyeColor);
        }

        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Create a simple circle texture for projectiles.
    /// </summary>
    public static ImageTexture CreateCircleTexture(int radius, Color color)
    {
        int size = radius * 2;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = new Vector2(x - radius, y - radius).Length();
                if (dist <= radius)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Creates a 14x12 retro pixel-art heart texture.
    /// state: 2 = Full Heart, 1 = Half Heart, 0 = Empty Container
    /// </summary>
    public static ImageTexture CreateHeartTexture(int state)
    {
        int width = 14;
        int height = 12;
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        // Heart silhouette map (1 = interior, 2 = outline)
        // 14 columns, 12 rows
        string[] map = {
            "..##.....##...",
            ".#..#...#..#..",
            "#....#.#....#.",
            "#...........#.",
            "#...........#.",
            "#...........#.",
            ".#.........#..",
            "..#.......#...",
            "...#.....#....",
            "....#...#.....",
            ".....#.#......",
            "......#.......",
        };

        var outlineColor = new Color(0.18f, 0.04f, 0.06f, 0.95f);
        var fillColor = new Color(0.92f, 0.15f, 0.22f, 1.0f);
        var emptyFill = new Color(0.2f, 0.1f, 0.12f, 0.4f);
        var shineColor = new Color(1.0f, 0.7f, 0.75f, 1.0f);

        // Draw outline and fill based on scanlines
        for (int y = 0; y < height; y++)
        {
            string row = map[y];
            int firstHash = row.IndexOf('#');
            int lastHash = row.LastIndexOf('#');

            if (firstHash != -1 && lastHash != -1)
            {
                // Middle divider for the top dip
                int midDip = (y < 3) ? row.IndexOf('#', firstHash + 1) : -1;

                for (int x = 0; x < width; x++)
                {
                    if (row[x] == '#')
                    {
                        image.SetPixel(x, y, outlineColor);
                    }
                    else if (x > firstHash && x < lastHash)
                    {
                        // Check if within the heart lobes
                        if (y < 2 && (x == 5 || x == 6 || x == 7))
                            continue; // transparent valley between lobes

                        bool isLeftHalf = x <= width / 2;
                        bool shouldFill = state == 2 || (state == 1 && isLeftHalf);

                        if (shouldFill)
                        {
                            // Specular highlight pixel on top-left
                            if (y == 2 && (x == 2 || x == 3))
                                image.SetPixel(x, y, shineColor);
                            else
                                image.SetPixel(x, y, fillColor);
                        }
                        else
                        {
                            image.SetPixel(x, y, emptyFill);
                        }
                    }
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Creates a 12x12 retro gold coin texture.
    /// </summary>
    public static ImageTexture CreateCoinTexture()
    {
        int size = 12;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        var gold = new Color(1.0f, 0.82f, 0.2f);
        var darkGold = new Color(0.65f, 0.45f, 0.1f);
        var shine = new Color(1.0f, 0.95f, 0.6f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = new Vector2(x - 5.5f, y - 5.5f).Length();
                if (dist <= 5.0f)
                {
                    if (dist > 4.0f) image.SetPixel(x, y, darkGold);
                    else if (x <= 4 && y <= 4) image.SetPixel(x, y, shine);
                    else image.SetPixel(x, y, gold);
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Creates a 12x12 retro bomb texture.
    /// </summary>
    public static ImageTexture CreateBombTexture()
    {
        int size = 12;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        var darkBody = new Color(0.2f, 0.22f, 0.26f);
        var shine = new Color(0.5f, 0.55f, 0.62f);
        var fuse = new Color(0.85f, 0.7f, 0.35f);
        var spark = new Color(1.0f, 0.3f, 0.1f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = new Vector2(x - 5.5f, y - 6.5f).Length();
                if (dist <= 4.5f)
                {
                    if (x <= 4 && y <= 5) image.SetPixel(x, y, shine);
                    else image.SetPixel(x, y, darkBody);
                }
            }
        }

        // Fuse & spark
        image.SetPixel(6, 1, fuse);
        image.SetPixel(7, 0, spark);
        image.SetPixel(8, 0, new Color(1.0f, 0.8f, 0.2f));

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Creates a 10x14 retro brass skeleton key texture.
    /// </summary>
    public static ImageTexture CreateKeyTexture()
    {
        int w = 10, h = 14;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        var brass = new Color(0.9f, 0.75f, 0.25f);
        var darkBrass = new Color(0.55f, 0.45f, 0.15f);

        // Key head loop
        for (int x = 2; x <= 7; x++)
        {
            for (int y = 1; y <= 5; y++)
            {
                if (x == 2 || x == 7 || y == 1 || y == 5)
                    image.SetPixel(x, y, brass);
            }
        }

        // Shaft
        for (int y = 6; y <= 12; y++)
        {
            image.SetPixel(4, y, brass);
            image.SetPixel(5, y, darkBrass);
        }

        // Teeth
        image.SetPixel(6, 10, brass);
        image.SetPixel(7, 10, brass);
        image.SetPixel(6, 12, brass);
        image.SetPixel(7, 12, brass);

        return ImageTexture.CreateFromImage(image);
    }
}
