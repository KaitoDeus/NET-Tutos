using System.Text.RegularExpressions;
using Markdig;

namespace NET_Tutos.Services;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .UseEmojiAndSmiley()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Pre-sanitize LaTeX / Big-O notations into clean programming code
        markdown = SanitizeMathNotation(markdown);

        var html = Markdown.ToHtml(markdown, _pipeline);

        // Post-sanitize any lingering Markdig mathematics wrappers or raw backslash artifacts
        html = CleanRenderedMathArtifacts(html);

        return html;
    }

    private static string SanitizeMathNotation(string md)
    {
        if (string.IsNullOrEmpty(md)) return md;

        // Big-O notation normalization: $O(1)$ or \(O(1)\) -> `O(1)`
        md = Regex.Replace(md, @"\\\(O\(([^)]*)\)\\\)", "`O($1)`");
        md = Regex.Replace(md, @"\$O\(([^$]+)\)\$", "`O($1)`");
        md = Regex.Replace(md, @"\$\(([^$]+)\)\$", "($1)");

        // LaTeX arrows and math symbols
        md = Regex.Replace(md, @"\$\\rightarrow\$|\\rightarrow\b", "→");
        md = Regex.Replace(md, @"\$\\leftarrow\$|\\leftarrow\b", "←");
        md = Regex.Replace(md, @"\$\\ge\$|\\ge\b", "≥");
        md = Regex.Replace(md, @"\$\\le\$|\\le\b", "≤");
        md = Regex.Replace(md, @"\\times\b", "×");
        md = Regex.Replace(md, @"\\dots\b", "...");
        md = Regex.Replace(md, @"\\log\b", "log");
        md = Regex.Replace(md, @"\\text\{([^}]+)\}", "$1");

        // Clean block equations like $$\text{NewCapacity} = ...$$ -> `NewCapacity = ...`
        md = Regex.Replace(md, @"\$\$(.*?)\$\$", m =>
        {
            var inner = m.Groups[1].Value.Trim();
            inner = Regex.Replace(inner, @"\\text\{([^}]+)\}", "$1");
            inner = inner.Replace(@"\times", "*").Replace(@"\dots", "...").Replace(@"\?", "?").Replace(@"\", "");
            return $"`{inner}`";
        });

        // Strip raw backslash wrapper pairs \(...\) and \[...\]
        md = Regex.Replace(md, @"\\\((.*?)\\\)", "$1");
        md = Regex.Replace(md, @"\\\[(.*?)\\\]", "$1");

        return md;
    }

    private static string CleanRenderedMathArtifacts(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;

        // Remove <span class="math">\(...\)</span> and replace with clean code tag
        html = Regex.Replace(html, @"<span class=""math"">\\\(O\(([^)]*)\)\\\)</span>", "<code>O($1)</code>");
        html = Regex.Replace(html, @"<span class=""math"">\\\((.*?)\\\)</span>", "<code>$1</code>");
        html = Regex.Replace(html, @"<div class=""math"">\\\[(.*?)\\\]</div>", "<pre><code>$1</code></pre>");

        // Remove any orphan backslashes attached to parentheses
        html = html.Replace(@"\(", "").Replace(@"\)", "");

        return html;
    }
}
