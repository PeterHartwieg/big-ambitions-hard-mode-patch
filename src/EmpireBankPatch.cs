using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HardModePatch.Core;
using HarmonyLib;
using UI.Notification;

namespace HardModePatch
{
    // Empire Bank (3781110629), a bank the player owns and runs from the phone.
    //
    // - Interest withdrawals reach the player as plain cash, which the game never taxes. The gateway's
    //   TryChangePlayerMoney is the one place cash moves; a withdrawal is taxed there at the save's rate.
    // - Emergency central-bank funding ($10M from the Risk Center card, at least $500K from the dashboard's
    //   liquidity banner) all goes through EmpireBankRuntime.RequestEmergencyFunding, which is skipped.
    // - Applicant credit scores are drawn, then raised to the product's minimum (Math.Max), so a minimum of
    //   850 makes every applicant perfect. The Math.Max after BellCredit is replaced so the drawn score stays.
    // - Loan products allow 50% APR, a 10% fee and a one-month term. Edits beyond the limits are refused,
    //   rate nudges are limited before the mod saves them, products loaded from a save are brought within
    //   the limits, approvals are clamped, and corporate negotiation offers are clamped before they're
    //   assessed. Contracts already signed keep their terms.
    // - It fetches /v1/stats and supporters.json from the author's servers without asking. The stats
    //   coroutine (reached from Settings and after a heartbeat) and the supporters refresh are skipped.
    //   The opt-in heartbeat is left to the player.
    public sealed class EmpireBankPatch : ITargetPatch
    {
        const int Gateway = 0, Emergency = 1, LoanApplicants = 2, CardApplicants = 3, UpdateLoan = 4, UpdateProduct = 5, AdjustRate = 6,
            Stats = 7, Supporters = 8, EmergencyCard = 9, SaveData = 10, ApproveLoan = 11, ApproveCard = 12, Negotiate = 13;
        const string WithdrawalReason = "withdraw";
        static PropertyInfo productsProperty, productIdProperty, productTypeProperty, rateProperty, feeProperty, termProperty;
        static PropertyInfo casesProperty, caseIdProperty, caseTypeProperty;

        public TargetInfo Target { get { return Targets.EmpireBank; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var hooks = new MethodBase[14];
            var gateway = target.GetType("EmpireBank.Integration.BigAmbitionsPlayerEconomyGateway", false);
            var card = target.GetType("EmpireBank.UI.EmpireBankEmergencyCard", false);
            var loans = target.GetType("EmpireBank.Core.LoanApplicationService", false);
            var cards = target.GetType("EmpireBank.Core.CreditCardApplicationService", false);
            var runtime = target.GetType("EmpireBank.Core.EmpireBankRuntime", false);
            var product = target.GetType("EmpireBank.Models.BankProduct", false);
            var telemetry = target.GetType("EmpireTelemetry.EmpireTelemetryClient", false);
            var supporters = target.GetType("EmpireBank.UI.EmpireBankSupportersView", false);
            var negotiation = target.GetType("EmpireBank.Models.NegotiationCase", false);

            if (gateway != null) hooks[Gateway] = Declared(gateway, "TryChangePlayerMoney", typeof(decimal), typeof(string));
            if (card != null) hooks[EmergencyCard] = Declared(card, "OnRequest");
            if (loans != null) hooks[LoanApplicants] = WithScoreClamp(loans, "Create");
            if (cards != null) hooks[CardApplicants] = WithScoreClamp(cards, "CreateApplication");
            if (runtime != null) hooks[Emergency] = Declared(runtime, "RequestEmergencyFunding", typeof(decimal));
            if (runtime != null && product != null && ReadProductMembers(runtime, product))
            {
                hooks[UpdateLoan] = Find(runtime, "UpdateLoanProduct", "productId", "apr", "termMonths", "feeRate", "message");
                hooks[UpdateProduct] = Find(runtime, "UpdateProduct", "productId", "rate", "termMonths", "feeValue", "message");
                hooks[AdjustRate] = Declared(runtime, "AdjustProductRate", typeof(string), typeof(decimal));
                hooks[SaveData] = Find(runtime, "ApplySaveData");
                hooks[ApproveLoan] = Find(runtime, "ApproveApplication", "id", "apr", "term");
                hooks[ApproveCard] = Find(runtime, "ApproveCreditCardApplication", "id", "apr");
                if (negotiation != null && ReadNegotiationMembers(runtime, negotiation))
                    hooks[Negotiate] = Find(runtime, "SubmitNegotiationOffer", "caseId", "rate", "termMonths", "feeRate");
            }
            // The stats coroutine itself: Settings reaches it through FetchStatsIfStale, a successful heartbeat directly.
            if (telemetry != null)
            {
                var fetch = AccessTools.DeclaredMethod(telemetry, "FetchStats", Type.EmptyTypes) as MethodInfo;
                hooks[Stats] = fetch != null && fetch.ReturnType == typeof(IEnumerator) ? fetch : null;
            }
            if (supporters != null) hooks[Supporters] = Declared(supporters, "StartRemoteRefresh");
            return hooks;
        }

        static MethodBase Declared(Type type, string name, params Type[] parameters)
        {
            return AccessTools.DeclaredMethod(type, name, parameters);
        }

        static bool HasParameters(MethodInfo method, params string[] names)
        {
            var actual = method.GetParameters().Select(p => p.Name).ToArray();
            return names.All(actual.Contains);
        }

        // The one declared method of that name, provided it has every named parameter the patch binds to.
        static MethodBase Find(Type type, string name, params string[] parameters)
        {
            var matches = AccessTools.GetDeclaredMethods(type).Where(m => m.Name == name).ToList();
            return matches.Count == 1 && HasParameters(matches[0], parameters) ? matches[0] : null;
        }

        static bool HasEnumValues(Type type, params string[] names)
        {
            return type.IsEnum && names.All(n => Enum.GetNames(type).Contains(n));
        }

        static bool Writable(PropertyInfo property, Type type)
        {
            return property != null && property.PropertyType == type && property.CanRead && property.CanWrite;
        }

        // Only a method whose IL still holds the BellCredit-then-Math.Max pair can be rewritten.
        static MethodBase WithScoreClamp(Type service, string name)
        {
            var method = AccessTools.GetDeclaredMethods(service).FirstOrDefault(m => m.Name == name);
            if (method == null) return null;
            try { return CountScoreClamps(PatchProcessor.GetOriginalInstructions(method)) == 1 ? method : null; }
            catch (Exception) { return null; }
        }

        static readonly MethodInfo MathMax = AccessTools.Method(typeof(Math), "Max", new[] { typeof(int), typeof(int) });

        static int CountScoreClamps(IList<CodeInstruction> code)
        {
            int count = 0;
            for (int index = 1; index < code.Count; index++)
                if (IsBellCredit(code[index - 1]) && code[index].Calls(MathMax)) count++;
            return count;
        }

        static bool IsBellCredit(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && method != null && method.Name == "BellCredit" && method.ReturnType == typeof(int);
        }

        static bool ReadProductMembers(Type runtime, Type product)
        {
            productsProperty = AccessTools.Property(runtime, "Products");
            productIdProperty = AccessTools.Property(product, "ProductId");
            productTypeProperty = AccessTools.Property(product, "ProductType");
            rateProperty = AccessTools.Property(product, "InterestRate");
            feeProperty = AccessTools.Property(product, "OriginationFeeRate");
            termProperty = AccessTools.Property(product, "TermMonths");
            return productsProperty != null && typeof(IEnumerable).IsAssignableFrom(productsProperty.PropertyType)
                && productIdProperty != null && productIdProperty.PropertyType == typeof(string)
                && productTypeProperty != null && HasEnumValues(productTypeProperty.PropertyType, "Loan", "CreditCard")
                && Writable(rateProperty, typeof(decimal)) && Writable(feeProperty, typeof(decimal)) && Writable(termProperty, typeof(int));
        }

        static bool ReadNegotiationMembers(Type runtime, Type negotiation)
        {
            casesProperty = AccessTools.Property(runtime, "NegotiationCases");
            caseIdProperty = AccessTools.Property(negotiation, "CaseId");
            caseTypeProperty = AccessTools.Property(negotiation, "CaseType");
            return casesProperty != null && typeof(IEnumerable).IsAssignableFrom(casesProperty.PropertyType)
                && caseIdProperty != null && caseIdProperty.PropertyType == typeof(string)
                && caseTypeProperty != null && HasEnumValues(caseTypeProperty.PropertyType, "CorporateCreditFacility", "PreferredDeposit");
        }

        public IList<ContractCheck> Contracts(Assembly target, IList<MethodBase> hooks) { return new ContractCheck[0]; }

        public void Revert() { }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            var self = typeof(EmpireBankPatch);
            harmony.Patch(hooks[Gateway], prefix: new HarmonyMethod(self, "MoneyPrefix"));
            harmony.Patch(hooks[Emergency], prefix: new HarmonyMethod(self, "EmergencyPrefix"));
            harmony.Patch(hooks[EmergencyCard], prefix: new HarmonyMethod(self, "EmergencyPrefix"));
            harmony.Patch(hooks[LoanApplicants], transpiler: new HarmonyMethod(self, "ScoreTranspiler"));
            harmony.Patch(hooks[CardApplicants], transpiler: new HarmonyMethod(self, "ScoreTranspiler"));
            harmony.Patch(hooks[UpdateLoan], prefix: new HarmonyMethod(self, "UpdateLoanPrefix"));
            harmony.Patch(hooks[UpdateProduct], prefix: new HarmonyMethod(self, "UpdateProductPrefix"));
            harmony.Patch(hooks[AdjustRate], prefix: new HarmonyMethod(self, "AdjustRatePrefix"));
            harmony.Patch(hooks[SaveData], postfix: new HarmonyMethod(self, "SaveDataPostfix"));
            harmony.Patch(hooks[ApproveLoan], prefix: new HarmonyMethod(self, "ApproveLoanPrefix"));
            harmony.Patch(hooks[ApproveCard], prefix: new HarmonyMethod(self, "ApproveCardPrefix"));
            harmony.Patch(hooks[Negotiate], prefix: new HarmonyMethod(self, "NegotiatePrefix"));
            harmony.Patch(hooks[Stats], prefix: new HarmonyMethod(self, "StatsPrefix"));
            harmony.Patch(hooks[Supporters], prefix: new HarmonyMethod(self, "RemotePrefix"));
        }

        static void MoneyPrefix(ref decimal amount, string reason)
        {
            try
            {
                if (!Rules.BankTaxWithdrawals.Enabled || amount <= 0m || reason == null) return;
                if (reason.IndexOf(WithdrawalReason, StringComparison.OrdinalIgnoreCase) < 0) return;
                var game = SaveGameManager.Current;
                if (game == null || game.gameVariables == null) return;
                decimal gross = amount;
                amount = BankRules.AfterTax(gross, game.gameVariables.taxPercentage);
                Log.Info("Empire Bank: withdrawal of " + gross.ToString("0.00") + " taxed at " + game.gameVariables.taxPercentage + "%, " + amount.ToString("0.00") + " paid out.");
            }
            catch (Exception ex) { Log.Error("Empire Bank: withdrawal left untaxed: " + ex); }
        }

        // Both entry points: the Risk Center card (its click handler) and the runtime method every path uses.
        // The card's handler is skipped too so it doesn't refresh the UI for funding that never came.
        static bool EmergencyPrefix()
        {
            if (!Rules.BankNoEmergencyBorrowing.Enabled) return true;
            try { Notifications.ShowError("hardmode_bank_emergency_blocked", "hardmodeEmergencyBlocked"); }
            catch (Exception) { }
            Log.Info("Empire Bank: emergency borrowing refused.");
            return false;
        }

        // Called in place of Math.Max(product.MinimumCreditScore, BellCredit(rng)), same arguments.
        public static int ApplicantScore(int productMinimum, int drawn)
        {
            return Rules.BankHonestCreditScores.Enabled ? BankRules.ApplicantScore(productMinimum, drawn) : Math.Max(productMinimum, drawn);
        }

        static IEnumerable<CodeInstruction> ScoreTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var replacement = AccessTools.Method(typeof(EmpireBankPatch), "ApplicantScore");
            int replaced = 0;
            for (int index = 1; index < code.Count; index++)
            {
                if (!IsBellCredit(code[index - 1]) || !code[index].Calls(MathMax)) continue;
                code[index].opcode = OpCodes.Call;
                code[index].operand = replacement;
                replaced++;
            }
            if (replaced != 1) throw new InvalidOperationException("expected one credit score clamp, found " + replaced);
            return code;
        }

        static bool UpdateLoanPrefix(decimal apr, int termMonths, decimal feeRate, ref string message, ref bool __result)
        {
            if (!Rules.BankProductLimits.Enabled) return true;
            string refusal = BankRules.CheckLoan(apr, feeRate, termMonths);
            if (refusal == null) return true;
            message = refusal;
            __result = false;
            return false;
        }

        static bool UpdateProductPrefix(object __instance, string productId, decimal rate, int termMonths, decimal feeValue, ref string message, ref bool __result)
        {
            if (!Rules.BankProductLimits.Enabled) return true;
            try
            {
                string kind = KindOf(FindProduct(__instance, productId));
                string refusal = kind == "Loan" ? BankRules.CheckLoan(rate, feeValue, termMonths) : kind == "CreditCard" ? BankRules.CheckCard(rate) : null;
                if (refusal == null) return true;
                message = refusal;
                __result = false;
                return false;
            }
            catch (Exception ex) { Log.Error("Empire Bank: product edit not checked: " + ex); return true; }
        }

        // The mod saves the new rate as soon as it applies the change, so the change is limited first.
        static void AdjustRatePrefix(object __instance, string productId, ref decimal delta)
        {
            if (!Rules.BankProductLimits.Enabled || delta <= 0m) return;
            try
            {
                var product = FindProduct(__instance, productId);
                if (product == null) return;
                string kind = KindOf(product);
                delta = BankRules.ClampDelta(kind == "Loan", kind == "CreditCard", (decimal)rateProperty.GetValue(product, null), delta);
            }
            catch (Exception ex) { Log.Error("Empire Bank: rate change not checked: " + ex); }
        }

        // Products in a save may predate the limits (or the rule being switched on). New applications copy a
        // product's rate and term and approvals read its fee, so loaded products are brought within the limits.
        static void SaveDataPostfix(object __instance)
        {
            if (!Rules.BankProductLimits.Enabled) return;
            try { NormalizeProducts(__instance); }
            catch (Exception ex) { Log.Error("Empire Bank: saved products not checked: " + ex); }
        }

        static void ApproveLoanPrefix(object __instance, ref decimal apr, ref int term)
        {
            if (!Rules.BankProductLimits.Enabled) return;
            try
            {
                NormalizeProducts(__instance);
                apr = BankRules.ClampRate(true, false, apr);
                term = BankRules.ClampTerm(term);
            }
            catch (Exception ex) { Log.Error("Empire Bank: approval not checked: " + ex); }
        }

        static void ApproveCardPrefix(ref decimal apr)
        {
            if (Rules.BankProductLimits.Enabled) apr = BankRules.ClampRate(false, true, apr);
        }

        // Corporate facilities are negotiated, not made from a product. Offers are clamped before the mod
        // assesses them; preferred deposits are the bank's own cost and left alone.
        static void NegotiatePrefix(object __instance, string caseId, ref decimal rate, ref int termMonths, ref decimal feeRate)
        {
            if (!Rules.BankProductLimits.Enabled) return;
            try
            {
                var cases = casesProperty.GetValue(__instance, null) as IEnumerable;
                if (cases == null) return;
                foreach (var item in cases)
                {
                    if (item == null || (string)caseIdProperty.GetValue(item, null) != caseId) continue;
                    string type = caseTypeProperty.GetValue(item, null).ToString();
                    if (type == "PreferredDeposit") return;
                    rate = BankRules.ClampRate(true, false, rate);
                    feeRate = BankRules.ClampFee(feeRate);
                    if (type == "CorporateCreditFacility") termMonths = BankRules.ClampTerm(termMonths);
                    return;
                }
            }
            catch (Exception ex) { Log.Error("Empire Bank: negotiation offer not checked: " + ex); }
        }

        static void NormalizeProducts(object runtime)
        {
            var products = productsProperty.GetValue(runtime, null) as IEnumerable;
            if (products == null) return;
            int changed = 0;
            foreach (var product in products)
            {
                if (product == null) continue;
                string kind = KindOf(product);
                if (kind != "Loan" && kind != "CreditCard") continue;
                decimal rate = (decimal)rateProperty.GetValue(product, null);
                decimal cappedRate = BankRules.ClampRate(kind == "Loan", kind == "CreditCard", rate);
                bool touched = cappedRate != rate;
                if (touched) rateProperty.SetValue(product, cappedRate, null);
                if (kind == "Loan")
                {
                    decimal fee = (decimal)feeProperty.GetValue(product, null);
                    int term = (int)termProperty.GetValue(product, null);
                    if (BankRules.ClampFee(fee) != fee) { feeProperty.SetValue(product, BankRules.ClampFee(fee), null); touched = true; }
                    if (BankRules.ClampTerm(term) != term) { termProperty.SetValue(product, BankRules.ClampTerm(term), null); touched = true; }
                }
                if (touched) changed++;
            }
            if (changed > 0) Log.Info("Empire Bank: brought " + changed + " products within the limits.");
        }

        static object FindProduct(object runtime, string productId)
        {
            var products = productsProperty.GetValue(runtime, null) as IEnumerable;
            if (products == null) return null;
            foreach (var product in products)
                if (product != null && (string)productIdProperty.GetValue(product, null) == productId) return product;
            return null;
        }

        static string KindOf(object product)
        {
            return product == null ? null : productTypeProperty.GetValue(product, null).ToString();
        }

        static bool RemotePrefix()
        {
            return !Rules.BankNoRemoteRequests.Enabled;
        }

        // An empty coroutine in place of the request, so the callers' StartCoroutine still gets one.
        static bool StatsPrefix(ref IEnumerator __result)
        {
            if (!Rules.BankNoRemoteRequests.Enabled) return true;
            __result = new object[0].GetEnumerator();
            return false;
        }
    }
}
