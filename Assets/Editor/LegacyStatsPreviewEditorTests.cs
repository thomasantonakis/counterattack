using NUnit.Framework;

public sealed class LegacyStatsPreviewEditorTests
{
    [Test] public void OriginalPreviewEventsPopulateAuthoredStatsWithoutChangingLayout()
    {
        using var fixture = new GoalkeeperBoxEntryFixture();
        fixture.RunLegacyStatsPreview();
    }
}
