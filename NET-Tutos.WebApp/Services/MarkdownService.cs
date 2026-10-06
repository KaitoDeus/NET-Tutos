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

        return Markdown.ToHtml(markdown, _pipeline);
    }
}

