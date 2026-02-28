namespace CompanyIntel.Api.Ingestion;

public static class TextChunker
{
    private const int CharsPerToken = 4;
    private const int TargetTokens = 128;
    private const int MaxTokens = 200;
    private const int OverlapTokens = 25;
    private const int MinTokens = 25;

    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    public static List<string> Chunk(string text)
    {
        var chunks = new List<string>();
        var targetChars = TargetTokens * CharsPerToken;
        var maxChars = MaxTokens * CharsPerToken;
        var overlapChars = OverlapTokens * CharsPerToken;
        var minChars = MinTokens * CharsPerToken;

        SplitRecursive(text, targetChars, maxChars, overlapChars, minChars, chunks);
        return chunks;
    }

    private static void SplitRecursive(
        string text,
        int targetChars,
        int maxChars,
        int overlapChars,
        int minChars,
        List<string> results
    )
    {
        text = text.Trim();
        if (text.Length == 0)
            return;

        if (text.Length <= maxChars)
        {
            if (text.Length >= minChars)
                results.Add(text);
            else if (results.Count > 0 && results[^1].Length + 1 + text.Length <= maxChars)
            {
                results[^1] = results[^1] + " " + text;
            }
            else
            {
                results.Add(text);
            }
            return;
        }

        var separator = FindBestSeparator(text, targetChars);
        var position = 0;

        while (position < text.Length)
        {
            var remaining = text.Length - position;
            if (remaining <= maxChars)
            {
                var final = text[position..].Trim();
                if (final.Length >= minChars)
                    results.Add(final);
                else if (results.Count > 0 && results[^1].Length + 1 + final.Length <= maxChars)
                    results[^1] = results[^1] + " " + final;
                break;
            }

            var splitAt = FindSplitPoint(text, position, targetChars, separator);
            var chunk = text[position..splitAt].Trim();

            if (chunk.Length >= minChars)
                results.Add(chunk);
            else if (results.Count > 0 && results[^1].Length + 1 + chunk.Length <= maxChars)
                results[^1] = results[^1] + " " + chunk;

            position = Math.Max(position + 1, splitAt - overlapChars);
        }
    }

    private static string FindBestSeparator(string text, int targetChars)
    {
        foreach (var sep in Separators)
        {
            var idx = text.IndexOf(
                sep,
                0,
                Math.Min(targetChars * 2, text.Length),
                StringComparison.Ordinal
            );
            if (idx >= 0)
                return sep;
        }
        return " ";
    }

    private static int FindSplitPoint(string text, int start, int targetChars, string separator)
    {
        var searchEnd = Math.Min(start + targetChars + targetChars / 2, text.Length);
        var searchStart = start + targetChars / 2;

        var bestSplit = -1;
        var bestDistance = int.MaxValue;
        var targetPos = start + targetChars;

        var pos = searchStart;
        while (pos < searchEnd)
        {
            var idx = text.IndexOf(separator, pos, searchEnd - pos, StringComparison.Ordinal);
            if (idx < 0)
                break;

            var splitPos = idx + separator.Length;
            var distance = Math.Abs(splitPos - targetPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSplit = splitPos;
            }
            pos = idx + 1;
        }

        if (bestSplit >= 0)
            return bestSplit;

        return Math.Min(start + targetChars, text.Length);
    }
}
