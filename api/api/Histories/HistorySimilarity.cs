namespace MedScans.Histories;

// Placeholder "semantic similarity" scorer using token overlap (Jaccard index) over
// normalized words. This satisfies the plan's "smart search based on semantic
// similarities" requirement without pulling in a heavyweight embedding/vector-search
// dependency. Recommended follow-up (pending review, per the plan's own request):
// replace with embedding-based KNN search, e.g. ONNX sentence-embeddings + cosine
// similarity, consistent with how OnnxBrainTumorAnalyzer already uses onnxruntime
// in this project.
public static class HistorySimilarity
{
    private static readonly char[] Separators = { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' };

    // Every criterion the caller actually specified must match (score > 0) -- a strong hit
    // on one field can't paper over a complete miss on another. Only specified fields are
    // considered at all, so a search using just one filter is unaffected.
    public static double Score(HistoryRecord record, HistorySearchCriteria criteria)
    {
        double score = 0;
        var matched = 0;

        if (!string.IsNullOrWhiteSpace(criteria.Title))
        {
            var titleScore = TokenOverlap(record.Title, criteria.Title);
            if (titleScore == 0)
            {
                return 0;
            }

            score += titleScore;
            matched++;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Description))
        {
            var descriptionScore = TokenOverlap(record.Description, criteria.Description);
            if (descriptionScore == 0)
            {
                return 0;
            }

            score += descriptionScore;
            matched++;
        }

        return matched == 0 ? 0 : score / matched;
    }

    private static double TokenOverlap(string text, string query)
    {
        var textTokens = Tokenize(text);
        var queryTokens = Tokenize(query);

        if (textTokens.Count == 0 || queryTokens.Count == 0)
        {
            return 0;
        }

        var intersection = textTokens.Intersect(queryTokens).Count();
        var union = textTokens.Union(queryTokens).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }
}
