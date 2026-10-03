using HarmonyLib;
using KSA;
using System.Reflection.Emit;

namespace Compendium
{
    [HarmonyPatch]
    internal static class RootTargetPatches
    {
        [HarmonyPatch(typeof(InputEvents), nameof(InputEvents.ApplyInputEvents))]
        [HarmonyPrefix]
        private static void ApplyRootTargetRequests()
        {
            RootNavigationTargets.ApplyRequests();
        }

        [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.SetTarget))]
        [HarmonyPrefix]
        private static void ChangingNativeTarget(Vehicle __instance, IOrbiter? target, Part? targetPart)
        {
            if (target != __instance && targetPart?.Tree?.OwningVehicle != __instance)
            {
                RootNavigationTargets.Clear(__instance, stopTracking: target == null);
            }
        }

        [HarmonyPatch(typeof(Universe), nameof(Universe.UnsetTargetCommand))]
        [HarmonyPrefix]
        private static bool UnsetRootTarget(string celestialId)
        {
            if (Universe.CurrentSystem?.Get(celestialId) is Vehicle vehicle && RootNavigationTargets.GetTarget(vehicle) != null)
            {
                RootNavigationTargets.QueueSelection(vehicle, null);
                Console.WriteLine($"Compendium: navigation target clear requested for '{celestialId}'.");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FlightComputer), nameof(FlightComputer.ComputeControl))]
        [HarmonyPrefix]
        private static void AddControlTarget(FlightComputer __instance, ref FlightComputerNavigation nav, out bool __state)
        {
            __state = false;
            if (nav.Target == null)
            {
                RootNavigationTargets.ApplyControlTarget(__instance, ref nav);
                __state = nav.Target.HasValue;
            }
        }

        [HarmonyPatch(typeof(FlightComputer), nameof(FlightComputer.ComputeControl))]
        [HarmonyFinalizer]
        private static void RestoreControlTarget(ref FlightComputerNavigation nav, bool __state)
        {
            if (__state)
            {
                nav.Target = null;
            }
        }

        [HarmonyPatch(typeof(Vehicle), "UpdateNavBallMarkers")]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> AddMarkerTarget(IEnumerable<CodeInstruction> instructions)
        {
            var create = AccessTools.Method(typeof(NavigationTarget), nameof(NavigationTarget.Create));
            var replacement = AccessTools.Method(typeof(RootNavigationTargets), nameof(RootNavigationTargets.CreateMarkerTarget));
            int replacements = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(create))
                {
                    // Preserve branch targets on the first instruction of the replacement.
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    replacements++;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (replacements != 1)
            {
                throw new InvalidOperationException($"Compendium expected one navball target call, found {replacements}.");
            }
        }
    }
}
