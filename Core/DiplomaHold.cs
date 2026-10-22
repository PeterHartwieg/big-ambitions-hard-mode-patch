using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HardModePatch.Core
{
    // Keeps a save's factory diploma records while an unreviewed RLO runs, and gives them back once a reviewed RLO
    // and HMP's courses are loaded again. RLO's careers line (CareersStore, modData "reallife:careers") reads
    //     v=1~h:...~s:...~c:rec;rec~r:rec;rec~...
    // with fields split by '|'. A record's first field is its course id. An RLO that doesn't know a course id drops
    // that record when it reads the line, and the record is gone after RLO's next save. The hold is a line of our
    // own in the same format holding only the 'c' (under way) and 'r' (finished) records of our course ids.
    public static class DiplomaHold
    {
        public const string CareersKey = "reallife:careers";
        public const string HoldKey = "hardmodepatch:factory_diplomas_held";
        static readonly char[] Parts = { 'c', 'r' };

        sealed class Line
        {
            public string Version;
            public readonly List<KeyValuePair<char, List<string>>> Parts = new List<KeyValuePair<char, List<string>>>();

            public List<string> Records(char letter)
            {
                foreach (var part in Parts) if (part.Key == letter) return part.Value;
                return null;
            }

            public List<string> Ensure(char letter)
            {
                var records = Records(letter);
                if (records != null) return records;
                records = new List<string>();
                Parts.Add(new KeyValuePair<char, List<string>>(letter, records));
                return records;
            }

            public override string ToString()
            {
                var text = new StringBuilder("v=").Append(Version);
                foreach (var part in Parts) text.Append('~').Append(part.Key).Append(':').Append(string.Join(";", part.Value.ToArray()));
                return text.ToString();
            }
        }

        // The line split as RLO's TechReader splits it; null if it isn't one. A later part with the same letter
        // replaces an earlier one there, so it does here too.
        static Line Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var pieces = text.Split('~');
            if (!pieces[0].StartsWith("v=", StringComparison.Ordinal)) return null;
            int version;
            if (!int.TryParse(pieces[0].Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out version)) return null;
            var line = new Line { Version = pieces[0].Substring(2) };
            for (int i = 1; i < pieces.Length; i++)
            {
                string piece = pieces[i];
                if (piece.Length < 2 || piece[1] != ':') continue;
                var records = piece.Substring(2).Split(';').Where(r => r.Length > 0).ToList();
                int at = line.Parts.FindIndex(p => p.Key == piece[0]);
                if (at >= 0) line.Parts[at] = new KeyValuePair<char, List<string>>(piece[0], records);
                else line.Parts.Add(new KeyValuePair<char, List<string>>(piece[0], records));
            }
            return line;
        }

        static int IdOf(string record)
        {
            int bar = record.IndexOf('|');
            int id;
            return int.TryParse(bar < 0 ? record : record.Substring(0, bar), NumberStyles.Integer, CultureInfo.InvariantCulture, out id) ? id : -1;
        }

        static bool Ours(string record, ICollection<int> ids) { return ids.Contains(IdOf(record)); }

        // Every (part, id) our records occupy in a line.
        static HashSet<int> IdsIn(Line line, ICollection<int> ids)
        {
            var found = new HashSet<int>();
            foreach (char letter in Parts)
            {
                var records = line.Records(letter);
                if (records == null) continue;
                foreach (var record in records) if (Ours(record, ids)) found.Add(IdOf(record));
            }
            return found;
        }

        public static int[] Ids { get { return FactoryDiplomas.All.Select(c => c.Id).ToArray(); } }

        // How many of our records the line or hold carries.
        public static int Count(string text)
        {
            var line = Parse(text);
            if (line == null) return 0;
            var ids = Ids;
            return Parts.Sum(letter => (line.Records(letter) ?? new List<string>()).Count(r => Ours(r, ids)));
        }

        // Under an unreviewed RLO: the hold to store, built from our records in the careers line, plus held records
        // for courses the line no longer has. Null when there is nothing to hold. The line itself is never changed.
        public static string Keep(string careers, string held)
        {
            var ids = Ids;
            var line = Parse(careers);
            var previous = Parse(held);
            if (previous != null && line != null && previous.Version != line.Version) previous = null;
            string version = line != null ? line.Version : previous != null ? previous.Version : null;
            if (version == null) return null;
            var kept = new Line { Version = version };
            var present = line == null ? new HashSet<int>() : IdsIn(line, ids);
            foreach (char letter in Parts)
            {
                var records = new List<string>();
                if (line != null && line.Records(letter) != null) records.AddRange(line.Records(letter).Where(r => Ours(r, ids)));
                if (previous != null && previous.Records(letter) != null)
                    records.AddRange(previous.Records(letter).Where(r => Ours(r, ids) && !present.Contains(IdOf(r))));
                if (records.Count > 0) kept.Ensure(letter).AddRange(records);
            }
            return kept.Parts.Count == 0 ? null : kept.ToString();
        }

        // True when every held course is already in the careers line, so RLO read and kept it: the hold can go.
        public static bool Covered(string careers, string held)
        {
            var hold = Parse(held);
            if (hold == null) return true;
            var line = Parse(careers);
            if (line == null) return false;
            var ids = Ids;
            var present = IdsIn(line, ids);
            return IdsIn(hold, ids).All(present.Contains);
        }

        // Under a reviewed RLO with our courses registered: the careers line with each held record added whose course
        // the line has in neither part. Null when the line or hold can't be used (missing, other version) or nothing is
        // missing. RLO's own reader then checks each record as it does any other.
        public static string Restore(string careers, string held, out int added)
        {
            added = 0;
            var line = Parse(careers);
            var hold = Parse(held);
            if (line == null || hold == null || line.Version != hold.Version) return null;
            var ids = Ids;
            var present = IdsIn(line, ids);
            foreach (char letter in Parts)
            {
                var records = hold.Records(letter);
                if (records == null) continue;
                foreach (var record in records)
                {
                    if (!Ours(record, ids) || present.Contains(IdOf(record))) continue;
                    line.Ensure(letter).Add(record);
                    added++;
                }
            }
            return added == 0 ? null : line.ToString();
        }
    }
}
