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
}
