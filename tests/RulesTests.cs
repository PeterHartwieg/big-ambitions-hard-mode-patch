using System.IO;
using System.Linq;
using System.Text.Json;
using HardModePatch.Core;
using Xunit;

public class RulesTests
{
    [Fact]
    public void RuleIdsAreUniqueAndEveryRuleStartsOnItsHardDefault()
    {
        Assert.Equal(Rules.All.Count, Rules.All.Select(r => r.Id).Distinct().Count());
        Assert.All(Rules.All, r => Assert.True(r.Default));
    }

    [Fact]
    public void EveryRuleAndTargetHasAnEnglishLabel()
    {
        var labels = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(File.ReadAllText(Path.Combine("Locales", "en.json")))!;
        Assert.All(Rules.All, r => Assert.True(labels.ContainsKey(r.LabelKey), r.LabelKey));
        Assert.True(labels.ContainsKey("hardmode_title"));
        foreach (var target in Rules.All.Select(r => r.Target).Distinct())
            Assert.True(labels.ContainsKey("hardmode_target_" + target.Key), target.Key);
    }

    [Fact]
    public void VanillaPricesComeBackOnlyForSavesHardcoreDidNotMark()
    {
        Assert.True(HardcoreStart.ShouldRestoreVanillaPrices(snapshotTaken: true, saveIsHardcore: false));
        Assert.False(HardcoreStart.ShouldRestoreVanillaPrices(snapshotTaken: true, saveIsHardcore: true));
        Assert.False(HardcoreStart.ShouldRestoreVanillaPrices(snapshotTaken: false, saveIsHardcore: false));
    }
}

public class RuleChangeTests
{
    [Fact]
    public void SetRaisesChangedOnlyOnARealChange()
    {
        var rule = new Rule("test_rule", Targets.EmpireBank, true);
        int calls = 0;
        bool last = true;
        rule.Changed += value => { calls++; last = value; };
        rule.Set(true);
        Assert.Equal(0, calls);
        rule.Set(false);
        Assert.Equal(1, calls);
        Assert.False(last);
        Assert.False(rule.Enabled);
    }
}
