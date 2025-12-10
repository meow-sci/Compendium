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
            // Uses the loaded JSON data to populate category lists for celestial bodies
            // Extracts the key name (e.g., "Compendium" from "Compendium.Mercury")
            // and collects all unique ListGroups values for each key
            
            foreach (var kvp in bodyJsonDict)
            {
                string fullKey = kvp.Key; // e.g., "Compendium.Mercury"
                CompendiumData data = kvp.Value;

                // Skip CompendiumJson entries
                if (fullKey == "CompendiumJson" || fullKey.StartsWith("CompendiumJson."))
                    { continue; }

                // Extract the key name (part before the first dot)
                string keyName;
                int dotIndex = fullKey.IndexOf('.');
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

            // Build the category tree structure
            // buttonsCatsTree will have structure: [categoryName][parentBodyId] = { "Body": parentBodyId, "Children": [childBodyId1, childBodyId2, ...] }
            buttonsCatsTree = new Dictionary<string, Dictionary<string, object>>();
            
            var logFile = @"C:\temp\compendium_debug.log";
            
            // Collect all celestial objects from the tree
            var allCelestials = new List<Celestial>();
            
            // Use passed worldSun parameter, or fall back to Universe.WorldSun
            var sunToUse = worldSun ?? Universe.WorldSun;
            
            if (sunToUse != null)
            {
                CollectAllCelestials(sunToUse, allCelestials);
                System.IO.File.AppendAllText(logFile, $"CategoryLoader - Collected {allCelestials.Count} celestials\n");
                if (allCelestials.Count > 0)
                {
                    categoriesBuiltWithCelestials = true;
                }
            }
            else
            {
                System.IO.File.AppendAllText(logFile, "CategoryLoader - worldSun is NULL\n");
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
                        
                        System.IO.File.AppendAllText(logFile, $"  Checking {cel.Id} for category {categoryName}\n");
                        System.IO.File.AppendAllText(logFile, $"    Tried keys: {systemKey}, {compendiumKey}\n");
                        System.IO.File.AppendAllText(logFile, $"    celData is null: {celData == null}\n");
                        if (celData != null)
                        {
                            System.IO.File.AppendAllText(logFile, $"    ListGroups is null: {celData.ListGroups == null}\n");
                            if (celData.ListGroups != null)
                            {
                                System.IO.File.AppendAllText(logFile, $"    ListGroups: {string.Join(", ", celData.ListGroups)}\n");
                                System.IO.File.AppendAllText(logFile, $"    Contains {categoryName}: {celData.ListGroups.Contains(categoryName)}\n");
                            }
                        }

                        if (celData != null && celData.ListGroups != null && celData.ListGroups.Contains(categoryName))
                        {
                            System.IO.File.AppendAllText(logFile, $"    MATCHED! Adding {cel.Id} to category {categoryName}\n");
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
                                    System.IO.File.AppendAllText(logFile, $"    Added child {childCel.Id} to parent {cel.Id}\n");
                                }
                            }
                        }
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