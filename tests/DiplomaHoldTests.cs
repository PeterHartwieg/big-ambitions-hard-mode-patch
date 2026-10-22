using System.Linq;
using HardModePatch.Core;
using Xunit;

public class DiplomaHoldTests
{
    // Records as RLO 0.49.0's CareersStore writes them: course|mode|start|seen|done|settled|homework|fee|instalments|loan|
    // on loan|grant|stage|exam from|exam to|retakes|score|grade|ended|graduated|fee late|start told|excused|excused day.
    const string Course19 = "19|0|20|3|3|26|1|3900|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1";        // studying since day 20
    const string Course34Exam = "34|0|10|8|8|30|4|3800|0|0|0|0|1|31|36|0|0|0|0|0|0|0|0|-1";   // exam window days 31 to 36
    const string Failed18 = "18|0|1|8|8|7|4|3600|0|0|0|0|3|8|13|1|30|4|13|0|0|0|0|-1";
    const string Passed18 = "18|0|15|8|8|21|4|3600|0|0|0|0|2|22|27|0|71|2|23|0|0|0|0|-1";
    const string Ordinary = "5|0|2|1|1|8|0|900|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1";
    static readonly string Line = "v=1~h:7|1|30|0|0|0|1|0|0|-1|0~s:3|5000|5000|8~c:" + Course19 + ";" + Ordinary + ";" + Course34Exam
        + "~r:" + Failed18 + ";" + Passed18 + ";2|0|1|5|5|6|2|800|0|0|0|0|2|10|12|0|60|3|12|0|0|0|0|-1~w:~q:~t:-1|0";
    const string Resaved = "v=1~h:7|1|50~s:3|5000|5000|8~c:" + Ordinary + "~r:2|0|1|5~w:~q:~t:-1|0";

    [Fact]
    public void KeepHoldsEveryAttemptOfOurCoursesAndNothingElse()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        Assert.Equal(4, DiplomaHold.Count(held));
        Assert.Contains("31#" + Failed18, held);
        Assert.Contains("31#" + Passed18, held);
        Assert.DoesNotContain(Ordinary, held);
        Assert.DoesNotContain("~h:", held);
        Assert.Equal(4, DiplomaHold.Count(Line));
    }

    [Fact]
    public void NothingToHoldWithoutOurCourses()
    {
        Assert.Null(DiplomaHold.Keep("v=1~h:7|1|9~c:" + Ordinary + "~r:2|0|1", null, 9));
        Assert.Null(DiplomaHold.Keep(null, null, 9));
        Assert.Null(DiplomaHold.Keep("garbage", null, 9));
    }

    [Fact]
    public void KeepRetainsHeldAttemptsTheLineLostAndPrefersTheLine()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        Assert.Equal(held, DiplomaHold.Keep(Resaved, held, 50));
        string newer = "v=1~h:7|1|33~c:19|0|20|5|5|28|1|3900|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1~r:";
        string kept = DiplomaHold.Keep(newer, held, 33)!;
        Assert.Contains("33#19|0|20|5|5|28", kept);
        Assert.DoesNotContain("19|0|20|3|3|26", kept);
        Assert.Contains("31#" + Passed18, kept);
        Assert.Equal(4, DiplomaHold.Count(kept));
        Assert.Equal(held, DiplomaHold.Keep("v=2~h:1", held, 40));
    }

    [Fact]
    public void RestoreGivesBackEveryAttemptAndMovesCoursesUnderWayOn()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        Assert.False(DiplomaHold.Covered(Resaved, held));
        var back = DiplomaHold.Restore(Resaved, held, 51, 4, 40)!;
        Assert.Equal(4, back.Added);
        Assert.Equal(0, back.Waiting);
        // Held 20 days: the course under way starts and last settled 20 days later, the exam window moves too.
        Assert.Contains("19|0|40|3|3|46|1|3900", back.Careers);
        Assert.Contains("34|0|10|8|8|30|4|3800|0|0|0|0|1|51|56|", back.Careers);
        // Results come back as they were, both attempts.
        Assert.Contains(Failed18, back.Careers);
        Assert.Contains(Passed18, back.Careers);
        Assert.Contains("~h:7|1|50~s:3|5000|5000|8~", back.Careers);
        Assert.EndsWith("~w:~q:~t:-1|0", back.Careers);
        // The hold now holds what was written, so the next load can confirm it.
        Assert.False(DiplomaHold.Covered(Resaved, back.Hold));
        Assert.True(DiplomaHold.Covered(back.Careers, back.Hold));
        Assert.False(DiplomaHold.Covered(back.Careers, held));
        var again = DiplomaHold.Restore(back.Careers, back.Hold, 52, 4, 40)!;
        Assert.Null(again.Careers);
        Assert.Equal(0, again.Added);
    }

    [Fact]
    public void AttemptsThatDontFitRLOsLimitsWaitInTheHold()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        // 39 results leave room for one result, and a limit of 2 courses for one more course: the pass comes first.
        string full = "v=1~h:7|1|50~c:" + Ordinary + "~r:" + string.Join(";", Enumerable.Range(100, 39).Select(i => i + "|0|1|5|5|6|2|800|0|0|0|0|2|10|12|0|60|3|12|0|0|0|0|-1"));
        var back = DiplomaHold.Restore(full, held, 51, 2, 40)!;
        Assert.Equal(2, back.Added);
        Assert.Equal(2, back.Waiting);
        Assert.Contains(Passed18, back.Careers);
        Assert.DoesNotContain(Failed18, back.Careers);
        Assert.False(DiplomaHold.Covered(back.Careers, back.Hold));
        Assert.Contains("#" + Failed18, back.Hold);
        Assert.Equal(4, DiplomaHold.Count(back.Hold));
    }

    [Fact]
    public void ACourseAlreadyUnderWayInTheLineKeepsTheHeldOneWaiting()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        string line = "v=1~h:7|1|50~c:19|0|45|1|1|46|0|3900|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|-1~r:";
        var back = DiplomaHold.Restore(line, held, 51, 4, 40)!;
        Assert.Equal(3, back.Added);
        Assert.Equal(1, back.Waiting);
        Assert.Contains("31#" + Course19, back.Hold);
    }

    [Fact]
    public void RestoreRefusesOtherVersionsAndMissingLines()
    {
        string held = DiplomaHold.Keep(Line, null, 31)!;
        Assert.Null(DiplomaHold.Restore("v=2~h:7|1|10", held, 40, 4, 40));
        Assert.Null(DiplomaHold.Restore(null, held, 40, 4, 40));
        Assert.False(DiplomaHold.Covered(null, held));
        Assert.True(DiplomaHold.Covered("v=1~h:", null));
        var added = DiplomaHold.Restore("v=1~h:7|1|10", held, 40, 4, 40)!;
        Assert.Equal(4, added.Added);
        Assert.Contains("~c:19|", added.Careers);
    }

    [Fact]
    public void ReviewIsByExactHashPrefixAndUnreadableIsNotReviewed()
    {
        Assert.True(TargetGate.Reviewed("4F82B4F88F919178", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.True(TargetGate.Reviewed("2f360a365cf7915f", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.False(TargetGate.Reviewed("f1b93c176b33f79d", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.False(TargetGate.Reviewed("0000000000000000", Targets.RealLifeOverhaul.VerifiedHashes));
        Assert.False(TargetGate.Reviewed(null, Targets.RealLifeOverhaul.VerifiedHashes));
    }
}
