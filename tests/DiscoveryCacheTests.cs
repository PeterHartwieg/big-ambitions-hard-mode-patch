using HardModePatch.Core;
using Xunit;

public class DiscoveryCacheTests
{
    sealed class Lamp { public bool Alive = true; }
    static bool Alive(Lamp lamp) => lamp.Alive;

    [Fact]
    public void EmptySearchIsCachedWithoutRepeatedSeeding()
    {
        var cache = new DiscoveryCache<Lamp>();
        Assert.False(cache.Ready);
        cache.Seed(new Lamp[0]);
        Assert.True(cache.Ready);
        Assert.Empty(cache.Snapshot(Alive));
        Assert.Same(cache.Snapshot(Alive), cache.Snapshot(Alive));
    }

    [Fact]
    public void NewlyEnabledLightsAppearWithoutAnotherGlobalSearch()
    {
        var a = new Lamp(); var b = new Lamp();
        var cache = new DiscoveryCache<Lamp>();
        cache.Seed(new[] { a });
        var first = cache.Snapshot(Alive);
        Assert.True(cache.Track(b));
        Assert.False(cache.Track(b));
        Assert.Equal(new[] { a, b }, cache.Snapshot(Alive));
        Assert.NotSame(first, cache.Snapshot(Alive));
        Assert.Same(cache.Snapshot(Alive), cache.Snapshot(Alive));
    }

    [Fact]
    public void DestroyedLightsDisappearWithOrWithoutADestroyCallback()
    {
        var a = new Lamp(); var b = new Lamp(); var c = new Lamp();
        var cache = new DiscoveryCache<Lamp>();
        cache.Seed(new[] { a, b, c });
        cache.Remove(a);
        b.Alive = false;
        Assert.Equal(new[] { c }, cache.Snapshot(Alive));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void SceneOrOptionResetReleasesOldLightsAndRequiresAFreshSeed()
    {
        var cache = new DiscoveryCache<Lamp>(); var old = new Lamp(); var next = new Lamp();
        cache.Seed(new[] { old });
        cache.Reset();
        Assert.False(cache.Ready);
        Assert.Equal(0, cache.Count);
        Assert.False(cache.Track(old));
        cache.Seed(new[] { next });
        Assert.Equal(new[] { next }, cache.Snapshot(Alive));
    }

    [Fact]
    public void SeedingDeduplicatesAndDoesNotRetainPreviousSceneReferences()
    {
        var cache = new DiscoveryCache<Lamp>(); var old = new Lamp(); var next = new Lamp();
        cache.Seed(new[] { old });
        cache.Seed(new[] { next, next, null! });
        Assert.Equal(new[] { next }, cache.Snapshot(Alive));
    }
}
