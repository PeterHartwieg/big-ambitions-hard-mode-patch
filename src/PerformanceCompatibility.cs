using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HardModePatch.Core;

namespace HardModePatch
{
    // These optimizations depend on engine lifecycle semantics, beyond the existing balance patches'
    // signature contracts. Fail open to the authors' discovery on a different or unreadable binary.
    public static class PerformanceCompatibility
    {
        public const string GameHash = "9427561b81aaab2a";
        public const string ExtensionsHash = "fafddfadf7aca598";
        public const string HdrpHash = "d0f8693039963881";
        public const string UnityCoreHash = "5afc743d350e95dc";

        public static bool Reviewed(Assembly assembly, string expected)
        {
            try
            {
                using (var stream = File.OpenRead(assembly.Location))
                using (var sha = SHA256.Create())
                    return TargetGate.HashPrefix(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "")) == expected;
            }
            catch (Exception) { return false; }
        }
    }
}
