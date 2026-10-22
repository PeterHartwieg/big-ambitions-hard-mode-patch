using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HarmonyLib;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: FactoryCheck <game Managed folder> <HardModePatch.dll> <RealLifeOverhaul.dll>");
    return 2;
}
string managed = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    if (new[] { "mscorlib", "System", "System.Core", "netstandard" }.Contains(name.Name)) return null;
    string path = Path.Combine(managed, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
var patch = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var rlo = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[2]));
Type bridge = patch.GetType("HardModePatch.RealLifeOverhaulPatch", true);
Type T(string type) => rlo.GetType("RealLifeOverhaul.Careers." + type, true);
const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
object Call(string name, params object[] values) => bridge.GetMethod(name, Any).Invoke(null, values);
object Get(object o, string field) => o.GetType().GetField(field, Any).GetValue(o);
void Set(object o, string field, object value)
{
    var member = o.GetType().GetField(field, Any);
    var setter = new DynamicMethod("FixtureInstanceField", typeof(void), new[] { typeof(object), typeof(object) }, typeof(Program).Module, true);
    var il = setter.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, member.DeclaringType);
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(member.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, member.FieldType);
    il.Emit(OpCodes.Stfld, member); il.Emit(OpCodes.Ret);
    ((Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>)))(o, value);
}
object Rlo(string type, string method, params object[] values) => T(type).GetMethod(method, Any).Invoke(null, values);
int checks = 0;
void Check(bool ok, string name)
{
    if (!ok) throw new Exception("FAIL " + name);
    checks++; Console.WriteLine("PASS " + name);
}
void WriteStatic(FieldInfo field, object value)
{
    var setter = new DynamicMethod("FixtureField", typeof(void), new[] { typeof(object) }, typeof(Program).Module, true);
    var il = setter.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, field.FieldType); il.Emit(OpCodes.Stsfld, field); il.Emit(OpCodes.Ret);
    ((Action<object>)setter.CreateDelegate(typeof(Action<object>)))(value);
}
var rule = patch.GetType("HardModePatch.Core.Rules").GetField("FactoryDiplomas").GetValue(null);
void Option(bool on) => rule.GetType().GetMethod("Set").Invoke(rule, new object[] { on });
var target = Activator.CreateInstance(bridge);
var hooks = (IList<MethodBase>)bridge.GetMethod("ResolveHooks").Invoke(target, new object[] { rlo });
Check(hooks.Count == 10 && hooks.All(h => h != null), "all game and RLO members resolve exactly");
Check(!(bool)Get(rule, "Default"), "new factory rule defaults off");
foreach (var contract in (IEnumerable)bridge.GetMethod("Contracts").Invoke(target, new object[] { rlo, hooks }))
{
    var method = (MethodBase)Get(contract, "Method"); var spec = Get(contract, "Contract");
    string hash = (string)patch.GetType("HardModePatch.IlText").GetMethod("Fingerprint").Invoke(null, new object[] { method });
    Check(hash == (string)Get(spec, "Expected"), "reviewed game purchase contract " + Get(spec, "Name"));
}
var catalogue = T("StudyCatalogue").GetField("All");
var original = (Array)catalogue.GetValue(null);
var originalIds = original.Cast<object>().Select(c => (int)Get(c, "Id")).ToArray();
Check(!originalIds.Intersect(new[] { 18, 19, 34 }).Any(), "released RLO catalogue leaves ids 18, 19, 34 free");
Check(new[] { 18, 19, 34, 35, 36, 37, 38, 39 }.All(id => !originalIds.Contains(id)), "all candidate spare ids verified against released binary");
var harmony = new Harmony("factorycheck");
bridge.GetMethod("Apply").Invoke(target, new object[] { harmony, rlo, hooks });
try
{
    Check(hooks.Take(3).All(h => Harmony.GetPatchInfo(h).Owners.Contains(harmony.Id)), "pickup, checkout and Campus offering hooks apply");
    var extended = (Array)catalogue.GetValue(null);
    Check(extended.Length == original.Length + 3 && !ReferenceEquals(original, extended), "readonly catalogue replaced atomically before careers start");
    Check(original.Cast<object>().Select(c => (int)Get(c, "Id")).SequenceEqual(originalIds), "original courses and order unchanged");
    var defs = new[] { 18, 19, 34 }.Select(id => Rlo("StudyCatalogue", "Find", id)).ToArray();
    int skillCount = (int)T("Skills").GetField("Count").GetRawConstantValue();
    foreach (var c in defs)
    {
        Check(c != null && Get(c, "Kind").ToString() == "Vocational" && Get(c, "Campus").ToString() == "School"
            && (int)Get(c, "Sessions") == 8 && (int)Get(c, "Homework") == 4 && (int)Get(c, "Hours") == 4
            && (int)Get(c, "School") == 2 && (int)Get(c, "Cert") == -1 && !(bool)Get(c, "Distance")
            && (int)Get(c, "Pass") == 50 && !((int[])Get(c, "Needs")).Any(), "native school diploma shape: " + Get(c, "Key"));
        Check((int)Get(c, "SkillA") < skillCount && (int)Get(c, "SkillB") < skillCount
            && (int)Get(c, "Fee") >= 3500 && (int)Get(c, "Fee") <= 4000, "existing skill ids and fee: " + Get(c, "Key"));
        Check(!(bool)Rlo("CareersDirector", "Offered", c), "option off hides new enrolments: " + Get(c, "Key"));
    }
    Option(true);
    Check(defs.All(c => (bool)Rlo("CareersDirector", "Offered", c)), "option on offers all three diplomas through RLO");
    Check(!(bool)Call("Ready", new object[] { null }), "missing director/study fails open");
    var file = Activator.CreateInstance(T("StudyFile"));
    object Enrol(int id)
    {
        var refusal = Rlo("StudyRules", "CanEnrol", file, Rlo("StudyCatalogue", "Find", id), Enum.ToObject(T("StudyMode"), 0));
        Check(Convert.ToInt32(refusal) == 0, "RLO permits native enrolment for id " + id);
        var e = Activator.CreateInstance(T("Enrolment"));
        Set(e, "Course", id); Set(e, "Start", 1); Set(e, "Fee", (int)Get(Rlo("StudyCatalogue", "Find", id), "Fee"));
        ((IList)Get(file, "Courses")).Add(e);
        return e;
    }
    var first = Enrol(18); var second = Enrol(19); var third = Enrol(34);
    for (int day = 1; day <= 21; day++)
    {
        var c = Rlo("StudyCatalogue", "Find", 18);
        if ((bool)Rlo("StudyRules", "On", Get(c, "Days"), day)) Rlo("StudyRules", "Attend", first, day);
        if ((int)Get(first, "Done") == 8)
        {
            Set(first, "Homework", 4);
            Rlo("StudyRules", "Advance", first, day);
            break;
        }
    }
    Check((int)Get(first, "Stage") == 1 && (int)Get(first, "Done") == 8 && (int)Get(first, "ExamFrom") <= 22, "RLO class accounting for eight sittings leads to exam in about three weeks");
    int examDay = (int)Get(first, "ExamFrom");
    while (!(bool)Rlo("StudyRules", "ExamDay", first, examDay)) examDay++;
    int score = (int)Rlo("StudyRules", "ExamScore", first, 35, false, 0);
    Rlo("StudyRules", "Sit", first, score, examDay);
    file.GetType().GetMethod("Finish").Invoke(file, new[] { first });
    Check((bool)file.GetType().GetMethod("Holds").Invoke(file, new object[] { 18 }), "native exam result grants matching diploma");
    Check(!(bool)file.GetType().GetMethod("Holds").Invoke(file, new object[] { 19 }), "unfinished diploma does not qualify");

    var ordinary = Activator.CreateInstance(T("Enrolment"));
    Set(ordinary, "Course", 10); Set(ordinary, "Stage", 2); Set(ordinary, "Score", 75); Set(ordinary, "Grade", 2);
    ((IList)Get(file, "Results")).Add(ordinary);
    Set(file, "LoanCents", 123456L);
    ((int[])Get(file, "Skill"))[16] = 12000;
    string text = (string)Rlo("CareersStore", "Write", file);
    object Read(string saved)
    {
        object[] a = { saved, 0 };
        return T("CareersStore").GetMethod("Read").Invoke(null, a);
    }
    var restored = Read(text);
    Check(((IList)Get(restored, "Courses")).Count == 2 && ((IList)Get(restored, "Results")).Count == 2
        && (bool)restored.GetType().GetMethod("Holds").Invoke(restored, new object[] { 18 }), "enrolled and completed diplomas survive RLO codec roundtrip");
    Check((int)Get(((IList)Get(restored, "Courses"))[0], "Fee") == (int)Get(second, "Fee"), "course progress and fees survive save/load");
    long mask = (long)Rlo("StudyRules", "Passed", restored);
    Check((mask & (1L << 18)) != 0 && (mask & (1L << 19)) == 0, "native 64-bit qualification mask includes new pass only");
    var highFile = Activator.CreateInstance(T("StudyFile"));
    var highResult = Activator.CreateInstance(T("Enrolment"));
    Set(highResult, "Course", 34); Set(highResult, "Stage", 2);
    ((IList)Get(highFile, "Results")).Add(highResult);
    var highRestored = Read((string)Rlo("CareersStore", "Write", highFile));
    Check(((long)Rlo("StudyRules", "Passed", highRestored) & (1L << 34)) != 0
        && (bool)highRestored.GetType().GetMethod("Holds").Invoke(highRestored, new object[] { 34 }), "id 34 survives save/load and the high half of the native mask");
    Option(false);
    Check(((IList)Get(Read(text), "Courses")).Count == 2 && ((IList)Get(Read(text), "Results")).Count == 2, "option off retains enrolled and completed records");
    Option(true);
    var director = RuntimeHelpers.GetUninitializedObject(T("CareersDirector"));
    Set(director, "_study", restored); Set(director, "_studyOn", true);
    var singleton = T("CareersDirector").GetField("<Instance>k__BackingField", Any);
    singleton.SetValue(null, director);
    object Denied(params string[] items) => Call("Denied", (object)items);
    Check(Denied("ba:itemname_industrialsewingmachine") == null, "held matching diploma allows sewing purchase");
    Check(Denied("ba:itemname_kilnmachine") != null && Denied("ba:itemname_polishingmachine") != null, "other diplomas still needed despite sewing pass");
    Check(Denied("ba:itemname_lasercuttingmachine", "ba:itemname_consumergoodsassemblymachine") == null, "laser and plain consumer goods stay free");
    Check(Denied("ba:itemname_lasercuttingmachine", "ba:itemname_kilnmachine") != null, "mixed basket catches an unqualified machine");
    var notices = hooks[0].DeclaringType.Assembly.GetType("UI.Notification.Notifications", true);
    harmony.Patch(notices.GetMethod("ShowError", new[] { typeof(string), typeof(string), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(NoticeSink).GetMethod("Capture")));
    var cargoType = typeof(List<>).MakeGenericType(hooks[1].DeclaringType.GetField("_cargoInstances", Any).FieldType.GetGenericArguments()[0]);
    IList Basket(params string[] items)
    {
        var list = (IList)Activator.CreateInstance(cargoType);
        foreach (var item in items)
        {
            var cargo = RuntimeHelpers.GetUninitializedObject(cargoType.GetGenericArguments()[0]);
            Set(cargo, "itemName", item); Set(cargo, "amount", 1); list.Add(cargo);
        }
        return list;
    }
    var ui = hooks[1].DeclaringType;
    var mode = ui.GetNestedType("Mode"); var uiType = ui.GetNestedType("Type");
    var mixed = Basket("ba:itemname_industrialsewingmachine", "ba:itemname_kilnmachine");
    bool Purchase(IList list, int m, Dictionary<int, int> quantities = null, int kind = 0) =>
        (bool)Call("PurchasePrefix", list, quantities ?? new Dictionary<int, int>(), Enum.ToObject(mode, m), Enum.ToObject(uiType, kind));
    Check(!Purchase(mixed, 0) && NoticeSink.Keys.Last() == "hardmode_factory_requires_hmp_electronics_technician", "checkout refuses mixed basket with matching in-world locale key");
    Check(Purchase(mixed, 1, new Dictionary<int, int> { { 0, 1 }, { 1, 0 } }), "single-toggle checkout ignores unselected locked machines");
    Check(!Purchase(mixed, 1, new Dictionary<int, int> { { 1, 1 } }), "single-toggle checkout refuses selected locked machine");
    Check(Purchase(mixed, 0, kind: 1), "tax payment UI stays untouched");
    var window = RuntimeHelpers.GetUninitializedObject(ui);
    Set(window, "_cargoInstances", mixed); Set(window, "_cargoInstancesAmounts", new Dictionary<int, int>());
    Set(window, "_mode", Enum.ToObject(mode, 0)); Set(window, "_type", Enum.ToObject(uiType, 0));
    int before = NoticeSink.Keys.Count;
    hooks[1].Invoke(window, null);
    Check(NoticeSink.Keys.Count == before + 1 && !(bool)Get(window, "_orderPlaced"), "actual patched checkout stops before money, queue and UI order state");
    var purchaser = Activator.CreateInstance(hooks[0].DeclaringType);
    var controllerType = purchaser.GetType().GetField("itemController").FieldType;
    var controller = RuntimeHelpers.GetUninitializedObject(controllerType);
    var sale = Activator.CreateInstance(controllerType.GetField("playerItemPurchaserSettings").FieldType);
    Set(sale, "enabled", true); Set(sale, "itemName", "ba:itemname_kilnmachine");
    Set(controller, "playerItemPurchaserSettings", sale); Set(purchaser, "itemController", controller);
    before = NoticeSink.Keys.Count;
    hooks[0].Invoke(purchaser, null);
    Check(NoticeSink.Keys.Count == before + 1, "actual patched depot pickup refuses before cargo is created");
    Set(sale, "enabled", false);
    Check((bool)Call("GrabPrefix", purchaser), "owned machine outside a sale is not gated");
    Set(director, "_broken", true);
    Check(Denied("ba:itemname_kilnmachine") == null, "faulted careers director fails open");
    Set(director, "_broken", false);
    var settings = rlo.GetType("RealLifeOverhaul.Settings");
    foreach (string flag in new[] { "Careers", "CareersStudy" })
    {
        settings.GetField(flag).SetValue(null, false);
        Check(Denied("ba:itemname_kilnmachine") == null, flag + " off fails open immediately");
        settings.GetField(flag).SetValue(null, true);
    }
    Set(director, "_studyOn", false);
    Check(Denied("ba:itemname_kilnmachine") == null, "stopped study fails open immediately");
    Set(director, "_studyOn", true);
    Option(false); Check(Denied("ba:itemname_kilnmachine") == null, "HMP option off fails open immediately"); Option(true);
    WriteStatic(catalogue, original);
    Check(Denied("ba:itemname_kilnmachine") == null, "lost catalogue injection fails open");
    var withoutHmp = Read(text);
    Check(((IList)Get(withoutHmp, "Courses")).Count == 0 && ((IList)Get(withoutHmp, "Results")).Count == 1, "unmodified RLO safely ignores removed diploma ids");
    Check((bool)withoutHmp.GetType().GetMethod("Holds").Invoke(withoutHmp, new object[] { 10 })
        && (long)Get(withoutHmp, "LoanCents") == 123456L && ((int[])Get(withoutHmp, "Skill"))[16] == 12000, "removal preserves ordinary diploma, skills and student debt");
    string resavedWithout = (string)Rlo("CareersStore", "Write", withoutHmp);
    Check(resavedWithout is string, "RLO can resave after HMP removal");
    WriteStatic(catalogue, extended);
    // A save played under an unreviewed RLO lost its diploma records; HMP held them (Core/DiplomaHold, RloGate).
    var hold = patch.GetType("HardModePatch.Core.DiplomaHold", true);
    var gateType = patch.GetType("HardModePatch.RloGate", true);
    string held = (string)hold.GetMethod("Keep").Invoke(null, new object[] { text, null });
    Check(held != null && (int)hold.GetMethod("Count").Invoke(null, new object[] { held }) == 3, "the hold takes the three HMP diploma records from RLO's line");
    var gameAssembly = hooks[0].DeclaringType.Assembly;
    Type gameType = gameAssembly.GetType("GameInstance", true);
    object save = RuntimeHelpers.GetUninitializedObject(gameType);
    var registrations = (IList)Activator.CreateInstance(gameType.GetField("BuildingRegistrations").FieldType);
    registrations.Add(RuntimeHelpers.GetUninitializedObject(registrations.GetType().GetGenericArguments()[0]));
    Set(save, "BuildingRegistrations", registrations);
    var modData = new Dictionary<string, string> { { "reallife:careers", resavedWithout }, { "hardmodepatch:factory_diplomas_held", held } };
    Set(save, "modData", modData);
    bool OnLoaded() => (bool)gateType.GetMethod("OnGameLoaded", Any).Invoke(null, new[] { save });
    Check(OnLoaded() && modData.ContainsKey("hardmodepatch:factory_diplomas_held"), "reviewed RLO with courses registered: held records given back, hold kept until RLO saves");
    var given = Read(modData["reallife:careers"]);
    Check(((IList)Get(given, "Courses")).Count == 2 && (int)Get(((IList)Get(given, "Courses"))[0], "Course") == 19
        && (bool)given.GetType().GetMethod("Holds").Invoke(given, new object[] { 18 })
        && (bool)given.GetType().GetMethod("Holds").Invoke(given, new object[] { 10 }) && (long)Get(given, "LoanCents") == 123456L,
        "RLO's own reader takes the given-back course and result, ordinary records unchanged");
    modData["reallife:careers"] = (string)Rlo("CareersStore", "Write", given);
    Check(OnLoaded() && !modData.ContainsKey("hardmodepatch:factory_diplomas_held"), "after RLO has saved them the hold is cleared");
    Check(!OnLoaded(), "nothing held: the careers record is left alone");
    var holdsBridge = bridge.GetField("holds", Any);
    var savedHolds = holdsBridge.GetValue(null);
    holdsBridge.SetValue(null, null);
    Check(Denied("ba:itemname_kilnmachine") == null && (bool)bridge.GetField("failed", Any).GetValue(null), "reflection failure opens the sale and latches the bridge off");
    holdsBridge.SetValue(null, savedHolds);
    singleton.SetValue(null, null);
    var accepts = gateType.GetMethod("Accepts");
    Check(!(bool)accepts.Invoke(null, new object[] { patch }) && (bool)gateType.GetProperty("Refused").GetValue(null), "unreviewed assembly identity rejected");
    Check((bool)accepts.Invoke(null, new object[] { rlo }) && !(bool)gateType.GetProperty("Refused").GetValue(null), "reviewed RLO accepted");
    bridge.GetField("failed", Any).SetValue(null, true);
    Check(Denied("ba:itemname_kilnmachine") == null, "bridge fault latches gate off");
    bridge.GetField("failed", Any).SetValue(null, false);
    try
    {
        Call("Inject", rlo);
        Check(false, "colliding injection rejected");
    }
    catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
    { Check(ReferenceEquals(catalogue.GetValue(null), extended), "id collision rejects injection without partial write"); }
}
finally
{
    bridge.GetMethod("Revert").Invoke(target, null);
    harmony.UnpatchAll(harmony.Id);
}
var missing = (IList<MethodBase>)bridge.GetMethod("ResolveHooks").Invoke(target, new object[] { patch });
Check(missing.Any(h => h == null), "missing or changed RLO shape cannot resolve");
Check(Call("Denied", (object)new[] { "ba:itemname_kilnmachine" }) == null, "missing or changed RLO leaves gate off");
Console.WriteLine($"{checks} offline factory checks passed; Unity UI, class play and actual saves still need an in-game check.");
return 0;

public static class NoticeSink
{
    public static readonly List<string> Keys = new();
    public static bool Capture(string __0) { Keys.Add(__0); return false; }
}
