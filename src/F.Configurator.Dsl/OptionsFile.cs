namespace F.Configurator.Dsl;

// A `;`-separated file of options: a header of column names, then one row per option. Empty lines are skipped.
internal sealed record OptionsFile(string Path, OptionsLine? Header, IReadOnlyList<OptionsLine> Rows)
{
    public static OptionsFile Read(string path, string text)
    {
        var lines = text.TrimStart('﻿').Split('\n')
            .Select((line, index) => Line(path, index + 1, line.TrimEnd('\r')))
            .Where(line => line.Span.Length > 0)
            .ToList();
        return new OptionsFile(path, lines.FirstOrDefault(), [.. lines.Skip(1)]);
    }

    private static OptionsLine Line(string path, int number, string text)
    {
        var cells = new List<OptionsCell>();
        var column = 1;
        foreach (var cell in text.Split(';'))
        {
            cells.Add(new OptionsCell(cell, new SourceSpan(path, number, column, cell.Length)));
            column += cell.Length + 1;
        }

        return new OptionsLine(cells, new SourceSpan(path, number, 1, text.Length));
    }
}

internal sealed record OptionsLine(IReadOnlyList<OptionsCell> Cells, SourceSpan Span);

internal sealed record OptionsCell(string Text, SourceSpan Span);
