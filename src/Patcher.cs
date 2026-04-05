using HarmonyLib;
using KSA;

namespace Compendium
{
    [HarmonyPatch]
    internal static class Patcher
    {
        private static Harmony? _harmony = new Harmony("Compendium");

        public static void Patch()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("Compendium");
            }

            Console.WriteLine("Patching Compendium...");
            _harmony.PatchAll(typeof(Patcher).Assembly);
        }

        public static void Unload()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
            }
        }

        [HarmonyPatch(typeof(ModLibrary), nameof(ModLibrary.LoadAll))]
        [HarmonyPostfix]
        public static void AfterLoad()
        {
            Console.WriteLine("ModLibrary.LoadAll patched by Compendium.");
        }

        [HarmonyPatch(typeof(Astronomical), nameof(Astronomical.ShouldDrawLines), new[] { typeof(Astronomical), typeof(Viewport), typeof(Orbit) })]
        [HarmonyPostfix]
        public static void ShouldDrawLinesPostfix(Astronomical astronomical, ref bool __result)
        {
            if (!Compendium.TryGetOrbitVisibilityOverride(astronomical, out OrbitVisibilityMode mode))
            {
                return;
            }

            switch (mode)
            {
                case OrbitVisibilityMode.Always:
                    if (astronomical.OrbitLineMode != OrbitUiMode.Never)
                    {
                        __result = true;
                    }
                    break;
                case OrbitVisibilityMode.Off:
                    __result = false;
                    break;
            }
        }
    }
}