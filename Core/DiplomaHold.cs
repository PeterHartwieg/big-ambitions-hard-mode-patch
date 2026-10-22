using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HardModePatch.Core
{
    // Keeps a save's factory diploma records while RLO can't read them, and gives them back once a reviewed RLO and
    // HMP's courses are loaded again. RLO's careers line (CareersStore, modData "reallife:careers") reads
    //     v=1~h:...~s:...~c:rec;rec~r:rec;rec~...
    // with fields split by '|'. 'c' holds courses under way, 'r' finished ones. A record's fields start
    //     course|mode|start|seen|done|settled day|homework|fee|instalments|loan|on loan|grant|stage|exam from|exam to|...
    // An RLO that doesn't know a course id drops that record when it reads the line, and the record is gone after
    // RLO's next save.
    //
    // The hold is our own line in the same version: 'c' and 'r' parts whose records are "<day>#<record>", the day the
    // record was last seen in RLO's line. An attempt is its course id and start day, so a failed attempt and a later
    // passed one are two attempts, and the hold is cleared only when every held attempt is back in RLO's line.
    public static class DiplomaHold
    {
        public const string CareersKey = "reallife:careers";
        public const string HoldKey = "hardmodepatch:factory_diplomas_held";
        const int StageField = 12, StartField = 2, SettledField = 5, ExamFromField = 13, ExamToField = 14, ExcusedDayField = 23;
        const int Studying = 0, Exam = 1, Passed = 2, Retake = 6;
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

        // One of our records: where it is ('c' or 'r'), its fields and, in the hold, the day it was last seen.
        sealed class Held
        {
            public char Part;
            public int Day;
            public string[] Fields;
            public string Key { get { return Field(0) + "@" + Field(StartField); } }
            public int Id { get { return Int(0, -1); } }
            public string Field(int i) { return i < Fields.Length ? Fields[i] : string.Empty; }
            public int Int(int i, int otherwise)
            {
                int value;
                return i < Fields.Length && int.TryParse(Fields[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : otherwise;
            }
            public bool Live { get { int stage = Int(StageField, 0); return stage == Studying || stage == Exam || stage == Retake; } }
            public string Record { get { return string.Join("|", Fields); } }
            public string Stored { get { return Day.ToString(CultureInfo.InvariantCulture) + "#" + Record; } }
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

        public static int[] Ids { get { return FactoryDiplomas.All.Select(c => c.Id).ToArray(); } }

        static Held FromLine(char part, string record, int day)
        {
            return new Held { Part = part, Day = day, Fields = record.Split('|') };
        }

        static Held FromHold(char part, string stored)
        {
            int hash = stored.IndexOf('#');
            int day;
            if (hash <= 0 || !int.TryParse(stored.Substring(0, hash), NumberStyles.Integer, CultureInfo.InvariantCulture, out day)) return null;
            return new Held { Part = part, Day = day, Fields = stored.Substring(hash + 1).Split('|') };
        }

        // Our records in RLO's line.
        static List<Held> Ours(Line line, int day)
        {
            var ids = Ids;
            var found = new List<Held>();
            if (line == null) return found;
            foreach (char letter in Parts)
                foreach (var record in line.Records(letter) ?? new List<string>())
                {
                    var held = FromLine(letter, record, day);
                    if (ids.Contains(held.Id)) found.Add(held);
                }
            return found;
        }

        static List<Held> HeldIn(Line hold)
        {
            var found = new List<Held>();
            if (hold == null) return found;
            foreach (char letter in Parts)
                foreach (var stored in hold.Records(letter) ?? new List<string>())
                {
                    var held = FromHold(letter, stored);
                    if (held != null) found.Add(held);
                }
            return found;
        }

        static string Write(string version, IEnumerable<Held> records)
        {
            var hold = new Line { Version = version };
            foreach (var record in records) hold.Ensure(record.Part).Add(record.Stored);
            return hold.Parts.Count == 0 ? null : hold.ToString();
        }

        // How many of our records a careers line or hold carries.
        public static int Count(string text)
        {
            var line = Parse(text);
            if (line == null) return 0;
            int inLine = Ours(line, 0).Count;
            return inLine > 0 ? inLine : HeldIn(line).Count;
        }

        // While RLO can't read our courses: the hold to store. Our attempts in RLO's line, seen today, plus held attempts
        // the line no longer has. Null when there is nothing to hold. RLO's line itself is never changed.
        public static string Keep(string careers, string held, int today)
        {
            var line = Parse(careers);
            var previous = Parse(held);
            if (previous != null && line != null && previous.Version != line.Version) return held;
            string version = line != null ? line.Version : previous != null ? previous.Version : null;
            if (version == null) return null;
            var kept = Ours(line, today);
            var keys = new HashSet<string>(kept.Select(k => k.Key));
            kept.AddRange(HeldIn(previous).Where(h => !keys.Contains(h.Key)));
            return Write(version, kept);
        }

        // True when every held attempt is in RLO's line, so RLO has read and saved it: the hold can go.
        public static bool Covered(string careers, string held)
        {
            var hold = Parse(held);
            if (hold == null) return true;
            var line = Parse(careers);
            if (line == null) return false;
            var present = new HashSet<string>(Ours(line, 0).Select(r => r.Key));
            return HeldIn(hold).All(h => present.Contains(h.Key));
        }

        // A course under way, held for some days, picks up where it stood: its start, last counted class and exam window
        // move on by the days it was held, so RLO counts no classes missed and no exam window lapsed meanwhile.
        static void Pause(Held record, int days)
        {
            if (days <= 0 || !record.Live) return;
            Func<int, bool, string> moved = (field, only) =>
            {
                int value = record.Int(field, int.MinValue);
                if (value == int.MinValue || (only && value < 0)) return record.Field(field);
                return (value + days).ToString(CultureInfo.InvariantCulture);
            };
            int stage = record.Int(StageField, 0);
            var fields = record.Fields.ToArray();
            if (stage == Studying)
            {
                fields[StartField] = moved(StartField, false);
                if (fields.Length > SettledField) fields[SettledField] = moved(SettledField, true);
                if (fields.Length > ExcusedDayField) fields[ExcusedDayField] = moved(ExcusedDayField, true);
            }
            else
            {
                if (fields.Length > ExamFromField) fields[ExamFromField] = moved(ExamFromField, false);
                if (fields.Length > ExamToField) fields[ExamToField] = moved(ExamToField, false);
            }
            record.Fields = fields;
        }

        public sealed class Restored
        {
            // RLO's careers line with the records added; null when nothing was added.
            public string Careers;
            // The hold to store: the added records as written (so a later load can confirm them) and those that waited.
            public string Hold;
            public int Added, Waiting;
        }

        // With a reviewed RLO and our courses registered: RLO's line with each held attempt added that the line doesn't
        // have, within RLO's limits (at most liveMost courses under way, one per course, and resultsMost results), or
        // null when the line or hold can't be used (missing, another version). Attempts that don't fit stay held.
        public static Restored Restore(string careers, string held, int today, int liveMost, int resultsMost)
        {
            var line = Parse(careers);
            var hold = Parse(held);
            if (line == null || hold == null || line.Version != hold.Version) return null;
            var mine = Ours(line, today);
            var present = new HashSet<string>(mine.Select(r => r.Key));
            var others = new List<Held>();
            foreach (var record in line.Records('c') ?? new List<string>()) others.Add(FromLine('c', record, today));
            int live = others.Count(r => r.Live);
            var liveCourses = new HashSet<int>(others.Where(r => r.Live).Select(r => r.Id));
            int results = (line.Records('r') ?? new List<string>()).Count;
            var result = new Restored();
            var stays = new List<Held>();
            // When room is short, a pass comes back before a failed attempt: the pass is what qualifies.
            foreach (var record in HeldIn(hold).OrderBy(r => r.Part == 'r' && r.Int(StageField, 0) != Passed ? 1 : 0))
            {
                if (present.Contains(record.Key)) { stays.Add(record); continue; }
                bool fits = record.Part == 'c'
                    ? record.Live && live < liveMost && !liveCourses.Contains(record.Id)
                    : !record.Live && results < resultsMost;
                if (!fits) { result.Waiting++; stays.Add(record); continue; }
                Pause(record, today - record.Day);
                record.Day = today;
                line.Ensure(record.Part).Add(record.Record);
                if (record.Part == 'c') { live++; liveCourses.Add(record.Id); }
                else results++;
                result.Added++;
                stays.Add(record);
            }
            result.Careers = result.Added == 0 ? null : line.ToString();
            result.Hold = Write(hold.Version, stays);
            return result;
        }
    }
}
