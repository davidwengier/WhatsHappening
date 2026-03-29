namespace WhatsHappening;

public static class ColorHelper
{
    public const string DefaultGroupColor = "#F3F3F3";
    public const string DefaultPickerColor = "#6B7280";

    public static string? NormalizeHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var hex = color.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length == 3)
        {
            if (!hex.All(Uri.IsHexDigit))
            {
                return null;
            }

            hex = string.Create(6, hex, static (buffer, source) =>
            {
                buffer[0] = source[0];
                buffer[1] = source[0];
                buffer[2] = source[1];
                buffer[3] = source[1];
                buffer[4] = source[2];
                buffer[5] = source[2];
            });
        }
        else if (hex.Length != 6 || !hex.All(Uri.IsHexDigit))
        {
            return null;
        }

        return $"#{hex.ToUpperInvariant()}";
    }

    public static string ContrastColor(string? color)
    {
        var normalized = NormalizeHexColor(color);
        if (normalized is null)
        {
            return "#FFFFFF";
        }

        var hex = normalized.AsSpan(1);
        var red = Convert.ToInt32(hex[..2].ToString(), 16);
        var green = Convert.ToInt32(hex[2..4].ToString(), 16);
        var blue = Convert.ToInt32(hex[4..6].ToString(), 16);

        var luminance =
            (0.2126 * Linearize(red)) +
            (0.7152 * Linearize(green)) +
            (0.0722 * Linearize(blue));

        var whiteContrast = 1.05 / (luminance + 0.05);
        var blackContrast = (luminance + 0.05) / 0.05;

        return blackContrast >= whiteContrast ? "#000000" : "#FFFFFF";
    }

    private static double Linearize(int channel)
    {
        var normalized = channel / 255d;
        return normalized <= 0.04045
            ? normalized / 12.92
            : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }
}
