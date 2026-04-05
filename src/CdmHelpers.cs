using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;
using System.Numerics;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    internal enum OrbitVisibilityMode
    {
        Always,
        On,
        Off
    }

    public partial class Compendium
    {

        // Makes a dictionary of predetermined colors for orbit lines
        private static Dictionary<string, float3> orbitLineColors = new Dictionary<string, float3>
        {
            { "White", new float3(1.0f, 1.0f, 1.0f) },
            { "Pink", new float3(1.0f, 0.75f, 0.8f) },
            { "Red", new float3(1.0f, 0.0f, 0.0f) },
            { "Orange", new float3(1.0f, 0.65f, 0.0f) },
            { "Yellow", new float3(1.0f, 1.0f, 0.0f) },
            { "Green", new float3(0.0f, 1.0f, 0.0f) },
            { "Cyan", new float3(0.0f, 1.0f, 1.0f) },
            { "Blue", new float3(0.0f, 0.0f, 1.0f) },
            { "Magenta", new float3(1.0f, 0.0f, 1.0f) },
            { "Purple", new float3(0.5f, 0.0f, 0.5f) },
            { "Black", new float3(0.0f, 0.0f, 0.0f) },
        };

        private static readonly Dictionary<string, OrbitVisibilityMode> orbitVisibilityOverrides = new();

        private static string GetCelestialPath(Celestial celestial)
        {
            var pathSegments = new Stack<string>();
            Astronomical? current = celestial;

            while (current is Celestial currentCelestial)
            {
                pathSegments.Push(currentCelestial.Id);
                current = currentCelestial.Parent as Astronomical;
            }

            return string.Join(".", pathSegments);
        }

        private static string GetDisplayBodyId(string bodyKey)
        {
            if (string.IsNullOrWhiteSpace(bodyKey))
            {
                return bodyKey;
            }

            int dotIndex = bodyKey.LastIndexOf('.');
            return dotIndex >= 0 ? bodyKey[(dotIndex + 1)..] : bodyKey;
        }

        private static string GetOrbitVisibilityKey(Celestial celestial)
        {
            string currentSystem = Universe.CurrentSystem?.Id ?? "Dummy";
            return $"{currentSystem}.{GetCelestialPath(celestial)}";
        }

        private static IEnumerable<string> GetBodyJsonLookupKeys(Celestial celestial)
        {
            string currentSystem = Universe.CurrentSystem?.Id ?? "Dummy";
            string celestialPath = GetCelestialPath(celestial);

            yield return $"{currentSystem}.{celestialPath}";
            yield return $"Compendium.{celestialPath}";
            yield return $"{currentSystem}.{celestial.Id}";
            yield return $"Compendium.{celestial.Id}";
        }

        private static CompendiumData? GetBodyJsonData(Celestial celestial)
        {
            foreach (string key in GetBodyJsonLookupKeys(celestial))
            {
                if (bodyJsonDict.TryGetValue(key, out var data))
                {
                    return data;
                }
            }

            return null;
        }

        internal static bool TryGetOrbitVisibilityOverride(Astronomical astronomical, out OrbitVisibilityMode mode)
        {
            if (astronomical is Celestial celestial)
            {
                return orbitVisibilityOverrides.TryGetValue(GetOrbitVisibilityKey(celestial), out mode);
            }

            mode = OrbitVisibilityMode.On;
            return false;
        }

        private static OrbitVisibilityMode GetOrbitVisibilityMode(Celestial celestial)
        {
            if (orbitVisibilityOverrides.TryGetValue(GetOrbitVisibilityKey(celestial), out var mode))
            {
                return mode;
            }

            return celestial.ShowOrbit ? OrbitVisibilityMode.On : OrbitVisibilityMode.Off;
        }

        private static void SetOrbitVisibilityMode(Celestial celestial, OrbitVisibilityMode mode)
        {
            string key = GetOrbitVisibilityKey(celestial);

            if (mode == OrbitVisibilityMode.On)
            {
                orbitVisibilityOverrides.Remove(key);
            }
            else
            {
                orbitVisibilityOverrides[key] = mode;
            }

            celestial.ShowOrbit = mode != OrbitVisibilityMode.Off;
            CompendiumData? data = GetBodyJsonData(celestial);
            celestial.DrawnUiBox = mode == OrbitVisibilityMode.Off ? (data?.DrawnUiBox ?? false) : true;
        }

        private void DrawOrbitModeButtons(string idPrefix, OrbitVisibilityMode currentMode, Action<OrbitVisibilityMode> applyMode)
        {
            DrawOrbitModeButton($"< Always >##{idPrefix}_always", currentMode == OrbitVisibilityMode.Always, () => applyMode(OrbitVisibilityMode.Always));
            ImGui.SameLine();
            DrawOrbitModeButton($"< On >##{idPrefix}_on", currentMode == OrbitVisibilityMode.On, () => applyMode(OrbitVisibilityMode.On));
            ImGui.SameLine();
            DrawOrbitModeButton($"< Off >##{idPrefix}_off", currentMode == OrbitVisibilityMode.Off, () => applyMode(OrbitVisibilityMode.Off));
        }

        private void DrawOrbitModeButton(string label, bool selected, Action onClick)
        {
            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new float4(0.26f, 0.59f, 0.98f, 0.80f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new float4(0.26f, 0.59f, 0.98f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new float4(0.20f, 0.50f, 0.90f, 1.0f));
            }

            if (ImGui.Button(new ImString(label)))
            {
                onClick();
            }

            if (selected)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
            }
        }

        private static void CollectAllCelestials(Astronomical astro, List<Celestial> collection)
        {
            if (astro is Celestial cel && !collection.Contains(cel))
            {
                collection.Add(cel);
            }

            // KSA API change: Children moved to IParentBody, which Celestial and StellarBody implement.
            if (astro is IParentBody parentBody && parentBody.Children != null)
            {
                foreach (var child in parentBody.Children)
                {
                    if (child is Astronomical childAstro)
                    {
                        CollectAllCelestials(childAstro, collection);
                    }
                }
            }
        }

        private static Celestial? FindCelestialById(Astronomical? body, string id, string? parentId = null)
        {
            if (body == null) return null;
            if (body is Celestial cel && cel.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(parentId))
                {
                    return cel;
                }

                if (cel.Parent is Celestial parentCelestial && parentCelestial.Id.Equals(parentId, StringComparison.OrdinalIgnoreCase))
                {
                    return cel;
                }
            }

            if (body is IParentBody parentBody && parentBody.Children != null)
            {
                foreach (var child in parentBody.Children)
                {
                    if (child is Astronomical childAstro)
                    {
                        var found = FindCelestialById(childAstro, id, parentId);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private static Celestial? FindCelestialByKey(Astronomical? body, string bodyKey)
        {
            if (string.IsNullOrWhiteSpace(bodyKey))
            {
                return null;
            }

            string[] parts = bodyKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
            string id = parts.Length > 0 ? parts[^1] : bodyKey;
            string? parentId = parts.Length > 1 ? parts[^2] : null;
            return FindCelestialById(body, id, parentId);
        }

        private static void ToggleCategoryOrbits(string categoryKey, OrbitVisibilityMode mode)
        {
            if (buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey)) return;

            var categoryTreeData = buttonsCatsTree[categoryKey];
            foreach (var parentEntry in categoryTreeData)
            {
                // Skip the "Data" entry which contains CompendiumData for category description
                if (parentEntry.Key == "Data") continue;

                // Toggle parent orbit
                var parentCelestial = FindCelestialByKey(Universe.WorldSun, parentEntry.Key);
                if (parentCelestial != null)
                {
                    SetOrbitVisibilityMode(parentCelestial, mode);
                }

                // Toggle children orbits
                var parentEntryData = (Dictionary<string, object>)parentEntry.Value;
                var childrenIds = (List<string>)parentEntryData["Children"];
                foreach (var childId in childrenIds)
                {
                    var childCelestial = FindCelestialByKey(Universe.WorldSun, childId);
                    if (childCelestial != null)
                    {
                        SetOrbitVisibilityMode(childCelestial, mode);
                    }
                }
            }
        }

        private static void ToggleOrbitGroupOrbits(string categoryKey, string groupKey, OrbitVisibilityMode mode, string? parentBodyKey = null)
        {
            if (buttonsCatsTree == null || !buttonsCatsTree.TryGetValue(categoryKey, out var categoryTreeData))
            {
                return;
            }

            foreach (var parentEntry in categoryTreeData)
            {
                if (parentEntry.Key == "Data")
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(parentBodyKey) &&
                    !string.Equals(parentEntry.Key, parentBodyKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parentEntryData = (Dictionary<string, object>)parentEntry.Value;
                var childrenIds = (List<string>)parentEntryData["Children"];

                if (childrenIds.Count == 0)
                {
                    if (string.IsNullOrWhiteSpace(parentBodyKey))
                    {
                        var parentCelestial = FindCelestialByKey(Universe.WorldSun, parentEntry.Key);
                        CompendiumData? parentBodyJson = parentCelestial != null ? GetBodyJsonData(parentCelestial) : null;
                        if (parentCelestial != null && string.Equals(parentBodyJson?.OrbitLineGroup, groupKey, StringComparison.OrdinalIgnoreCase))
                        {
                            SetOrbitVisibilityMode(parentCelestial, mode);
                        }
                    }

                    continue;
                }

                foreach (var childId in childrenIds)
                {
                    var childCelestial = FindCelestialByKey(Universe.WorldSun, childId);
                    CompendiumData? childBodyJson = childCelestial != null ? GetBodyJsonData(childCelestial) : null;
                    if (childCelestial != null && string.Equals(childBodyJson?.OrbitLineGroup, groupKey, StringComparison.OrdinalIgnoreCase))
                    {
                        SetOrbitVisibilityMode(childCelestial, mode);
                    }
                }
            }
        }

        private static bool TryGetListGroupData(string listGroupKey, out CompendiumData? listGroupData)
        {
            listGroupData = null;
            CompendiumData? listGroupsContainer = null;
            string currentSystem = Universe.CurrentSystem?.Id ?? "Dummy";

            if (!bodyJsonDict.TryGetValue($"{currentSystem}.ListGroupsData", out listGroupsContainer))
            {
                bodyJsonDict.TryGetValue("Compendium.ListGroupsData", out listGroupsContainer);
            }

            if (listGroupsContainer?.ListGroupsData == null)
            {
                return false;
            }

            return listGroupsContainer.ListGroupsData.TryGetValue(listGroupKey, out listGroupData);
        }

        private static void ToggleAllOrbitLines(OrbitVisibilityMode mode)
        {
            if (Universe.WorldSun == null) return;
            var allCelestials = new List<Celestial>();
            CollectAllCelestials(Universe.WorldSun, allCelestials);

            foreach (var celestial in allCelestials)
            {
                SetOrbitVisibilityMode(celestial, mode);
            }
        }

        private static int fontPushCount = 0;

        private void PushTheFont(float scale)
        {
            if (fontNames.Length > 0 && selectedFontIndex < fontNames.Length && loadedFonts.TryGetValue(fontNames[selectedFontIndex], out ImFontPtr value))
            {
                ImGui.PushFont(value, fontSizeCurrent * scale);
                fontPushCount++;
            }
            // If no custom font is available, ImGui will use the default font automatically
        }

        private void PopTheFont()
        {
            if (fontPushCount > 0)
            {
                ImGui.PopFont();
                fontPushCount--;
            }
        }
        
        private void BigIndent()
        {
            ImGui.Text("                 ");
            ImGui.SameLine();
        }
        private void PushTheColor(string color)
        {
            if (color == "ltblue")
            { ImGui.PushStyleColor(ImGuiCol.Header, new float4(0.26f, 0.59f, 0.98f, 0.80f)); }
            if (color == "blackstuff")
            {
                ImGui.PushStyleColor(ImGuiCol.Header, new float4(0.0f, 0.0f, 0.0f, 0.0f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new float4(0.26f, 0.59f, 0.98f, 0.80f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new float4(0.26f, 0.59f, 0.98f, 1.0f));
            }
        }

        private void DrawBoldSeparator(float thickness, Vector4 color)
        {
            ImGui.Dummy(new float2(-1, thickness)); // Add spacing/height to the layout
            float2 p_min = ImGui.GetCursorScreenPos();
            float2 p_max = new float2(p_min.X + ImGui.GetContentRegionAvail().X, p_min.Y + thickness);

            // Convert Vector4 to uint color (RGBA)
            uint colorU32 = (uint)((int)(color.W * 255) << 24 | (int)(color.Z * 255) << 16 | (int)(color.Y * 255) << 8 | (int)(color.X * 255));
            
            // Add the line to the window's draw list
            ImGui.GetWindowDrawList().AddRectFilled(p_min, p_max, colorU32);
        }

        private static string FormatMassWithUnit(double massInKg)
        {
            // Convert mass to appropriate order of magnitude unit
            // Based on https://en.wikipedia.org/wiki/Orders_of_magnitude_(mass)

            if (massInKg >= 1e30) // Yottagram (Yg)
                return $"{massInKg / 1e30:F3} Yg";
            else if (massInKg >= 1e27) // Zettagram (Zg)
                return $"{massInKg / 1e27:F3} Zg";
            else if (massInKg >= 1e24) // Yottagram (Yg)
                return $"{massInKg / 1e24:F3} Yg";
            else if (massInKg >= 1e21) // Zettagram (Zg)
                return $"{massInKg / 1e21:F3} Zg";
            else if (massInKg >= 1e18) // Exagram (Eg)
                return $"{massInKg / 1e18:F3} Eg";
            else if (massInKg >= 1e15) // Petagram (Pg)
                return $"{massInKg / 1e15:F3} Pg";
            else if (massInKg >= 1e12) // Teragram (Tg)
                return $"{massInKg / 1e12:F3} Tg";
            else if (massInKg >= 1e9) // Gigagram (Gg)
                return $"{massInKg / 1e9:F3} Gg";
            else if (massInKg >= 1e6) // Megagram (Mg) / tonne
                return $"{massInKg / 1e6:F3} Mg";
            else if (massInKg >= 1e3) // Kilogram (kg)
                return $"{massInKg / 1e3:F3} Mg";
            else if (massInKg >= 1) // Kilogram (kg)
                return $"{massInKg:F3} kg";
            else // Very small masses
                return $"{massInKg:E3} kg";
        }

    }
}