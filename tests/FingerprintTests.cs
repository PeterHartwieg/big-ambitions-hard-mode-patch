using HardModePatch.Core;
using Xunit;

public class FingerprintTests
{
    [Fact]
    public void SameInstructionsGiveTheSameSixteenDigitFingerprint()
    {
        var a = Fingerprint.Of(new[] { "ldarg.0", "call System.Math::Max(System.Int32,System.Int32)", "ret" });
        var b = Fingerprint.Of(new[] { "ldarg.0", "call System.Math::Max(System.Int32,System.Int32)", "ret" });
        Assert.Equal(a, b);
        Assert.Matches("^[0-9a-f]{16}$", a);
    }

    [Fact]
    public void AnyChangedInstructionChangesIt()
    {
        var a = Fingerprint.Of(new[] { "ldc.r4 551.37", "stfld GameInstance::Money", "ret" });
        var b = Fingerprint.Of(new[] { "ldc.r4 551.37", "stfld GameInstance::Money", "pop", "ret" });
        Assert.NotEqual(a, b);
    }
}
