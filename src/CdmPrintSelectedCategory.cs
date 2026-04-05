using Brutal.ImGuiApi;
using KSA;
using ImGui = Brutal.ImGuiApi.ImGui;

using System.Numerics;

namespace Compendium
{
    public partial class Compendium
    {
        // Add this field to store the selected celestial object
        private static object? selectedCelestial;

        private sealed class CategoryDisplayEntry
        {
            public string BodyKey { get; init; } = string.Empty;
            public string DisplayBodyId { get; init; } = string.Empty;
            public List<string> DirectChildren { get; } = new();
            public SortedDictionary<string, List<string>> GroupedChildren { get; } = new(StringComparer.OrdinalIgnoreCase);
            public bool HasChildren => DirectChildren.Count > 0 || GroupedChildren.Count > 0;
        }

        private sealed class CategoryDisplayCache
        {
            public List<CategoryDisplayEntry> Entries { get; } = new();
            public SortedDictionary<string, List<CategoryDisplayEntry>> GroupedEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
            public SortedDictionary<string, List<string>> GroupedLeafEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, CategoryDisplayCache>? categoryDisplayCacheByKey;

        public void PrintSelectedCategoryByKey(string categoryKey)
        {
            try
            {
                if (string.IsNullOrEmpty(categoryKey) || buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey))
                {
                    if (selectedCategoryKey != "Legend")
                    {
                        ImGui.Text("No data for this category");
                    }
                    return;
                }

                var cachedCategoryData = GetOrBuildCategoryDisplayCache(categoryKey);
                if (cachedCategoryData.Entries.Count == 0 && cachedCategoryData.GroupedEntries.Count == 0 && cachedCategoryData.GroupedLeafEntries.Count == 0)
                {
                    return;
                }

                foreach (var displayEntry in cachedCategoryData.Entries)
                {
                    DrawCategoryDisplayEntry(displayEntry);
                }

                var groupedCategoryKeys = new SortedSet<string>(cachedCategoryData.GroupedEntries.Keys, StringComparer.OrdinalIgnoreCase);
                foreach (var groupKey in cachedCategoryData.GroupedLeafEntries.Keys)
                {
                    groupedCategoryKeys.Add(groupKey);
                }

                foreach (var groupKey in groupedCategoryKeys)
                {
                    ImString groupLabel = new ImString($"      {groupKey}##leafgroup_{categoryKey}_{groupKey}");
                    if (ImGui.TreeNode(groupLabel))
                    {
                        DrawOrbitGroupSelectable($"          ({groupKey})##leafgroupinfo_{categoryKey}_{groupKey}", groupKey, null);

                        if (cachedCategoryData.GroupedEntries.TryGetValue(groupKey, out var groupedEntries))
                        {
                            foreach (var displayEntry in groupedEntries)
                            {
                                DrawCategoryDisplayEntry(displayEntry, nestedInSubgroup: true);
                            }
                        }

                        if (cachedCategoryData.GroupedLeafEntries.TryGetValue(groupKey, out var groupedLeafEntries))
                        {
                            foreach (var bodyId in groupedLeafEntries)
                            {
                                DrawCelestialSelectable($"          {GetDisplayBodyId(bodyId)}##grouped_{bodyId}", bodyId);
                            }
                        }

                        ImGui.TreePop();
                    }
                }
            }
            catch (Exception ex)
            {
                ImString errorMsg = new ImString($"Error: {ex.Message}");
                ImGui.Text(errorMsg);
            }
        }

        private void DrawCelestialSelectable(string label, string bodyKey)
        {
            ImString celestialId = new ImString(label);
            bool isSelected = selectedCelestialId == bodyKey;

            if (isSelected)
            {
                PushTheColor("ltblue");
            }

            if (ImGui.Selectable(celestialId, isSelected))
            {
                selectedCelestialId = bodyKey;
                selectedCelestial = FindCelestialByKey(Universe.WorldSun, bodyKey);
                selectedOrbitGroupKey = null;
                selectedOrbitGroupParentBodyKey = null;
                showWindow = "Celestial";
            }

            if (isSelected)
            {
                ImGui.PopStyleColor(1);
            }
        }

        private void DrawCategoryDisplayEntry(CategoryDisplayEntry displayEntry, bool nestedInSubgroup = false)
        {
            string headerPrefix = nestedInSubgroup ? "      " : string.Empty;
            string parentPrefix = nestedInSubgroup ? "               " : "         ";
            string childPrefix = nestedInSubgroup ? "               " : "          ";
            string subgroupPrefix = nestedInSubgroup ? "               " : "          ";
            string subgroupItemPrefix = nestedInSubgroup ? "                    " : "               ";
            string leafPrefix = nestedInSubgroup ? "          " : "      ";

            if (displayEntry.HasChildren)
            {
                PushTheColor("blackstuff");

                ImString headerLabel = new ImString($"{headerPrefix}{displayEntry.DisplayBodyId}##parentHeader_{displayEntry.BodyKey}");
                if (ImGui.CollapsingHeader(headerLabel))
                {
                    ImGui.Separator();
                    DrawCelestialSelectable($"{parentPrefix}({displayEntry.DisplayBodyId})##parent_{displayEntry.BodyKey}", displayEntry.BodyKey);

                    foreach (var childId in displayEntry.DirectChildren)
                    {
                        DrawCelestialSelectable($"{childPrefix}{GetDisplayBodyId(childId)}##child_{childId}", childId);
                    }

                    foreach (var group in displayEntry.GroupedChildren)
                    {
                        ImString groupLabel = new ImString($"{subgroupPrefix}{group.Key}##group_{displayEntry.BodyKey}_{group.Key}");
                        if (ImGui.TreeNode(groupLabel))
                        {
                            DrawOrbitGroupSelectable($"{subgroupItemPrefix}({group.Key})##groupinfo_{displayEntry.BodyKey}_{group.Key}", group.Key, displayEntry.BodyKey);

                            foreach (var childId in group.Value)
                            {
                                DrawCelestialSelectable($"{subgroupItemPrefix}{GetDisplayBodyId(childId)}##child_{childId}", childId);
                            }

                            ImGui.TreePop();
                        }
                    }
                }

                ImGui.Separator();
                ImGui.PopStyleColor(3);
            }
            else
            {
                DrawCelestialSelectable($"{leafPrefix}{displayEntry.DisplayBodyId}##celestial_{displayEntry.BodyKey}", displayEntry.BodyKey);
            }
        }

        private void DrawOrbitGroupSelectable(string label, string groupKey, string? parentBodyKey)
        {
            ImString groupSelectableLabel = new ImString(label);
            bool isSelected =
                showWindow == "Group" &&
                string.Equals(selectedOrbitGroupKey, groupKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(selectedOrbitGroupParentBodyKey ?? string.Empty, parentBodyKey ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            if (isSelected)
            {
                PushTheColor("ltblue");
            }

            if (ImGui.Selectable(groupSelectableLabel, isSelected))
            {
                SelectOrbitGroup(groupKey, parentBodyKey);
            }

            if (isSelected)
            {
                ImGui.PopStyleColor(1);
            }
        }

        private void SelectOrbitGroup(string groupKey, string? parentBodyKey)
        {
            selectedCelestial = null;
            selectedCelestialId = "Group";
            selectedOrbitGroupKey = groupKey;
            selectedOrbitGroupParentBodyKey = parentBodyKey;
            showWindow = "Group";
        }

        private List<string> GetCategorySubgroupKeys(string categoryKey)
        {
            var subgroupKeys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var cachedCategoryData = GetOrBuildCategoryDisplayCache(categoryKey);

            foreach (var subgroupKey in cachedCategoryData.GroupedEntries.Keys)
            {
                subgroupKeys.Add(subgroupKey);
            }

            foreach (var displayEntry in cachedCategoryData.Entries)
            {
                foreach (var subgroupKey in displayEntry.GroupedChildren.Keys)
                {
                    subgroupKeys.Add(subgroupKey);
                }
            }

            foreach (var subgroupKey in cachedCategoryData.GroupedLeafEntries.Keys)
            {
                subgroupKeys.Add(subgroupKey);
            }

            return subgroupKeys.ToList();
        }

        private CategoryDisplayCache GetOrBuildCategoryDisplayCache(string categoryKey)
        {
            categoryDisplayCacheByKey ??= new Dictionary<string, CategoryDisplayCache>(StringComparer.OrdinalIgnoreCase);
            if (categoryDisplayCacheByKey.TryGetValue(categoryKey, out var cachedCategoryData))
            {
                return cachedCategoryData;
            }

            cachedCategoryData = new CategoryDisplayCache();
            if (buttonsCatsTree == null || !buttonsCatsTree.TryGetValue(categoryKey, out var rawCategoryData))
            {
                categoryDisplayCacheByKey[categoryKey] = cachedCategoryData;
                return cachedCategoryData;
            }

            IDictionary<string, object> orderedCategoryData;
            if (categoryKey != "Planets" || (categoryKey == "Planets" && !rawCategoryData.ContainsKey("Mercury")))
            {
                orderedCategoryData = new SortedDictionary<string, object>(rawCategoryData, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                var tempDict = new Dictionary<string, object>
                {
                    { "Mercury", rawCategoryData["Mercury"] }
                };

                foreach (var kvp in rawCategoryData)
                {
                    if (kvp.Key == "Mercury")
                    {
                        continue;
                    }

                    tempDict.Add(kvp.Key, kvp.Value);
                }

                orderedCategoryData = tempDict;
            }

            bool groupLeafEntriesByOrbitGroup = categoryKey == "Asteroids" || categoryKey == "Comets";

            foreach (var parentEntry in orderedCategoryData)
            {
                string parentBodyId = parentEntry.Key;
                if (parentBodyId == "Data")
                {
                    continue;
                }

                var parentData = (Dictionary<string, object>)parentEntry.Value;
                var childrenIds = (List<string>)parentData["Children"];

                if (childrenIds.Count == 0 && groupLeafEntriesByOrbitGroup)
                {
                    string orbitGroup = GetOrbitGroupLabel(parentBodyId);
                    if (!cachedCategoryData.GroupedLeafEntries.ContainsKey(orbitGroup))
                    {
                        cachedCategoryData.GroupedLeafEntries[orbitGroup] = new List<string>();
                    }

                    cachedCategoryData.GroupedLeafEntries[orbitGroup].Add(parentBodyId);
                    continue;
                }

                var displayEntry = new CategoryDisplayEntry
                {
                    BodyKey = parentBodyId,
                    DisplayBodyId = GetDisplayBodyId(parentBodyId)
                };

                foreach (var childId in childrenIds.OrderBy(GetDisplayBodyId, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldSkipOrbitGroupDropdown(childId))
                    {
                        displayEntry.DirectChildren.Add(childId);
                        continue;
                    }

                    string orbitGroup = GetOrbitGroupLabel(childId);
                    if (!displayEntry.GroupedChildren.ContainsKey(orbitGroup))
                    {
                        displayEntry.GroupedChildren[orbitGroup] = new List<string>();
                    }

                    displayEntry.GroupedChildren[orbitGroup].Add(childId);
                }

                if (groupLeafEntriesByOrbitGroup)
                {
                    string orbitGroup = GetOrbitGroupLabel(parentBodyId);
                    if (!cachedCategoryData.GroupedEntries.ContainsKey(orbitGroup))
                    {
                        cachedCategoryData.GroupedEntries[orbitGroup] = new List<CategoryDisplayEntry>();
                    }

                    cachedCategoryData.GroupedEntries[orbitGroup].Add(displayEntry);
                }
                else
                {
                    cachedCategoryData.Entries.Add(displayEntry);
                }
            }

            foreach (var group in cachedCategoryData.GroupedLeafEntries.Values)
            {
                group.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(GetDisplayBodyId(left), GetDisplayBodyId(right)));
            }

            categoryDisplayCacheByKey[categoryKey] = cachedCategoryData;
            return cachedCategoryData;
        }

        private string GetOrbitGroupLabel(string bodyKey)
        {
            var celestial = FindCelestialByKey(Universe.WorldSun, bodyKey);
            CompendiumData? bodyData = celestial != null ? GetBodyJsonData(celestial) : null;

            if (!string.IsNullOrWhiteSpace(bodyData?.OrbitLineGroup))
            {
                return bodyData.OrbitLineGroup;
            }

            return "Other";
        }

        private bool ShouldSkipOrbitGroupDropdown(string bodyKey)
        {
            var celestial = FindCelestialByKey(Universe.WorldSun, bodyKey);
            CompendiumData? bodyData = celestial != null ? GetBodyJsonData(celestial) : null;

            return bodyData?.ListGroups?.Any(group => string.Equals(group, "Moons", StringComparison.OrdinalIgnoreCase)) == true;
        }

        public void PrintTermsCategory()
        {
            PushTheFont(1.7f);
            ImGui.TextWrapped("Terms and Definitions:\n");
            PopTheFont();
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
            ImGui.Text("");
            PushTheFont(1.0f);



            // Unfortunately, bullets don't do text wrapping, so do a bullet but then sameline and text for the actual content
            ImGui.Text("ATMOSPHERE: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("Indicates whether the celestial body has an atmosphere.  Some celestial bodies may have very thin atmospheres, while others may have thick, dense atmospheres.\n\n");
            ImGui.Text("AXIAL TILT: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The angle of tilt from it's rotational axis to either the KSA solar reference plane or the equatorial plane of the body which the orbit is around. A higher tilt results in more extreme seasons over it's orbital period, because different hemispheres receive more or less sunlight at different times of the year.  ** Real axial tilt values are defined as the angle between the body's rotational axis and the perpendicular to its orbital plane. **\n\n");
            ImGui.Text("CURRENT SPEED: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The current velocity of the celestial body in its orbit around its parent body.  In a perfectly circular orbit, this speed remains constant.  In non-circular orbits, the speed varies - being fastest at periapsis (closest approach) and slowest at apoapsis (farthest distance).\n\n");
            ImGui.Text("ECCENTRICITY: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("A measure of how much an orbit deviates from a perfect circle. An eccentricity of 0 indicates a perfectly circular orbit, while values closer to 1 indicate more elongated orbits.  Eccentricity values greater than 1 indicate hyperbolic trajectories - meaning the object is not gravitationally bound to the central body and is on an escape trajectory.\n\n");
            ImGui.Text("ESCAPE VELOCITY: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped(" The minimum speed needed for an object to escape from the gravitational influence of the celestial body without further propulsion.\n\n");
            ImGui.Text("GRAVITY: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The acceleration due to gravity at a given point in space. This value indicates how strongly the body pulls objects towards its center.  The gravity (surface) value displayed here is calculated from what would be felt at an average surface (mean radius) location.   Locations farther from the celestial's center will experience a drop-off in gravitational acceleration, while locations closer to the center will experience a higher gravity.\n\n");
            ImGui.Text("INCLINATION: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The tilt of an object's orbital plane in relation to another plane - for example the solar reference plane, or the equatorial plane of the body which the orbit is around.\n\n");
            ImGui.Text("MASS: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The amount of matter contained in the celestial body, typically measured in kilograms (kg).  Because diffrent celestial bodies can be made of very different materials, their mass in relation to size (average density) can vary widely.  For example - ice is much less dense than rock or metal.\n\n");
            ImGui.Text("MEAN RADIUS: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The average radius of the celestial body, calculated as the radius of a sphere with the same volume as the body.  For non-spherical bodies, this provides a standardized way to compare sizes.\n\n");
            ImGui.Text("ORBITAL PERIOD: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The time it takes for a celestial body to complete one full orbit around its parent body.\n\n");
            ImGui.Text("SEA LEVEL PRESSURE: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The atmospheric pressure at the mean surface level of the celestial body.  This value indicates how dense or thin the atmosphere is at the average surface level.\n\n");
            ImGui.Text("SEMIMAJOR AXIS: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("Half of the longest diameter of an elliptical orbit. It represents the average distance between the celestial body and its parent body over the course of its orbit.\n\n");
            ImGui.Text("SEMIMINOR AXIS: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("Half of the shortest diameter of an elliptical orbit. It is perpendicular to the semimajor axis and helps define the shape of the orbit.\n\n");
            ImGui.Text("SIDEREAL PERIOD: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The time it takes for a celestial body to complete one full rotation on its axis relative to distant stars.  This is different from a solar day, which is based on the position of the sun in the sky from one rotation to the next.  A Retrograde rotation means the body spins in the opposite direction to its orbit around the parent body.\n\n");
            ImGui.Text("SPHERE OF INFLUENCE: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("The region around a celestial body where its gravitational influence dominates over that of other bodies.\n\n");
            ImGui.Text("TIDALLY LOCKED ROTATION: "); ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("Indicates whether the celestial body is tidally locked to its parent body.  A tidally locked body always shows the same face to the body it orbits about.  In some cases both the parent and satellite are tidally locked to each other, both always showing the same face to each other.\n\n");

            PopTheFont();
        }
    }
}