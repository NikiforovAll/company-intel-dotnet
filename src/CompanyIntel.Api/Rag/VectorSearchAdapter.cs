using System.Text.RegularExpressions;
using CompanyIntel.Api.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.VectorData;

namespace CompanyIntel.Api.Rag;

public static partial class VectorSearchAdapter
{
    [GeneratedRegex(@"## Page (\d+)", RegexOptions.None)]
    private static partial Regex PageHeaderRegex();

    public static Func<
        string,
        CancellationToken,
        Task<IEnumerable<TextSearchProvider.TextSearchResult>>
    > Create(VectorStoreCollection<Guid, DocumentRecord> collection, int top = 5) =>
        async (query, ct) =>
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<TextSearchProvider.TextSearchResult>();
            await foreach (
                var result in collection.SearchAsync(query, top: top, cancellationToken: ct)
            )
            {
                var sourceName = FormatSourceName(result.Record.FileName, result.Record.Text);
                if (!seen.Add(sourceName))
                    continue;

                results.Add(
                    new TextSearchProvider.TextSearchResult
                    {
                        Text = result.Record.Text,
                        SourceName = sourceName,
                        SourceLink = result.Record.Source,
                    }
                );
            }
            return results;
        };

    private static string FormatSourceName(string fileName, string chunkText)
    {
        var match = PageHeaderRegex().Match(chunkText);
        return match.Success ? $"{fileName} (Page {match.Groups[1].Value})" : fileName;
    }
}
