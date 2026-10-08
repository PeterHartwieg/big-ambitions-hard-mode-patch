using System.Collections.Generic;

namespace HardModePatch.Core
{
    // A rule is one switch on the options page. Each patch reads its rule every time it runs, so a change
    // takes effect without a restart. The default is the hard value.
    public sealed class Rule
    {
        public readonly string Id;
        public readonly TargetInfo Target;
        public readonly bool Default;
        public bool Enabled;

        public Rule(string id, TargetInfo target, bool defaultValue)
        {
            Id = id;
            Target = target;
            Default = defaultValue;
            Enabled = defaultValue;
        }

        // The options page label is a key in Locales/en.json.
        public string LabelKey { get { return "hardmode_" + Id; } }
    }

    public static class Rules
    {
        // Hardcore Ambitions: keep the indebted start, drop what fights a custom difficulty.
        public static readonly Rule HardcoreKeepDifficulty = new Rule("hardcore_keep_difficulty", Targets.HardcoreAmbitions, true);
        public static readonly Rule HardcoreNoFunds = new Rule("hardcore_no_funds", Targets.HardcoreAmbitions, true);
        public static readonly Rule HardcoreVehiclePricesPerSave = new Rule("hardcore_vehicle_prices_per_save", Targets.HardcoreAmbitions, true);

        public static readonly IList<Rule> All = new[] { HardcoreKeepDifficulty, HardcoreNoFunds, HardcoreVehiclePricesPerSave };
    }
}
