using System.Text.Json;

namespace Compendium
{
    public class CompendiumData
    {
        // The different fields that can be in the JSON data for each celestial body
        public string? Sorting { get; set; }
        public List<string>? ListGroups { get; set; }
        public string? Text { get; set; }
        public List<string>? Factoids { get; set; }
        public List<string>? VisitedBy { get; set; }
        public string? LearnMoreUrl { get; set; }
    }

    public partial class Compendium
    {
        // Deserializes the json data for text descriptions later into a dictionary, load all of the jsons found in the folderpath given
        // The resulting dictionary is stored in bodyJsonDict
        public  static Dictionary<string, CompendiumData> bodyJsonDict = new Dictionary<string, CompendiumData>();
        public static string? parentDir;

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
                Console.WriteLine($"Looking for Compendium JSON data - in folders of: {parentDir}");
                
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
                
                Console.WriteLine($"Found {searchDirs.Count} directories with JSON files");
                
                // Collect all JSON files from allowed directories
                string[] jsonFiles = Array.Empty<string>();
                foreach (var dir in searchDirs)
                {
                    var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
                    jsonFiles = jsonFiles.Concat(files).ToArray();
                }
                
                Console.WriteLine($"Found {jsonFiles.Length} total JSON files");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        // Read and deserialize each JSON file - only include to bodyJsonDict if the json contains the kvp "CompendiumJson":"True"
                        var json = File.ReadAllText(file);
                        var jsonDoc = JsonDocument.Parse(json);
                        Console.WriteLine($"Processing JSON file: {Path.GetFileName(file)}");
                        
                        // Check for "CompendiumJson": "True" at the root level, otherwise this json file is ignored
                        if (jsonDoc.RootElement.TryGetProperty("CompendiumJson", out var compendiumValue) && compendiumValue.GetString() == "True")
                        {
                            Console.WriteLine($"  File has CompendiumJson marker");
                            // Iterate through all properties at the root level (except CompendiumJson) to find all keys containing body data
                            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                            {
                                // Skip the CompendiumJson marker property
                                if (property.Name == "CompendiumJson")
                                    continue;
                                
                                Console.WriteLine($"  Processing key: {property.Name}");
                                // Try to deserialize this property as a Dictionary<string, CompendiumData>
                                try
                                {
                                    var data = JsonSerializer.Deserialize<Dictionary<string, CompendiumData>>(property.Value.GetRawText(), options);
                                    if (data != null)
                                    {
                                        Console.WriteLine($"    Found {data.Count} bodies in {property.Name}");
                                        foreach (var kvp in data)
                                        {
                                            // Store with key format: "MainKey.BodyName" (e.g., "Compendium.Mercury")
                                            string fullKey = $"{property.Name}.{kvp.Key}";
                                            bodyJsonDict[fullKey] = kvp.Value;
                                            Console.WriteLine($"      Loaded: {fullKey}");
                                        }
                                    }
                                }
                                catch (Exception innerEx)
                                {
                                    Console.WriteLine($"    Failed to deserialize {property.Name}: {innerEx.Message}");
                                    // If this property isn't a valid body data structure, skip it
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  File missing CompendiumJson marker, skipping");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR loading {Path.GetFileName(file)}: {ex.Message}");
                        // Store error to display in UI
                        bodyJsonDict["ERROR"] = new CompendiumData 
                        { 
                            Text = $"Failed to load {Path.GetFileName(file)}: {ex.Message}" 
                        };
                    }
                }
                
                Console.WriteLine($"Total bodies loaded in bodyJsonDict: {bodyJsonDict.Count}");
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