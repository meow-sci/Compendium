using Brutal.Numerics;
using System.Globalization;

namespace Compendium
{
    public partial class Compendium
    {
        private const string SettingsFileName = "settings.ini";

        // Open/closed state last written to settings.ini; ImGui's imgui.ini only keeps position/size, not visibility.
        private static bool savedCompendiumWindow = true;

        private static string? GetSettingsFilePath()
        {
            return string.IsNullOrWhiteSpace(dllDir) ? null : Path.Combine(dllDir, SettingsFileName);
        }

        private static void LoadSettings()
        {
            string? settingsPath = GetSettingsFilePath();
            if (settingsPath == null)
            {
                return;
            }

            if (!File.Exists(settingsPath))
            {
                SaveSettings();
                return;
            }

            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in File.ReadAllLines(settingsPath))
                {
                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        values[line[..equalsIndex].Trim()] = line[(equalsIndex + 1)..].Trim();
                    }
                }

                CompendiumWindow = ReadBool(values, "ShowWindow", CompendiumWindow);
                showBodyPointer = ReadBool(values, "ShowPointer", showBodyPointer);
                pointerDualLines = ReadBool(values, "PointerDualLines", pointerDualLines);
                pointerRainbow = ReadBool(values, "PointerRainbow", pointerRainbow);
                pointerColor = new float3(
                    ReadFloat(values, "PointerColorR", pointerColor.X),
                    ReadFloat(values, "PointerColorG", pointerColor.Y),
                    ReadFloat(values, "PointerColorB", pointerColor.Z));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compendium: Failed to read {settingsPath}: {ex.Message}");
            }

            savedCompendiumWindow = CompendiumWindow;
        }

        private static void SaveSettings()
        {
            string? settingsPath = GetSettingsFilePath();
            if (settingsPath == null)
            {
                return;
            }

            savedCompendiumWindow = CompendiumWindow;
            try
            {
                File.WriteAllLines(settingsPath, new[]
                {
                    "[Window]",
                    $"ShowWindow={CompendiumWindow.ToString(CultureInfo.InvariantCulture)}",
                    "[Pointer]",
                    $"ShowPointer={showBodyPointer.ToString(CultureInfo.InvariantCulture)}",
                    $"PointerDualLines={pointerDualLines.ToString(CultureInfo.InvariantCulture)}",
                    $"PointerRainbow={pointerRainbow.ToString(CultureInfo.InvariantCulture)}",
                    $"PointerColorR={pointerColor.X.ToString(CultureInfo.InvariantCulture)}",
                    $"PointerColorG={pointerColor.Y.ToString(CultureInfo.InvariantCulture)}",
                    $"PointerColorB={pointerColor.Z.ToString(CultureInfo.InvariantCulture)}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compendium: Failed to write {settingsPath}: {ex.Message}");
            }
        }

        // Called every frame so both the mod menu toggle and the window's close (X) button get persisted.
        private static void PersistWindowOpenState()
        {
            if (CompendiumWindow != savedCompendiumWindow)
            {
                SaveSettings();
            }
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool fallback)
        {
            return values.TryGetValue(key, out var text) && bool.TryParse(text, out bool value) ? value : fallback;
        }

        private static float ReadFloat(Dictionary<string, string> values, string key, float fallback)
        {
            return values.TryGetValue(key, out var text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? Math.Clamp(value, 0f, 1f)
                : fallback;
        }
    }
}
