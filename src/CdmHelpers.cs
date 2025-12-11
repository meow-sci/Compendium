using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;
using System.Numerics;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{  
    public partial class Compendium
    {
        public void CollectAllCelestials(Astronomical astro, List<Celestial> collection)
        {
            foreach (var child in astro.Children)
            {
                if (child is Celestial cel)
                {
                    collection.Add(cel);
                    CollectAllCelestials(cel, collection);
                }
            }
        }

        public Celestial? FindCelestialById(Astronomical? root, string id)
        {
            if (root == null) return null;
            if (root is Celestial cel && cel.Id == id)
            {
                return cel;
            }
            foreach (var child in root.Children)
            {
                var found = FindCelestialById(child, id);
                if (found != null) return found;
            }
            return null;
        }

        public void ToggleCategoryOrbits(string categoryKey, bool showOrbit)
        {
            if (buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey)) return;
            
            var categoryTreeData = buttonsCatsTree[categoryKey];
            foreach (var parentEntry in categoryTreeData)
            {
                // Toggle parent orbit
                var parentCelestial = FindCelestialById(Universe.WorldSun, parentEntry.Key);
                if (parentCelestial != null)
                {
                    parentCelestial.ShowOrbit = showOrbit;
                }
                
                // Toggle children orbits
                var childrenIds = (List<string>)((Dictionary<string, object>)parentEntry.Value)["Children"];
                foreach (var childId in childrenIds)
                {
                    var childCelestial = FindCelestialById(Universe.WorldSun, childId);
                    if (childCelestial != null)
                    {
                        childCelestial.ShowOrbit = showOrbit;
                    }
                }
            }
        }

        public void PushTheFont(float scale)
        {
            if (fontNames.Length > 0 && selectedFontIndex < fontNames.Length && loadedFonts.TryGetValue(fontNames[selectedFontIndex], out ImFontPtr value))
            {
                ImGui.PushFont(value, fontSizeCurrent * scale);
            }
            // If no custom font is available, ImGui will use the default font automatically
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

        public string FormatMassWithUnit(double massInKg)
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