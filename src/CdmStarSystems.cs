using Brutal.ImGuiApi;
using KSA;
using System.Numerics;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    public partial class Compendium
    {
        private const string StarsCategoryKey = "Stars";
        private const double SolarMassKg = 1.98847e30;
        private const double SolarRadiusKm = 695700.0;
        private const double LightYearMeters = 9.4607304725808e15;

        private static Astronomical? selectedSystemRoot;
        private static Astronomical? categoriesBuiltForRoot;
        private static readonly ImInputString systemSearchBuffer = new ImInputString(128);

        // Roots are the parentless StellarBody / Barycenter entries, one per star system.
        private static List<Astronomical> GetSystemRoots()
        {
            var roots = new List<Astronomical>();
            foreach (var root in Universe.Roots)
            {
                if (root is Astronomical astronomical)
                {
                    roots.Add(astronomical);
                }
            }
            return roots;
        }

        private static Astronomical? GetRootOf(Astronomical? astronomical)
        {
            return astronomical is IParentBody parentBody ? IIndependentRoot.RootOf(parentBody) as Astronomical : null;
        }

        private static Astronomical? FindRootById(string? rootId)
        {
            if (string.IsNullOrWhiteSpace(rootId))
            {
                return null;
            }

            foreach (var root in Universe.Roots)
            {
                if (root is Astronomical astronomical && astronomical.Id.Equals(rootId, StringComparison.OrdinalIgnoreCase))
                {
                    return astronomical;
                }
            }
            return null;
        }

        private static Astronomical? GetSelectedRoot()
        {
            if (selectedSystemRoot != null && FindRootById(selectedSystemRoot.Id) == selectedSystemRoot)
            {
                return selectedSystemRoot;
            }

            selectedSystemRoot = GetRootOf(Universe.WorldSun) ?? Universe.WorldSun;
            return selectedSystemRoot;
        }

        private static void CollectSystemStars(Astronomical astronomical, List<Astronomical> stars)
        {
            if (astronomical is StellarBody || astronomical is Barycenter)
            {
                stars.Add(astronomical);
            }

            if (astronomical is IParentBody parentBody && parentBody.Children != null)
            {
                foreach (var child in parentBody.Children)
                {
                    if (child is StellarBody || child is Barycenter)
                    {
                        CollectSystemStars((Astronomical)child, stars);
                    }
                }
            }
        }

        private static Astronomical? FindSystemStar(string id)
        {
            var root = GetSelectedRoot();
            if (root == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var stars = new List<Astronomical>();
            CollectSystemStars(root, stars);
            return stars.FirstOrDefault(star => star.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        // Id of the star or barycenter a celestial ultimately orbits.
        private static string GetHostStarId(Celestial celestial)
        {
            IParentBody? current = celestial.Parent;
            while (current is Celestial parentCelestial)
            {
                current = parentCelestial.Parent;
            }
            return current is Astronomical host ? GetBodyDisplayName(host) : current?.Id ?? "Unknown";
        }

        private static string GetDefaultCategoryKey(List<string> keys)
        {
            if (keys.Contains("Planets")) return "Planets";
            if (keys.Contains(StarsCategoryKey)) return StarsCategoryKey;
            return keys.Count > 0 ? keys[0] : "None";
        }

        private static void SelectSystem(Astronomical root)
        {
            selectedSystemRoot = root;
            buttonsCatsTree = null;
            categoryKeys = new List<string>();
            showWindow = "None";
            selectedCategoryIndex = -1;
            selectedCategoryKey = "None";
            selectedCelestial = null;
            selectedCelestialId = "Compendium Astronomicals Database";
            selectedOrbitGroupKey = null;
            selectedOrbitGroupParentBodyKey = null;
        }

        private void DrawSystemSelector()
        {
            var currentRoot = GetSelectedRoot();
            if (currentRoot == null)
            {
                return;
            }

            DrawPointerToggle();
            DrawBoldSeparator(4.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            ImGui.Text(" ");
            ImGui.Text("Star System");
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.BeginCombo(new ImString("##StarSystemSelector"), new ImString(GetBodyDisplayName(currentRoot))))
            {
                if (ImGui.IsWindowAppearing())
                {
                    ImGui.SetKeyboardFocusHere();
                }
                ImGui.InputText(new ImString("Search##StarSystemSearch"), systemSearchBuffer);

                string search = systemSearchBuffer.Value;
                foreach (var root in GetSystemRoots().OrderBy(root => GetBodyDisplayName(root), StringComparer.OrdinalIgnoreCase))
                {
                    string rootName = GetBodyDisplayName(root);
                    if (search.Length > 0 && !rootName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool isSelected = root == currentRoot;
                    if (ImGui.Selectable(new ImString($"{rootName}##system_{root.Id}"), isSelected) && !isSelected)
                    {
                        SelectSystem(root);
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private void AddStarsCategory(Astronomical root)
        {
            if (buttonsCatsTree == null)
            {
                return;
            }

            var stars = new List<Astronomical>();
            CollectSystemStars(root, stars);
            if (stars.Count == 0)
            {
                return;
            }

            if (!buttonsCatsTree.TryGetValue(StarsCategoryKey, out var starsCategory))
            {
                starsCategory = new Dictionary<string, object>();
                buttonsCatsTree[StarsCategoryKey] = starsCategory;
            }

            foreach (var star in stars)
            {
                starsCategory[star.Id] = new Dictionary<string, object>
                {
                    ["Body"] = star.Id,
                    ["Children"] = new List<string>()
                };
            }

            if (TryGetListGroupData(StarsCategoryKey, out var starsData) && starsData != null)
            {
                starsCategory["Data"] = starsData;
            }
        }

        private void DrawStarInformation(Astronomical star)
        {
            ImGui.Separator();
            PushTheFont(1.7f);
            ImGui.Text(new ImString(GetBodyDisplayName(star)));
            PopTheFont();
            PushTheFont(1);

            string kind = star switch
            {
                Barycenter => "Barycenter (a virtual center of mass, not a physical object)",
                OrbitingStar orbitingStar => $"Star orbiting {(orbitingStar.Parent is Astronomical starParent ? GetBodyDisplayName(starParent) : orbitingStar.Parent.Id)}",
                _ => "Star"
            };
            ImGui.Text(new ImString(kind));

            CompendiumData? bodyJson = GetBodyJsonData(star);
            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.WikipediaUrl))
            {
                ImGui.TextLinkOpenURL(new ImString($"Wikipedia Page: {bodyJson.WikipediaUrl}"), new ImString($"https://en.wikipedia.org/wiki/{bodyJson.WikipediaUrl}"));
            }
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            ImGui.Text(" ");

            double mass = star switch
            {
                StellarBody stellarBody => stellarBody.Mass,
                Barycenter barycenter => barycenter.Mass,
                _ => 0.0
            };

            if (star is StellarBody)
            {
                double radiusKm = star.MeanRadius / 1000.0;
                ImGui.Text(new ImString($"Mean Radius: {radiusKm:N0} km ({radiusKm / SolarRadiusKm:F3} Suns)"));
                ImGui.Text(new ImString($"Mass: {mass:E3} Kg ({mass / SolarMassKg:F4} Suns)"));
            }
            else if (mass > 0)
            {
                ImGui.Text(new ImString($"Combined Mass of Barycentric Stars: {mass:E3} Kg ({mass / SolarMassKg:F4} Suns)"));
            }

            if (star is OrbitingStar orbiting && orbiting.Orbit != null)
            {
                double semiMajorAxisAu = orbiting.Orbit.SemiMajorAxis / 1.496e11;
                double periodYears = orbiting.Orbit.Period / 31536000.0;
                ImGui.Text(new ImString($"Semi-Major Axis: {semiMajorAxisAu:N2} AU"));
                ImGui.Text(new ImString($"Eccentricity: {orbiting.Orbit.Eccentricity:F4}"));
                ImGui.Text(new ImString($"Orbital Period: {periodYears:N2} years"));
            }

            var homeRoot = GetRootOf(Universe.CurrentSystem?.HomeBody as Astronomical);
            if (homeRoot != null && homeRoot != GetRootOf(star))
            {
                double distanceLy = (star.GetPositionEcl() - homeRoot.GetPositionEcl()).Length() / LightYearMeters;
                ImGui.Text(new ImString($"Distance from {GetBodyDisplayName(homeRoot)}: {distanceLy:N3} light years"));
            }

            ImGui.Text(" ");
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            ImGui.Text(" ");
            if (ImGui.Button(new ImString($"Focus Camera on {GetBodyDisplayName(star)}##focusStar")))
            {
                Universe.MoveCameraTo(star);
            }
            DrawTargetControls(star);
            ImGui.Text(" ");

            if (bodyJson != null)
            {
                DrawBodyJsonText(bodyJson);
            }

            PopTheFont();
        }

        private void DrawStarsCategoryInformation()
        {
            PushTheFont(1.9f);
            ImGui.Text(new ImString(StarsCategoryKey));
            PopTheFont();
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            PushTheFont(1.2f);
            ImGui.Text(" ");
            if (TryGetListGroupData(StarsCategoryKey, out var categoryData) && categoryData?.CatText != null)
            {
                ImGui.TextWrapped(categoryData.CatText);
                if (categoryData.CatWikipediaUrl != null)
                {
                    ImGui.Text(" ");
                    ImGui.Text("Wikipedia Url:");
                    ImGui.TextLinkOpenURL(new ImString($"https://en.wikipedia.org/wiki/{categoryData.CatWikipediaUrl}"));
                }
            }
            PopTheFont();
        }

        private void DrawBodyJsonText(CompendiumData bodyJson)
        {
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            ImGui.Text(" ");
            if (bodyJson.Text != null)
            {
                ImGui.TextWrapped(bodyJson.Text);
            }
            if (bodyJson.Factoids != null && bodyJson.Factoids.Count > 0)
            {
                ImGui.Text(" ");
                ImGui.SeparatorText("Factoids:"); ImGui.Text(" ");
                foreach (var factoid in bodyJson.Factoids)
                {
                    // Bullet character inline so wrapping works (BulletText does not wrap)
                    ImGui.TextWrapped(new ImString($"• {factoid}\n\n"));
                }
            }
            if (bodyJson.VisitedBy != null && bodyJson.VisitedBy.Count > 0)
            {
                ImGui.Text(" ");
                ImGui.SeparatorText("Visited By:"); ImGui.Text(" ");
                foreach (var visitor in bodyJson.VisitedBy)
                {
                    ImGui.BulletText(new ImString(visitor));
                }
                ImGui.Text(" ");
            }
            ImGui.Text(" ");
        }
    }
}
