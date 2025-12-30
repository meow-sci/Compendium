using System.Text.Json;
using KSA;
using Brutal.ImGuiApi;

namespace Compendium
{
    public class CompendiumData
    {
        // The different fields that can be in the JSON data for each celestial body
        public List<string>? ListGroups { get; set; }
        public string? OrbitLineGroup { get; set; }
        public string? Text { get; set; }
        public List<string>? Factoids { get; set; }
        public List<string>? VisitedBy { get; set; }
        public string? WikipediaUrl { get; set; }
        public Dictionary<string, CompendiumData>? ListGroupsData { get; set; }
        public string? CatText { get; set; }
        public string? CatWikipediaUrl { get; set; }
        public OrbitLineMode? OrbitLineMode { get; set; }
        public bool? DrawnUiBox { get; set; }
        public float RadiusKm { get; internal set; }
        public string? MassWithSIPrefix { get; internal set; }
        public double EarthMasses { get; internal set; }
        public double LunarMasses { get; internal set; }
        public double Mass { get; internal set; }
        public string? MassText { get; internal set; }
        public double Gravity { get; internal set; }
        public string? GravityText { get; internal set; }
        public string? EccentricityText { get; internal set; }
        public double EscapeVelocity { get; internal set; }
        public string? OrbitalPeriod { get; internal set; }
        public string? ThisTiltText { get; internal set; }
        public string? TidalLockText { get; internal set; }
        public string? SiderealPeriodText { get; internal set; }
        public string? EscapeVelocityText { get; internal set; }
        public string? SemiMajorAxisText { get; internal set; }
        public string? SemiMinorAxisText { get; internal set; }
        public string? OrbitTypeText { get; internal set; }
        public string? InclinationText { get; internal set; }
        public string? SphereOfInfluenceText { get; internal set; }
        public bool HasAtmosphere { get; internal set; }
        public string? AtmosphereHeightText { get; internal set; }
        public string? SLPressureText { get; internal set; }
    }

    public partial class Compendium
    {
        // Deserializes the json data for text descriptions later into a dictionary, load all of the jsons found in the folderpath given
        // The resulting dictionary is stored in bodyJsonDict


        public void LoadCompendiumJsonData(string dataDir)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            try
            {
                // Get parent directory of the DLL folder
                parentDir = Directory.GetParent(dataDir)?.FullName ?? dataDir;
                Console.WriteLine($"Compendium: Looking for Compendium JSON data - in folders of: {parentDir}");
                
                // Get all subdirectories in parent folder
                var allSubdirs = Directory.GetDirectories(parentDir, "*", SearchOption.AllDirectories).ToList();
                
                // Filter out excluded folders at the parent level and only include directories with JSON files
                var excludedFolders = new[] { "Core", "Shaders", "Versions" };
                var searchDirs = new List<string>();
                
                // Check parent directory for JSON files
                if (Directory.GetFiles(parentDir, "*.json", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    searchDirs.Add(parentDir);
                }
                
                foreach (var subdir in allSubdirs)
                {
                    // Get the immediate child folder name under parentDir
                    string relativePath = Path.GetRelativePath(parentDir, subdir);
                    string topLevelFolder = relativePath.Split(Path.DirectorySeparatorChar)[0];
                    
                    // Skip if the top-level folder is in excluded list
                    if (!excludedFolders.Contains(topLevelFolder))
                    {
                        // Only add if directory contains JSON files
                        if (Directory.GetFiles(subdir, "*.json", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            searchDirs.Add(subdir);
                        }
                    }
                }
                                
                // Collect all JSON files from allowed directories
                string[] jsonFiles = Array.Empty<string>();
                foreach (var dir in searchDirs)
                {
                    var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
                    jsonFiles = jsonFiles.Concat(files).ToArray();
                }
                
                Console.WriteLine($"Compendium: Found {jsonFiles.Length} total JSON files - looking in each for CompendiumJson marker");

                foreach (var file in jsonFiles)
                {
                        // Read and deserialize each JSON file - only include to bodyJsonDict if the json contains the kvp "CompendiumJson":"True"
                        var json = File.ReadAllText(file);
                        var jsonDoc = JsonDocument.Parse(json);
                        //Console.WriteLine($"Processing JSON file: {Path.GetFileName(file)}");
                        
                        // Check for "CompendiumJson": "True" at the root level, otherwise this json file is ignored
                        if (jsonDoc.RootElement.TryGetProperty("CompendiumJson", out var compendiumValue) && compendiumValue.GetString() == "True")
                        {
                           // Console.WriteLine($"Compendium: File has CompendiumJson marker - {Path.GetFileName(file)}");
                            // Iterate through all properties at the root level (except CompendiumJson) to find all keys containing body data
                            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                            {
                                // Skip the CompendiumJson marker property
                                if (property.Name == "CompendiumJson")
                                    continue;
                                
                                
                                // Try to deserialize the entire property as CompendiumData which may contain ListGroupsData
                                try
                                {
                                    var mainData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(property.Value.GetRawText(), options);
                                    if (mainData != null)
                                    {
                                        Console.WriteLine($"Compendium: File has CompendiumJson marker - {Path.GetFileName(file)} - {mainData.Count} entries found");
                                        
                                        // Check if ListGroupsData exists and deserialize it specially
                                        if (mainData.TryGetValue("ListGroupsData", out var listGroupsElement))
                                        {
                                            // Deserialize the categories dictionary directly
                                            var categoriesDict = JsonSerializer.Deserialize<Dictionary<string, CompendiumData>>(listGroupsElement.GetRawText(), options);
                                            if (categoriesDict != null)
                                            {
                                                //foreach (var cat in categoriesDict.Keys)
                                                //{
                                                //    Console.WriteLine($"        Category: {cat}");
                                                //}
                                                
                                                // Store with key format: "MainKey.ListGroupsData"
                                                string listGroupsKey = $"{property.Name}.ListGroupsData";
                                                
                                                // Check if we already have a container for this key and merge
                                                if (bodyJsonDict.TryGetValue(listGroupsKey, out var existingContainer) && 
                                                    existingContainer.ListGroupsData != null)
                                                {
                                                    // Merge new categories into existing dictionary
                                                    foreach (var kvp in categoriesDict)
                                                    {
                                                        existingContainer.ListGroupsData[kvp.Key] = kvp.Value;
                                                        //Console.WriteLine($"          Added/Updated category: {kvp.Key}");
                                                    }
                                                }
                                                else
                                                {
                                                    // Create a new CompendiumData object with the ListGroupsData populated
                                                    var listGroupsContainer = new CompendiumData 
                                                    { 
                                                        ListGroupsData = categoriesDict 
                                                    };
                                                    bodyJsonDict[listGroupsKey] = listGroupsContainer;
                                                    //Console.WriteLine($"        Created new container at key: {listGroupsKey}");
                                                }
                                            }
                                        }                                        // Store all other body entries
                                        foreach (var kvp in mainData)
                                        {
                                            if (kvp.Key != "ListGroupsData")
                                            {
                                                var bodyData = JsonSerializer.Deserialize<CompendiumData>(kvp.Value.GetRawText(), options);
                                                if (bodyData != null)
                                                {
                                                    string fullKey = $"{property.Name}.{kvp.Key}";
                                                    bodyJsonDict[fullKey] = bodyData;
                                                    //Console.WriteLine($"      Loaded: {fullKey}");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"    Failed to deserialize {property.Name}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine($"  File missing CompendiumJson marker, skipping");
                        }
                    

                }
                
                Console.WriteLine($"Compendium: Total bodies loaded: {bodyJsonDict.Count}");
            }
            catch (Exception ex)
            {
                bodyJsonDict["ERROR"] = new CompendiumData 
                { 
                    Text = $"Failed to load JSON data files: {ex.Message}" 
                };
            }
        }
    }
}