using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace WhatsHappening;

public static partial class LinkHelper
{
    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.Compiled)]
    private static partial Regex UrlPattern();

    public static MarkupString Linkify(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new MarkupString(string.Empty);

        var escaped = WebUtility.HtmlEncode(text);
        var result = UrlPattern().Replace(escaped, match =>
        {
            var url = match.Value;
            // Trim trailing punctuation that's likely not part of the URL
            var trimmed = url.TrimEnd('.', ',', ';', ':', '!', '?');
            var suffix = url[trimmed.Length..];
            return $"""<a href="{trimmed}" target="_blank" rel="noopener noreferrer" onclick="event.stopPropagation()" style="color:var(--accent-fill-rest);">{trimmed}</a>{suffix}""";
        });

        return new MarkupString(result);
    }
}
