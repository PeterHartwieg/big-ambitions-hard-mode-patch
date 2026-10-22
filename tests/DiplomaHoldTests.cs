using HardModePatch.Core;
using Xunit;

public class DiplomaHoldTests
{
    // A careers line as RLO 0.49.0's CareersStore writes it: header, skills, two courses under way, two results.
    const string Line = "v=1~h:7|1|9|0|0|0|1|0|0|-1|0~s:3|5000|5000|8~c:18|0|2|3|3|8|1|3600|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1;5|0|2|1|1|8|0|900|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1~r:34|0|1|8|8|7|4|3800|0|0|0|0|3|20|23|0|71|2|23|0|0|0|0|-1;2|0|1|5|5|6|2|800|0|0|0|0|3|10|12|0|60|3|12|0|0|0|0|-1~w:~q:~t:-1|0";

    [Fact]
    public void KeepHoldsOnlyOurCoursesAndNeverTheOthers()
    {
        string held = DiplomaHold.Keep(Line, null)!;
        Assert.Equal(2, DiplomaHold.Count(held));
        Assert.StartsWith("v=1~", held);
        Assert.Contains("~c:18|", held);
        Assert.Contains("~r:34|", held);
        Assert.DoesNotContain("5|0|2|1", held);
        Assert.DoesNotContain("~h:", held);
        Assert.Equal(2, DiplomaHold.Count(Line));
    }

    [Fact]
    public void NothingToHoldWithoutOurCourses()
    {
        Assert.Null(DiplomaHold.Keep("v=1~h:7|1|9~c:5|0|2~r:2|0|1", null));
        Assert.Null(DiplomaHold.Keep(null, null));
        Assert.Null(DiplomaHold.Keep("garbage", null));
    }

    [Fact]
    public void KeepRetainsHeldCoursesTheLineLost()
    {
        string held = DiplomaHold.Keep(Line, null)!;
        // RLO dropped both after a save; the hold still has them.
        string resaved = "v=1~h:7|1|10~s:3|5000|5000|8~c:5|0|2|1~r:2|0|1|5~w:~q:~t:-1|0";
        Assert.Equal(held, DiplomaHold.Keep(resaved, held));
        // A newer record in the line replaces the held one for that course.
        string newer = "v=1~h:7|1|10~c:18|0|2|5|5|9~r:";
        string kept = DiplomaHold.Keep(newer, held)!;
        Assert.Contains("18|0|2|5|5|9", kept);
        Assert.DoesNotContain("18|0|2|3|3|8", kept);
        Assert.Contains("34|0|1|8", kept);
    }

    [Fact]
    public void RestoreAddsMissingRecordsOnceAndLeavesTheRest()
    {
        string held = DiplomaHold.Keep(Line, null)!;
        string resaved = "v=1~h:7|1|10~s:3|5000|5000|8~c:5|0|2|1~r:2|0|1|5~w:~q:~t:-1|0";
        Assert.False(DiplomaHold.Covered(resaved, held));
        string restored = DiplomaHold.Restore(resaved, held, out int added)!;
        Assert.Equal(2, added);
        Assert.Contains("~c:5|0|2|1;18|", restored);
        Assert.Contains("~r:2|0|1|5;34|", restored);
        Assert.Contains("~h:7|1|10~s:3|5000|5000|8~", restored);
        Assert.EndsWith("~w:~q:~t:-1|0", restored);
        Assert.True(DiplomaHold.Covered(restored, held));
        Assert.Null(DiplomaHold.Restore(restored, held, out added));
        Assert.Equal(0, added);
    }

    [Fact]
    public void RestoreAddsMissingPartsAndRefusesOtherVersions()
    {
        string held = DiplomaHold.Keep(Line, null)!;
        string restored = DiplomaHold.Restore("v=1~h:7|1|10", held, out int added)!;
        Assert.Equal(2, added);
        Assert.Contains("~c:18|", restored);
        Assert.Contains("~r:34|", restored);
        Assert.Null(DiplomaHold.Restore("v=2~h:7|1|10", held, out added));
        Assert.Null(DiplomaHold.Restore(null, held, out added));
        Assert.False(DiplomaHold.Covered(null, held));
        Assert.True(DiplomaHold.Covered("v=1~h:", null));
    }

    [Fact]
    public void ReviewIsByExactHashPrefixAndUnreadableIsNotReviewed()
    {
        Assert.True(TargetGate.Reviewed("4F82B4F88F919178", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.True(TargetGate.Reviewed("2f360a365cf7915f", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.False(TargetGate.Reviewed("0000000000000000", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.False(TargetGate.Reviewed(null, Targets.RealLifeOverhaul.VerifiedHashes));
    }
}
