using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;
using System.Numerics;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{  
    public partial class Compendium
    {

        // Makes a dictionary of predetermined colors for orbit lines
        public static Dictionary<string, float3> orbitLineColors = new Dictionary<string, float3>
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
        public void CollectAllCelestials(Astronomical astro, List<Celestial> collection)
        {
            if (astro is Celestial cel && !collection.Contains(cel))
            {
                collection.Add(cel);
            }
            foreach (var child in astro.Children)
            {
                CollectAllCelestials(child, collection);
            }
        }

        public static Celestial? FindCelestialById(Astronomical? body, string id)
        {
            if (body == null) return null;
            if (body is Celestial cel && cel.Id == id)
            {
                return cel;
            }
            foreach (var child in body.Children)
            {
                var found = FindCelestialById(child, id);
                if (found != null) return found;
            }
            return null;
        }

        public static void ToggleCategoryOrbits(string categoryKey, bool showOrbit)
        {
            if (buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey)) return;
            
            var systemName = Universe.CurrentSystem?.Id ?? "Dummy";
            var categoryTreeData = buttonsCatsTree[categoryKey];
            foreach (var parentEntry in categoryTreeData)
            {
                // Skip the "Data" entry which contains CompendiumData for category description
                if (parentEntry.Key == "Data") continue;
                
                // Toggle parent orbit
                var parentCelestial = FindCelestialById(Universe.WorldSun, parentEntry.Key);
                if (parentCelestial != null)
                {
                    // Toggle orbit visibility
                    parentCelestial.ShowOrbit = showOrbit;
                    
                    // Try to get the body's data from bodyJsonDict with proper key format
                    CompendiumData? parentData = null;
                    if (!bodyJsonDict.TryGetValue($"{systemName}.{parentCelestial.Id}", out parentData))
                    {
                        bodyJsonDict.TryGetValue($"Compendium.{parentCelestial.Id}", out parentData);
                    }
                    
                    // Set DrawnUiBox based on data or default
                    parentCelestial.DrawnUiBox = showOrbit ? true : (parentData?.DrawnUiBox ?? false);
                }
                
                // Toggle children orbits
                var parentEntryData = (Dictionary<string, object>)parentEntry.Value;
                var childrenIds = (List<string>)parentEntryData["Children"];
                foreach (var childId in childrenIds)
                {
                    var childCelestial = FindCelestialById(Universe.WorldSun, childId);
                    if (childCelestial != null)
                    {
                        // Toggle orbit visibility
                        childCelestial.ShowOrbit = showOrbit;
                        
                        // Try to get the child's data from bodyJsonDict with proper key format
                        CompendiumData? childData = null;
                        if (!bodyJsonDict.TryGetValue($"{systemName}.{childCelestial.Id}", out childData))
                        {
                            bodyJsonDict.TryGetValue($"Compendium.{childCelestial.Id}", out childData);
                        }
                        
                        // Set DrawnUiBox based on data or default
                        childCelestial.DrawnUiBox = showOrbit ? true : (childData?.DrawnUiBox ?? false);
                    }
                }
            }
        }

        private static int fontPushCount = 0;

        public void PushTheFont(float scale)
        {
            if (fontNames.Length > 0 && selectedFontIndex < fontNames.Length && loadedFonts.TryGetValue(fontNames[selectedFontIndex], out ImFontPtr value))
            {
                ImGui.PushFont(value, fontSizeCurrent * scale);
                fontPushCount++;
            }
            // If no custom font is available, ImGui will use the default font automatically
        }

        public void PopTheFont()
        {
            if (fontPushCount > 0)
            {
                ImGui.PopFont();
                fontPushCount--;
            }
        }
        
        public void BigIndent()
        {
            ImGui.Text("                 ");
            ImGui.SameLine();
        }
        public void PushTheColor(string color)
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

        public void DrawBoldSeparator(float thickness, Vector4 color)
        {
            ImGui.Dummy(new float2(-1, thickness)); // Add spacing/height to the layout
            float2 p_min = ImGui.GetCursorScreenPos();
            float2 p_max = new float2(p_min.X + ImGui.GetContentRegionAvail().X, p_min.Y + thickness);

            // Convert Vector4 to uint color (RGBA)
            uint colorU32 = (uint)((int)(color.W * 255) << 24 | (int)(color.Z * 255) << 16 | (int)(color.Y * 255) << 8 | (int)(color.X * 255));
            
            // Add the line to the window's draw list
            ImGui.GetWindowDrawList().AddRectFilled(p_min, p_max, colorU32);
        }

        public static string FormatMassWithUnit(double massInKg)
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