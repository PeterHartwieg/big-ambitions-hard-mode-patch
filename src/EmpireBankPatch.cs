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
    // - The emergency card borrows $10M from a central bank per click, without limit. The click is ignored.
    // - Applicant credit scores are drawn, then raised to the product's minimum (Math.Max), so a minimum of
    //   850 makes every applicant perfect. The Math.Max after BellCredit is replaced so the drawn score stays.
    // - Loan products allow 50% APR, a 10% fee and a one-month term. Edits beyond the limits are refused
    //   and rate nudges stop at the cap.
    // - On every save load and Settings visit it fetches /v1/stats and supporters.json from the author's
    //   servers without asking. Both fetches are skipped. The opt-in heartbeat is left to the player.
    public sealed class EmpireBankPatch : ITargetPatch
    {
        const int Gateway = 0, Emergency = 1, LoanApplicants = 2, CardApplicants = 3, UpdateLoan = 4, UpdateProduct = 5, AdjustRate = 6, Stats = 7, Supporters = 8;
        const string WithdrawalReason = "withdraw";
        static PropertyInfo productsProperty, productIdProperty, productTypeProperty, rateProperty;

        public TargetInfo Target { get { return Targets.EmpireBank; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var hooks = new MethodBase[9];
            var gateway = target.GetType("EmpireBank.Integration.BigAmbitionsPlayerEconomyGateway", false);
            var card = target.GetType("EmpireBank.UI.EmpireBankEmergencyCard", false);
            var loans = target.GetType("EmpireBank.Core.LoanApplicationService", false);
            var cards = target.GetType("EmpireBank.Core.CreditCardApplicationService", false);
            var runtime = target.GetType("EmpireBank.Core.EmpireBankRuntime", false);
            var product = target.GetType("EmpireBank.Models.BankProduct", false);
            var telemetry = target.GetType("EmpireTelemetry.EmpireTelemetryClient", false);
            var supporters = target.GetType("EmpireBank.UI.EmpireBankSupportersView", false);

            if (gateway != null) hooks[Gateway] = Declared(gateway, "TryChangePlayerMoney", typeof(decimal), typeof(string));
            if (card != null) hooks[Emergency] = Declared(card, "OnRequest");
            if (loans != null) hooks[LoanApplicants] = WithScoreClamp(loans, "Create");
            if (cards != null) hooks[CardApplicants] = WithScoreClamp(cards, "CreateApplication");
            if (runtime != null && product != null && ReadProductMembers(runtime, product))
            {
                hooks[UpdateLoan] = AccessTools.GetDeclaredMethods(runtime).FirstOrDefault(m => m.Name == "UpdateLoanProduct" && HasParameters(m, "productId", "apr", "termMonths", "feeRate", "message"));
                hooks[UpdateProduct] = AccessTools.GetDeclaredMethods(runtime).FirstOrDefault(m => m.Name == "UpdateProduct" && HasParameters(m, "productId", "rate", "termMonths", "feeValue", "message"));
                hooks[AdjustRate] = Declared(runtime, "AdjustProductRate", typeof(string), typeof(decimal));
            }
            if (telemetry != null) hooks[Stats] = Declared(telemetry, "FetchStatsIfStale");
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
            return productsProperty != null && productIdProperty != null && productTypeProperty != null && rateProperty != null
                && productTypeProperty.PropertyType.IsEnum && rateProperty.PropertyType == typeof(decimal) && rateProperty.CanWrite;
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            var self = typeof(EmpireBankPatch);
            harmony.Patch(hooks[Gateway], prefix: new HarmonyMethod(self, "MoneyPrefix"));
            harmony.Patch(hooks[Emergency], prefix: new HarmonyMethod(self, "EmergencyPrefix"));
            harmony.Patch(hooks[LoanApplicants], transpiler: new HarmonyMethod(self, "ScoreTranspiler"));
            harmony.Patch(hooks[CardApplicants], transpiler: new HarmonyMethod(self, "ScoreTranspiler"));
            harmony.Patch(hooks[UpdateLoan], prefix: new HarmonyMethod(self, "UpdateLoanPrefix"));
            harmony.Patch(hooks[UpdateProduct], prefix: new HarmonyMethod(self, "UpdateProductPrefix"));
            harmony.Patch(hooks[AdjustRate], postfix: new HarmonyMethod(self, "AdjustRatePostfix"));
            harmony.Patch(hooks[Stats], prefix: new HarmonyMethod(self, "RemotePrefix"));
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

        static bool EmergencyPrefix()
        {
            if (!Rules.BankNoEmergencyBorrowing.Enabled) return true;
            try { Notifications.ShowError("hardmode_bank_emergency_blocked"); }
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

        static void AdjustRatePostfix(object __instance, string productId)
        {
            if (!Rules.BankProductLimits.Enabled) return;
            try
            {
                var product = FindProduct(__instance, productId);
                if (product == null) return;
                string kind = KindOf(product);
                decimal rate = (decimal)rateProperty.GetValue(product, null);
                decimal capped = BankRules.ClampRate(kind == "Loan", kind == "CreditCard", rate);
                if (capped != rate) rateProperty.SetValue(product, capped, null);
            }
            catch (Exception ex) { Log.Error("Empire Bank: rate change not checked: " + ex); }
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
    }
}
