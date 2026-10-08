using System;

namespace HardModePatch.Core
{
    public static class CafeMath
    {
        // A session lasts 30 game minutes, so one PC seats two customers an hour.
        public const int SessionsPerPcPerHour = 2;

        // Off screen the game caps customers by building capacity and staffed items only; the cafe's PCs
        // need no staff, so they never lowered the cap. This applies the PC limit the cafe has on screen.
        public static int Cap(int gameCap, int usablePcs)
        {
            return Math.Max(0, Math.Min(gameCap, Math.Max(0, usablePcs) * SessionsPerPcPerHour));
        }
    }
}
