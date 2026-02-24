using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace WhatsHappening;

public static partial class LinkHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAdvancedExtensions()
        .UseAutoLinks()
        .Build();

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
            var trimmed = url.TrimEnd('.', ',', ';', ':', '!', '?');
            var suffix = url[trimmed.Length..];
            return $"""<a href="{trimmed}" target="_blank" rel="noopener noreferrer" onclick="event.stopPropagation()" style="color:var(--accent-fill-rest);">{trimmed}</a>{suffix}""";
        });

        return new MarkupString(result);
    }

    [GeneratedRegex(@"<a\s+href=""", RegexOptions.Compiled)]
    private static partial Regex AnchorPattern();

    public static MarkupString RenderMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new MarkupString(string.Empty);

        var html = Markdown.ToHtml(text, Pipeline);
        // Open all links in new tab
        html = AnchorPattern().Replace(html, "<a target=\"_blank\" rel=\"noopener noreferrer\" href=\"");
        return new MarkupString(html);
    }
}
