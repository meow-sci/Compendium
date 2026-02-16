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

                // Get the category data from buttonsCatsTree
                IDictionary<string, object> categoryData = buttonsCatsTree[categoryKey];
                // Sorts the parent bodies alphabetically, but ONLY if the category is not "Planets" or is "Planets" but does not contain Mercury
                if (categoryKey != "Planets" || (categoryKey == "Planets" && !categoryData.ContainsKey("Mercury")))
                {
                    categoryData = new SortedDictionary<string, object>(categoryData);
                }
                else
                {
                    // For "Planets" category which contains Mercury, fix to put Mercury first. For some reason the game lists it second after Venus.
                    // Only gets here if Mercury is present in the category
                        var tempDict = new Dictionary<string, object>
                        {
                            { "Mercury", buttonsCatsTree[categoryKey]["Mercury"] }
                        };
                        foreach (var kvp in categoryData)
                        {
                            if (kvp.Key == "Mercury")
                                { continue; }
                            tempDict.Add(kvp.Key, kvp.Value);
                        }
                        categoryData = tempDict;
                }
            
            if (categoryData.Count > 0)
            {
                
                // Iterate through each parent body in this category
                foreach (var parentEntry in categoryData)
                {
                    string parentBodyId = parentEntry.Key;
                    
                    // Skip the "Data" entry which contains category-level information
                    if (parentBodyId == "Data")
                        continue;
                    
                    var parentData = (Dictionary<string, object>)parentEntry.Value;
                    var childrenIds = (List<string>)parentData["Children"];
                    
                    if (childrenIds.Count > 0)
                    {
                        // Has children, show as collapsing header with black background
                        PushTheColor("blackstuff");

                        ImString headerLabel = new ImString($"{parentBodyId}");
                        if (ImGui.CollapsingHeader(headerLabel))
                        {
                            ImGui.Separator();
                            // Add the parent celestial as a selectable entry first
                            ImString parentId = new ImString($"         ({parentBodyId})##parent_{parentBodyId}");
                            bool isParentSelected = selectedCelestialId == parentBodyId;
                            
                            if (isParentSelected)
                            { PushTheColor("ltblue"); }
                            
                            if (ImGui.Selectable(parentId, isParentSelected))
                            { 
                                selectedCelestialId = parentBodyId; 
                                selectedCelestial = FindCelestialById(Universe.WorldSun, parentBodyId);
                                showWindow = "Celestial";
                            }
                            
                            if (isParentSelected)
                            { ImGui.PopStyleColor(1); }
                            
                            // Check if parent has more than 10 children - if so, group by OrbitLineGroup
                            if (childrenIds.Count > 10)
                            {
                                // Group children by their OrbitLineGroup
                                var groupedChildren = new Dictionary<string, List<string>>();
                                
                                foreach (var childId in childrenIds)
                                {
                                    // Get the OrbitLineGroup from JSON
                                    CompendiumData? childData = null;
                                    string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                                    string systemKey = $"{systemName}.{childId}";
                                    string compendiumKey = $"Compendium.{childId}";
                                    
                                    if (!Compendium.bodyJsonDict.TryGetValue(systemKey, out childData))
                                    {
                                        Compendium.bodyJsonDict.TryGetValue(compendiumKey, out childData);
                                    }
                                    
                                    string orbitGroup = childData?.OrbitLineGroup ?? "Other";
                                    
                                    if (!groupedChildren.ContainsKey(orbitGroup))
                                    {
                                        groupedChildren[orbitGroup] = new List<string>();
                                    }
                                    groupedChildren[orbitGroup].Add(childId);
                                }
                                
                                // Display each group as a collapsing header
                                foreach (var group in groupedChildren)
                                {
                                    ImString groupLabel = new ImString($"          {group.Key}");
                                    if (ImGui.TreeNode(groupLabel))
                                    {
                                        foreach (var childId in group.Value)
                                        {
                                            ImString childIdStr = new ImString($"               {childId}##child_{childId}");
                                            bool isChildSelected = selectedCelestialId == childId;
                                            
                                            if (isChildSelected)
                                            { PushTheColor("ltblue"); }
                                            
                                            if (ImGui.Selectable(childIdStr, isChildSelected))
                                            { 
                                                selectedCelestialId = childId; 
                                                selectedCelestial = FindCelestialById(Universe.WorldSun, childId);
                                                showWindow = "Celestial";
                                            }
                                            
                                            if (isChildSelected)
                                            { ImGui.PopStyleColor(1); }
                                        }
                                        ImGui.TreePop();
                                    }
                                }
                            }
                            else
                            {
                                // 10 or fewer children - show them directly without grouping
                                foreach (var childId in childrenIds)
                                {
                                    ImString childIdStr = new ImString($"          {childId}##child_{childId}");
                                    bool isChildSelected = selectedCelestialId == childId;
                                    
                                    if (isChildSelected)
                                    { PushTheColor("ltblue"); }
                                    
                                    if (ImGui.Selectable(childIdStr, isChildSelected))
                                    { 
                                        selectedCelestialId = childId; 
                                        selectedCelestial = FindCelestialById(Universe.WorldSun, childId);
                                        showWindow = "Celestial";
                                    }
                                    
                                    if (isChildSelected)
                                    { ImGui.PopStyleColor(1); }
                                }
                            }

                        }
                        ImGui.Separator();
                        ImGui.PopStyleColor(3);
                    }
                    else
                    {
                        // No children, make selectable
                        ImString celestialId = new ImString($"      {parentBodyId}##celestial_{parentBodyId}");
                        bool isCelestialSelected = selectedCelestialId == parentBodyId;
                        
                        if (isCelestialSelected)
                        { PushTheColor("ltblue"); }
                        
                        if (ImGui.Selectable(celestialId, isCelestialSelected))
                        {
                            selectedCelestialId = parentBodyId; 
                            selectedCelestial = FindCelestialById(Universe.WorldSun, parentBodyId);
                            showWindow = "Celestial";
                        }

                        if (isCelestialSelected)
                        { ImGui.PopStyleColor(1); }
                    }
                }
            }
            }
            catch (Exception ex)
            {
                ImString errorMsg = new ImString($"Error: {ex.Message}");
                ImGui.Text(errorMsg);
            }
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