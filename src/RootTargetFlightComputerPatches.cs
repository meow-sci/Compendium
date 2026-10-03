using Brutal.Numerics;
using HarmonyLib;
using KSA;
using System.Reflection;
using System.Reflection.Emit;

namespace Compendium
{
    [HarmonyPatch]
    internal static class RootTargetPresencePatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(GaugeCanvas), nameof(GaugeCanvas.IsContextVisible));
            yield return AccessTools.Method(typeof(GaugeButtonNavBallMode), nameof(GaugeButtonNavBallMode.IsDisabled));
            yield return AccessTools.Method(typeof(GaugeButtonNavBallMode), nameof(GaugeButtonNavBallMode.OnReleased));
            yield return AccessTools.Method(typeof(Vehicle), "UpdateNavballData");
        }

        internal static object? GetNavigationTarget(Vehicle vehicle)
        {
            return (object?)vehicle.Target ?? RootNavigationTargets.GetTarget(vehicle);
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> IncludeRootPresence(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = instructions.ToList();
            var getter = AccessTools.PropertyGetter(typeof(Vehicle), nameof(Vehicle.Target));
            var replacement = AccessTools.Method(typeof(RootTargetPresencePatches), nameof(GetNavigationTarget));
            int replacements = 0;
            for (int i = 0; i < code.Count - 1; i++)
            {
                if (!code[i].Calls(getter))
                {
                    continue;
                }
                var next = code[i + 1].opcode;
                // Only replace null tests; actual IOrbiter consumers retain the native target.
                if (next == OpCodes.Brtrue || next == OpCodes.Brtrue_S ||
                    next == OpCodes.Brfalse || next == OpCodes.Brfalse_S ||
                    (next == OpCodes.Ldnull && i + 2 < code.Count &&
                     (code[i + 2].opcode == OpCodes.Ceq || code[i + 2].opcode == OpCodes.Cgt_Un)))
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = replacement;
                    replacements++;
                }
            }
            if (replacements != 1)
            {
                throw new InvalidOperationException($"Compendium expected one target presence check in {__originalMethod.Name}, found {replacements}.");
            }
            return code;
        }
    }

    [HarmonyPatch]
    internal static class RootTargetFlightComputerPatches
    {
        [HarmonyPatch(typeof(Vehicle), "UpdateNavballData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> AddNavballFrameTarget(IEnumerable<CodeInstruction> instructions)
        {
            return RootTargetPatches.AddMarkerTarget(instructions);
        }

        [HarmonyPatch(typeof(Vehicle), "IsDisabled", new[] { typeof(VehicleReferenceFrame) })]
        [HarmonyPostfix]
        private static void EnableRootFrame(Vehicle __instance, VehicleReferenceFrame frame, ref bool __result)
        {
            if (frame == VehicleReferenceFrame.Dock && RootNavigationTargets.GetTarget(__instance) != null)
            {
                __result = !__instance.IsControllable;
            }
        }

        [HarmonyPatch(typeof(Vehicle), "IsDisabled", new[] { typeof(FlightComputerAttitudeTrackTarget) })]
        [HarmonyPostfix]
        private static void EnableRootTracking(Vehicle __instance, FlightComputerAttitudeTrackTarget trackTarget, ref bool __result)
        {
            if (trackTarget is FlightComputerAttitudeTrackTarget.Toward or FlightComputerAttitudeTrackTarget.Away &&
                RootNavigationTargets.GetTarget(__instance) != null)
            {
                __result = !__instance.IsControllable;
            }
        }

        [HarmonyPatch(typeof(Vehicle), "Hovered", new[] { typeof(FlightComputerAttitudeTrackTarget) })]
        [HarmonyPrefix]
        private static bool RootTrackingTooltip(Vehicle __instance, FlightComputerAttitudeTrackTarget trackTarget)
        {
            return !__instance.IsControllable || RootNavigationTargets.GetTarget(__instance) == null ||
                trackTarget is not (FlightComputerAttitudeTrackTarget.Toward or FlightComputerAttitudeTrackTarget.Away);
        }

        [HarmonyPatch(typeof(Program), "UpdateGauges")]
        [HarmonyPostfix]
        private static void RootTargetReadouts(Vehicle vehicle)
        {
            var root = RootNavigationTargets.GetTarget(vehicle);
            if (root == null)
            {
                return;
            }
            const UnitMask units = UnitMask.Millimeter | UnitMask.Centimeter | UnitMask.Meter |
                UnitMask.Kilometer | UnitMask.Gigameter | UnitMask.AU | UnitMask.LightYear;
            Program.TgtRelDistanceRoller.IsDisabled = false;
            Program.TgtRelDistanceRoller.PackTarget((root.GetPositionEcl() - vehicle.GetPositionEcl()).Length(), 1, units);
            Program.TgtRelVelocityRoller.IsDisabled = false;
            Program.TgtRelVelocityRoller.PackTarget((root.GetVelocityEcl() - vehicle.GetVelocityEcl()).Length(), 1, units);
            var approach = RootClosestApproach.Get(vehicle, root, Universe.GetElapsedTime());
            Program.TgtClosestDistanceRoller.IsDisabled = !approach.HasValue;
            Program.TgtClosestTimeRoller.IsDisabled = !approach.HasValue;
            if (approach.HasValue)
            {
                Program.TgtClosestDistanceRoller.PackTarget(approach.Value.Distance, 1, units);
                Program.TgtClosestTimeRoller.SetTimeTarget(Math.Max(0, (approach.Value.Time - Universe.GetElapsedTime()).Seconds()));
            }
        }
    }
}
