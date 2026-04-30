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
        var rgb = ParseRgb(color);
        if (rgb is null)
        {
            return "#FFFFFF";
        }

        var luminance =
            (0.2126 * Linearize(rgb.Value.Red)) +
            (0.7152 * Linearize(rgb.Value.Green)) +
            (0.0722 * Linearize(rgb.Value.Blue));

        var whiteContrast = 1.05 / (luminance + 0.05);
        var blackContrast = (luminance + 0.05) / 0.05;

        return blackContrast >= whiteContrast ? "#000000" : "#FFFFFF";
    }

    public static string? InterpolateColor(string? startColor, string? endColor, double amount)
    {
        var start = ParseRgb(startColor);
        var end = ParseRgb(endColor);
        if (start is null || end is null)
        {
            return null;
        }

        var clampedAmount = Math.Clamp(amount, 0d, 1d);
        var red = InterpolateChannel(start.Value.Red, end.Value.Red, clampedAmount);
        var green = InterpolateChannel(start.Value.Green, end.Value.Green, clampedAmount);
        var blue = InterpolateChannel(start.Value.Blue, end.Value.Blue, clampedAmount);
        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    private static (int Red, int Green, int Blue)? ParseRgb(string? color)
    {
        var normalized = NormalizeHexColor(color);
        if (normalized is null)
        {
            return null;
        }

        var hex = normalized.AsSpan(1);
        return (
            Convert.ToInt32(hex[..2].ToString(), 16),
            Convert.ToInt32(hex[2..4].ToString(), 16),
            Convert.ToInt32(hex[4..6].ToString(), 16));
    }

    private static int InterpolateChannel(int start, int end, double amount) =>
        (int)Math.Round(start + ((end - start) * amount), MidpointRounding.AwayFromZero);

    private static double Linearize(int channel)
    {
        var normalized = channel / 255d;
        return normalized <= 0.04045
            ? normalized / 12.92
            : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }
}
