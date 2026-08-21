using MedScans.Histories;

namespace MedScans.Tests.Histories;

public sealed class HistorySimilarityTests
{
    [Fact]
    public void Score_returns_positive_score_for_overlapping_title_tokens()
    {
        var record = HistoryRecord.Create(Guid.NewGuid(), DateTime.UtcNow, "Heartattack", "Some description");
        var criteria = new HistorySearchCriteria(null, "heartattack", null);

        var score = HistorySimilarity.Score(record, criteria);

        Assert.True(score > 0);
    }

    [Fact]
    public void Score_returns_zero_when_no_tokens_overlap()
    {
        var record = HistoryRecord.Create(Guid.NewGuid(), DateTime.UtcNow, "Heartattack", "Some description");
        var criteria = new HistorySearchCriteria(null, "unrelated", "also unrelated");

        var score = HistorySimilarity.Score(record, criteria);

        Assert.Equal(0, score);
    }
}
