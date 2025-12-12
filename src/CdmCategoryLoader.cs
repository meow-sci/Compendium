using KSA;

namespace Compendium
{  
    public partial class Compendium
    {
        public static Dictionary<string, HashSet<string>> categoriesDict = new Dictionary<string, HashSet<string>>();
        public static string[]? categoryNames;
        public static Dictionary<string, Dictionary<string, object>>? buttonsCatsTree;
        private static bool categoriesBuiltWithCelestials = false;

        // Method to load categories from the loaded JSON data, or hardcoded defaults if none found
        public void CategoryLoader(Astronomical? worldSun = null)
        {
            // Build the category tree structure
            // buttonsCatsTree will have structure: [categoryName][parentBodyId] = { "Body": parentBodyId, "Children": [childBodyId1, childBodyId2, ...] }
            buttonsCatsTree = new Dictionary<string, Dictionary<string, object>>();
            
            
            // Collect all celestial objects from the tree
            var allCelestials = new List<Celestial>();
            
            // Use passed worldSun parameter, or fall back to Universe.WorldSun
            var sunToUse = worldSun ?? Universe.WorldSun;
            
            if (sunToUse != null)
            {
                CollectAllCelestials(sunToUse, allCelestials);
                if (allCelestials.Count > 0)
                {
                    categoriesBuiltWithCelestials = true;
                }
            }
            else
            {
                categoriesBuiltWithCelestials = false;
            }

            // Build a lookup for parent-child relationships
            var childToParentMap = new Dictionary<string, string>();
            foreach (var cel in allCelestials)
            {
                foreach (var child in cel.Children)
                {
                    if (child is Celestial childCel)
                    {
                        childToParentMap[childCel.Id] = cel.Id;
                    }
                }
            }
            
            // NOW build categoriesDict from JSON data
            categoriesDict.Clear(); // Clear any previous data
            
            // First, find the sun's direct children (top-level bodies)
            var sunDirectChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (sunToUse != null)
            {
                foreach (var child in sunToUse.Children)
                {
                    if (child is Celestial childCel)
                    {
                        sunDirectChildren.Add(childCel.Id);
                    }
                }
            }
            
            foreach (var kvp in bodyJsonDict)
            {
                string fullKey = kvp.Key; // e.g., "Compendium.Mercury"
                CompendiumData data = kvp.Value;

                // Skip CompendiumJson entries - that simply identifies it as a json to read.
                if (fullKey == "CompendiumJson" || fullKey.StartsWith("CompendiumJson."))
                    { continue; }

                // Extract the body ID (part after the dot)
                string bodyId;
                int dotIndex = fullKey.IndexOf('.');
                if (dotIndex > 0)
                { bodyId = fullKey.Substring(dotIndex + 1); }
                else
                { bodyId = fullKey; }
                
                // SKIP if this body is NOT a direct child of the sun
                // Only include categories for bodies whose parent is the current sun
                bool isDirectChildOfSun = sunDirectChildren.Any(id => id.Equals(bodyId, StringComparison.OrdinalIgnoreCase));
                if (!isDirectChildOfSun)
                {
                    continue;
                }
                
                // Extract the key name (part before the first dot)
                string keyName;
                if (dotIndex > 0)
                { keyName = fullKey.Substring(0, dotIndex); }
                else
                { keyName = fullKey; }
                
                // Add ListGroups to the dictionary for this key
                if (data != null && data.ListGroups != null)
                {
                    if (!categoriesDict.ContainsKey(keyName))
                    { categoriesDict[keyName] = new HashSet<string>(); }
                    
                    foreach (var group in data.ListGroups)
                    { categoriesDict[keyName].Add(group); }
                }
            }

            // First - Check if the categoriesDict got made, and use a bit of logic to determine if the Universe.System.Id is one of the loaded keys in the categoriesDict from CdmCategoryLoader. If categoriedDict does exist and the System.Id does not have a key in categoriesDict, check that "Compendium" exists and fall back to using that for categories.
            // After all of that fallback if nothing found, just use default hardcoded lists for each category.
            if (categoriesDict != null && categoriesDict.Count > 0)
            {
                string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                if (categoriesDict.ContainsKey(systemName))
                {
                    categoryNames = categoriesDict[systemName].ToArray();
                }
                else if (categoriesDict.ContainsKey("Compendium"))
                {
                    categoryNames = categoriesDict["Compendium"].ToArray();
                }
                else
                {
                    // Fallback to hardcoded default categories
                    categoryNames = ["FallbackCats", "Planets", "Dwarf Planets", "Trans-Neptunian Objects", "Main Asteroids", "Comets", "Interstellar Objects", "Other"];
                }
            }
            else 
            {
                // Fallback to hardcoded default categories
                categoryNames = ["FallbackCats", "Planets", "Dwarf Planets", "Trans-Neptunian Objects", "Main Asteroids", "Comets", "Interstellar Objects", "Other"];

            }



            // Ensure "Other" category always exists for uncategorized bodies
            if (categoryNames != null && !categoryNames.Contains("Other"))
            {
                categoryNames = categoryNames.Append("Other").ToArray();
            }

            // Process each category
            if (categoryNames != null)
            {
                foreach (var categoryName in categoryNames)
                {
                    // Skip the FallbackCats marker if it's still there
                    if (categoryName == "FallbackCats")
                        continue;

                    buttonsCatsTree[categoryName] = new Dictionary<string, object>();

                    // Find all celestials that belong to this category
                    var celestialsInCategory = new List<Celestial>();
                    foreach (var cel in allCelestials)
                    {
                        // IGNORE bodies that are children of other bodies - their categories should not create buttons
                        if (childToParentMap.ContainsKey(cel.Id))
                        {
                            continue;
                        }
                        
                        // Try system-specific key first (e.g., "SolarSystem.Mercury")
                        CompendiumData? celData = null;
                        string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                        string systemKey = $"{systemName}.{cel.Id}";
                        string compendiumKey = $"Compendium.{cel.Id}";
                        
                        if (!Compendium.bodyJsonDict.TryGetValue(systemKey, out celData))
                        {
                            // Fall back to default "Compendium" key (e.g., "Compendium.Mercury")
                            Compendium.bodyJsonDict.TryGetValue(compendiumKey, out celData);
                        }
                        
                        if (celData != null)
                        {
                            if (celData.ListGroups != null)
                            {
                            }
                        }

                        if (celData != null && celData.ListGroups != null && celData.ListGroups.Contains(categoryName))
                        {
                            celestialsInCategory.Add(cel);
                        }
                    }

                    // First pass: Add parent bodies (those that are not children of another body)
                    foreach (var cel in celestialsInCategory)
                    {
                        if (!childToParentMap.ContainsKey(cel.Id))
                        {
                            // This is a parent body (not a child of another)
                            buttonsCatsTree[categoryName][cel.Id] = new Dictionary<string, object>
                            {
                                ["Body"] = cel.Id,
                                ["Children"] = new List<string>()
                            };
                        }
                    }

                    // Second pass: Add children under their parents
                    foreach (var cel in celestialsInCategory)
                    {
                        // For each body in this category, add ALL its children regardless of their category
                        if (buttonsCatsTree[categoryName].ContainsKey(cel.Id))
                        {
                            var parentData = (Dictionary<string, object>)buttonsCatsTree[categoryName][cel.Id];
                            var childrenList = (List<string>)parentData["Children"];
                            
                            // Add all children of this celestial
                            foreach (var child in cel.Children)
                            {
                                if (child is Celestial childCel)
                                {
                                    childrenList.Add(childCel.Id);
                                }
                            }
                        }
                    }
                    // Now for any game loaded celestial which did not get added to buttonsCatsTree at all (e.g., because it has no category in JSON), add it to the "Other" category if it exists and add the body there.
                    if (categoryName == "Other")
                    {
                        foreach (var cel in allCelestials)
                        {
                            // IGNORE bodies that are children of other bodies - their categories should not create buttons
                            if (childToParentMap.ContainsKey(cel.Id))
                            {
                                continue;
                            }
                            
                            // Check if this celestial is already in buttonsCatsTree under any category
                            bool alreadyAdded = false;
                            foreach (var cat in buttonsCatsTree.Keys)
                            {
                                if (buttonsCatsTree[cat].ContainsKey(cel.Id))
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }
                            
                            if (!alreadyAdded)
                            {
                                // Add this celestial to the "Other" category
                                buttonsCatsTree["Other"][cel.Id] = new Dictionary<string, object>
                                {
                                    ["Body"] = cel.Id,
                                    ["Children"] = new List<string>()
                                };
                                
                                // Also add its children
                                var parentData = (Dictionary<string, object>)buttonsCatsTree["Other"][cel.Id];
                                var childrenList = (List<string>)parentData["Children"];
                                
                                foreach (var child in cel.Children)
                                {
                                    if (child is Celestial childCel)
                                    {
                                        childrenList.Add(childCel.Id);
                                    }
                                }
                            }
                        }
                    }

                    // Now see if the categoryName has a 'ListGroupsData.<categoryName>' key in buttonsCatsTree.  If it does, add a key named 'data' to the    buttonsCatsTree[categoryName] dictionary with that data.
                    // This attaches some category-level descriptive data to the category for display in the UI when the category is selected.
                    CompendiumData? listGroupData = null;
                    if (!bodyJsonDict.TryGetValue($"{systemName}.ListGroupsData.{categoryName}", out listGroupData))
                    {
                        // Fall back to default "Compendium" key (e.g., "Compendium.ListGroupsData.Asteroids")
                        bodyJsonDict.TryGetValue($"Compendium.ListGroupsData.{categoryName}", out listGroupData);
                    }
                    if (listGroupData != null)
                    { buttonsCatsTree[categoryName]["Data"] = listGroupData; }
                }
                // Finally - look to the "Other" category and see if it has any bodies. If it does not, remove the "Other" category entirely.
                if (buttonsCatsTree.ContainsKey("Other"))
                {
                    if (buttonsCatsTree["Other"].Count == 0)
                    {
                        buttonsCatsTree.Remove("Other");
                    }
                }
            }
            
        }
        // Method to get the list of category keys - it gets called after CategoryLoader runs to populate buttonsCatsTree
        public List<string> GetCategoryKeys()
        {
            List<string> categoryKeys = buttonsCatsTree?.Keys.ToList() ?? new List<string>();
            return categoryKeys;
        }
    }
}