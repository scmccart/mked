using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax.Inlines;

namespace Mked.Controls;

/// <summary>Walks a Markdig AST and produces a list of terminal segment-lines.</summary>
internal sealed class MarkdownBlockRenderer
{
    private readonly bool _showFrontmatter;
    private readonly bool _plainLinks;

    internal MarkdownBlockRenderer(bool showFrontmatter, bool plainLinks)
    {
        _showFrontmatter = showFrontmatter;
        _plainLinks = plainLinks;
    }

    internal (List<List<Segment>> Lines, MarkdownViewerScrollInfo ScrollInfo) Render(
        Markdig.Syntax.MarkdownDocument ast, RenderOptions options, int maxWidth)
    {
        var allLines = new List<List<Segment>>();
        var blockStartLines = new List<int>(ast.Count);

        foreach (var block in ast)
        {
            if (block is Markdig.Syntax.BlankLineBlock
                      or Markdig.Syntax.LinkReferenceDefinitionGroup)
            {
                continue;
            }
            blockStartLines.Add(allLines.Count);
            allLines.AddRange(RenderBlock(block, options, maxWidth));
        }

        return (allLines, new MarkdownViewerScrollInfo(allLines.Count, blockStartLines.AsReadOnly()));
    }

    private List<List<Segment>> RenderBlock(
        Markdig.Syntax.Block block, RenderOptions options, int maxWidth)
    {
        var lines = new List<List<Segment>> { new() };

        switch (block)
        {
            case YamlFrontMatterBlock yaml:
                if (!_showFrontmatter) return new List<List<Segment>>();
                AppendFrontmatter(yaml, lines);
                break;

            case Markdig.Syntax.HeadingBlock heading:
                AppendHeading(heading, lines, options, maxWidth);
                break;

            case Markdig.Syntax.ParagraphBlock paragraph:
                AppendParagraph(paragraph, lines, options, maxWidth);
                break;

            case Markdig.Syntax.CodeBlock code:
                AppendCode(code, lines);
                break;

            case Markdig.Syntax.QuoteBlock quote:
                AppendQuote(quote, lines, options, maxWidth);
                break;

            case Markdig.Syntax.ListBlock list:
                AppendList(list, lines, options, maxWidth, depth: 0);
                break;

            case Markdig.Syntax.HtmlBlock html:
                AppendHtml(html, lines);
                break;

            case Markdig.Syntax.ThematicBreakBlock:
                AppendThematicBreak(lines, options, maxWidth);
                break;

            case Markdig.Extensions.Tables.Table table:
                AppendTable(table, lines, options, maxWidth);
                break;
        }

        // Trailing blank line as block separator
        if (lines[^1].Count > 0)
            lines.Add(new List<Segment>());

        return lines;
    }

    // ─── Block renderers ───────────────────────────────────────────────────────

    private void AppendHeading(
        Markdig.Syntax.HeadingBlock heading, List<List<Segment>> lines,
        RenderOptions options, int maxWidth)
    {
        var color = heading.Level switch
        {
            1 => "blue",
            2 => "green",
            3 => "yellow",
            _ => "grey",
        };
        var text = RenderInlines(heading.Inline);
        AppendRenderable(new Markup($"[bold {color}]{text}[/]"), lines, options, maxWidth);
    }

    private void AppendParagraph(
        Markdig.Syntax.ParagraphBlock paragraph, List<List<Segment>> lines,
        RenderOptions options, int maxWidth)
    {
        var text = RenderInlines(paragraph.Inline);
        AppendRenderable(new Markup(text), lines, options, maxWidth);
    }

    private static void AppendCode(Markdig.Syntax.CodeBlock code, List<List<Segment>> lines)
    {
        var style = new Style(decoration: Decoration.Dim);
        for (int i = 0; i < code.Lines.Count; i++)
        {
            var text = "  " + code.Lines.Lines[i].Slice.ToString();
            lines[^1].Add(new Segment(text, style));
            if (i < code.Lines.Count - 1)
                lines.Add(new List<Segment>());
        }
    }

    private static void AppendFrontmatter(YamlFrontMatterBlock yaml, List<List<Segment>> lines)
    {
        var style = new Style(decoration: Decoration.Dim);
        for (int i = 0; i < yaml.Lines.Count; i++)
        {
            var text = yaml.Lines.Lines[i].Slice.ToString();
            lines[^1].Add(new Segment(text, style));
            if (i < yaml.Lines.Count - 1)
                lines.Add(new List<Segment>());
        }
    }

    private void AppendQuote(
        Markdig.Syntax.QuoteBlock quote, List<List<Segment>> lines,
        RenderOptions options, int maxWidth)
    {
        var prefixSeg = new Segment("│ ", new Style(decoration: Decoration.Dim));
        var innerWidth = Math.Max(1, maxWidth - 2);

        // Render inner blocks into a temporary buffer
        var inner = new List<List<Segment>>();
        foreach (var child in quote)
        {
            var childLines = RenderBlock(child, options, innerWidth);
            inner.AddRange(childLines);
        }
        // Remove trailing blank
        if (inner.Count > 0 && inner[^1].Count == 0)
            inner.RemoveAt(inner.Count - 1);
        if (inner.Count == 0)
            inner.Add(new List<Segment>());

        // Prefix each inner line with the quote marker
        bool first = true;
        foreach (var innerLine in inner)
        {
            if (!first) lines.Add(new List<Segment>());
            lines[^1].Add(prefixSeg);
            lines[^1].AddRange(innerLine);
            first = false;
        }
    }

    private void AppendList(
        Markdig.Syntax.ListBlock list, List<List<Segment>> lines,
        RenderOptions options, int maxWidth, int depth)
    {
        int orderedIndex = 1;
        string indent = new string(' ', depth * 2);

        foreach (var item in list.Cast<Markdig.Syntax.ListItemBlock>())
        {
            string marker = list.IsOrdered ? $"{orderedIndex++}. " : (depth == 0 ? "• " : "◦ ");
            lines[^1].Add(new Segment($"{indent}{marker}"));

            bool firstChild = true;
            foreach (var child in item)
            {
                if (child is Markdig.Syntax.ParagraphBlock para && firstChild)
                {
                    var text = RenderInlines(para.Inline);
                    // Append inline content to same line as marker
                    int usedWidth = indent.Length + marker.Length;
                    AppendRenderable(new Markup(text), lines, options, Math.Max(1, maxWidth - usedWidth));
                }
                else if (child is Markdig.Syntax.ListBlock nestedList)
                {
                    lines.Add(new List<Segment>());
                    AppendList(nestedList, lines, options, maxWidth, depth + 1);
                }
                else if (!firstChild)
                {
                    lines.Add(new List<Segment>());
                    var childLines = RenderBlock(child, options, Math.Max(1, maxWidth - indent.Length - 2));
                    // Remove trailing blank from sub-block
                    if (childLines.Count > 0 && childLines[^1].Count == 0)
                        childLines.RemoveAt(childLines.Count - 1);
                    lines.AddRange(childLines);
                }
                firstChild = false;
            }

            lines.Add(new List<Segment>());
        }
    }

    private static void AppendHtml(Markdig.Syntax.HtmlBlock html, List<List<Segment>> lines)
    {
        for (int i = 0; i < html.Lines.Count; i++)
        {
            var text = html.Lines.Lines[i].Slice.ToString();
            lines[^1].Add(new Segment(text));
            if (i < html.Lines.Count - 1)
                lines.Add(new List<Segment>());
        }
    }

    private static void AppendThematicBreak(
        List<List<Segment>> lines, RenderOptions options, int maxWidth)
    {
        var rule = new Rule { Style = new Style(decoration: Decoration.Dim) };
        AppendRenderable(rule, lines, options, maxWidth);
    }

    private void AppendTable(
        Markdig.Extensions.Tables.Table table, List<List<Segment>> lines,
        RenderOptions options, int maxWidth)
    {
        var spectreTable = new Spectre.Console.Table();
        spectreTable.Border(TableBorder.Rounded);

        // Collect header and data rows
        List<Markdig.Extensions.Tables.TableRow>? headerRows = null;
        List<Markdig.Extensions.Tables.TableRow> dataRows = new();

        foreach (var row in table.Cast<Markdig.Extensions.Tables.TableRow>())
        {
            if (row.IsHeader)
                (headerRows ??= new()).Add(row);
            else
                dataRows.Add(row);
        }

        // Determine column count
        int colCount = table.ColumnDefinitions?.Count ?? 0;
        if (colCount == 0 && headerRows?.Count > 0)
            colCount = headerRows[0].Count;
        if (colCount == 0 && dataRows.Count > 0)
            colCount = dataRows[0].Count;

        // Add columns from header
        if (headerRows is { Count: > 0 })
        {
            foreach (var cell in headerRows[0].Cast<Markdig.Extensions.Tables.TableCell>())
                spectreTable.AddColumn(new TableColumn(new Markup($"[bold]{GetCellMarkup(cell)}[/]")));
        }
        else
        {
            for (int i = 0; i < colCount; i++)
                spectreTable.AddColumn(string.Empty);
        }

        // Add data rows
        foreach (var row in dataRows)
        {
            var cells = row.Cast<Markdig.Extensions.Tables.TableCell>()
                .Select(c => (IRenderable)new Markup(GetCellMarkup(c)))
                .ToArray();
            spectreTable.AddRow(cells);
        }

        AppendRenderable(spectreTable, lines, options, maxWidth);
    }

    // ─── Inline rendering ──────────────────────────────────────────────────────

    private string RenderInlines(ContainerInline? container)
    {
        if (container is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var inline in container)
            AppendInline(inline, sb);
        return sb.ToString();
    }

    private void AppendInline(Inline inline, System.Text.StringBuilder sb)
    {
        switch (inline)
        {
            case LiteralInline literal:
                sb.Append(Markup.Escape(literal.Content.ToString()));
                break;

            case EmphasisInline em:
                var tag = em.DelimiterCount switch
                {
                    2 => "bold",
                    3 => "bold italic",
                    _ => "italic",
                };
                sb.Append('[').Append(tag).Append(']');
                foreach (var child in em)
                {
                    AppendInline(child, sb);
                }
                sb.Append("[/]");
                break;

            case CodeInline code:
                sb.Append("[dim]").Append(Markup.Escape(code.Content)).Append("[/]");
                break;

            case LinkInline link:
                var linkText = new System.Text.StringBuilder();
                foreach (var child in link)
                {
                    AppendInline(child, linkText);
                }
                sb.Append(linkText);
                if (!_plainLinks && link.Url is { Length: > 0 })
                {
                    sb.Append("[dim] (").Append(Markup.Escape(link.Url)).Append(")[/]");
                }
                break;

            case AutolinkInline autolink:
                sb.Append(Markup.Escape(autolink.Url));
                break;

            case LineBreakInline:
                sb.Append('\n');
                break;

            case HtmlInline html:
                sb.Append(Markup.Escape(html.Tag));
                break;

            default:
                if (inline is ContainerInline container)
                {
                    foreach (var child in container)
                    {
                        AppendInline(child, sb);
                    }
                }
                break;
        }
    }

    private string GetCellMarkup(Markdig.Extensions.Tables.TableCell cell)
    {
        foreach (var child in cell)
        {
            if (child is Markdig.Syntax.ParagraphBlock para)
                return RenderInlines(para.Inline);
        }
        return string.Empty;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static void AppendRenderable(
        IRenderable renderable, List<List<Segment>> lines, RenderOptions options, int maxWidth)
    {
        foreach (var seg in renderable.Render(options, maxWidth))
        {
            if (seg.IsLineBreak)
                lines.Add(new List<Segment>());
            else
                lines[^1].Add(seg);
        }
    }
}
