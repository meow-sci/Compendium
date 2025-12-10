using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Internal;
using Brutal.Numerics;
using KSA;
using ModMenu;
using StarMap.API;
using System.Numerics;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    [StarMapMod]
    public partial class Compendium
    {
        // Static constructor to log when class is initialized
        static Compendium()
        {
            Console.WriteLine("=== Compendium static constructor called ===");
        }

        void BoldSeparator ()
        {
            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color }
        }
        public static string? dllDir;
        public static float fontSizeCurrent = 26;
        public static float windowOpacity = 1.0f;
        public static bool CompendiumWindow = true;
        public static int selectedFontIndex = 0;
        public static int previousFontIndex = -1;
        public static int selectedCategoryIndex = 0;
        public static string selectedCategoryKey = "";
        public static bool showFontSettings = false;
        public static string selectedCelestialId = "None";
        public static float mainContentWidth = 400f;
        public static List<string> categoryKeys = new List<string>();

        [ModMenuEntry("Compendium Window")]

        // Makes the menu entry to open/close the compendium window using ModMenu.Attributes.dll
        public static void DrawSubMenuEntry()
        {
            if (ImGui.MenuItem("Compendium Window"))
            { CompendiumWindow = !CompendiumWindow; 
            Console.WriteLine("Toggled Compendium ModMenu Window variable");
            }
        }

        [StarMapAfterGui]
        public unsafe void OnAfterUi(double dt)
        {
            try
            {
                if (!CompendiumWindow)
                {
                    return;
                }
                
                // Wait for universe to be fully loaded
                var logFile = @"C:\temp\compendium_debug.log";
                
                if (Universe.WorldSun == null)
                {
                    System.IO.File.AppendAllText(logFile, "OnAfterUi - Universe.WorldSun is NULL, waiting...\n");
                    ImGui.SetNextWindowBgAlpha(windowOpacity);
                    if (ImGui.Begin("Compendium"))
                    {
                        ImGui.Text("Waiting for universe to load...");
                        ImGui.End();
                    }
                    return;
                }
                
                System.IO.File.AppendAllText(logFile, $"Checking CategoryLoader - buttonsCatsTree null: {buttonsCatsTree == null}, count: {buttonsCatsTree?.Count ?? -1}, builtWithCelestials: {Compendium.categoriesBuiltWithCelestials}\n");
                
                // Build category tree on first render when Universe is loaded, or rebuild if it was built without celestials
                if ((buttonsCatsTree == null || buttonsCatsTree.Count == 0 || !Compendium.categoriesBuiltWithCelestials))
                {
                    var worldSun = Universe.WorldSun;
                    System.IO.File.AppendAllText(logFile, $"CategoryLoader starting - WorldSun null: {worldSun == null}\n");
                    CategoryLoader(worldSun);
                    categoryKeys = GetCategoryKeys();
                    System.IO.File.AppendAllText(logFile, $"CategoryLoader complete - Keys: {categoryKeys?.Count ?? 0}\n");
                    
                    // Initialize selectedCategoryKey with first non-Moons category
                    if (categoryKeys != null && categoryKeys.Count > 0)
                    {
                        foreach (var key in categoryKeys)
                        {
                            System.IO.File.AppendAllText(logFile, $"  Key: {key}\n");
                            if (string.IsNullOrEmpty(selectedCategoryKey) && key != "Moons")
                            {
                                selectedCategoryKey = key;
                                System.IO.File.AppendAllText(logFile, $"  Set selectedCategoryKey to: {key}\n");
                            }
                        }
                    }
                }
                
                // Get category keys if not already set
                if (categoryKeys == null || categoryKeys.Count == 0)
                {
                    categoryKeys = GetCategoryKeys();
                }

                ImGui.SetNextWindowBgAlpha(windowOpacity);
                if (!ImGui.Begin("Compendium"))
                {
                    ImGui.End();
                    return;
                }
                
                // Get available window size
                float2 windowSize = ImGui.GetContentRegionAvail();
                
                // Main content area
                ImGui.BeginChild("MainContent", new float2(mainContentWidth, 0));
                
                if (fontNames.Length > 0)
                {
                    // Apply selected font if it changed and exists
                    if (selectedFontIndex != previousFontIndex && selectedFontIndex < fontNames.Length)
                    { previousFontIndex = selectedFontIndex; }
                    
                    // Dropdown arrow button for font settings
                    if (ImGui.ArrowButton("FontSettingsArrow", showFontSettings ? ImGuiDir.Up : ImGuiDir.Down))
                    { showFontSettings = !showFontSettings; }
                    
                    if (showFontSettings)
                    {
                        ImGui.Text("Font Choices:");
                        ImGui.Text(" ");
                        // First gets the font style names to use for buttons by chopping off the beginning 'name-' part by using - as delimeter
                        string[] fontStyles = new string[fontNames.Length];
                        for (int i = 0; i < fontNames.Length; i++)
                        {
                            string[] parts = fontNames[i].Split('-');
                            fontStyles[i] = parts.Length > 1 ? parts[1] : fontNames[i];
                        }

                        // tries out making a dropdown box instead of buttons
                        //for (int j = 0; j < fontStyles.Length && j < fontNames.Length; j++)
                        // {
                        //     ImString fontLabel = new ImString(fontStyles[j]);
                        //     if (ImGui.Selectable(fontLabel, selectedFontIndex == j))
                        //     {selectedFontIndex = j; }
                        // }

                        for (int i = 0; i < fontStyles.Length && i < fontNames.Length; i++)
                        {
                            ImString buttonLabel = new ImString(fontStyles[i]);
                            if (ImGui.SmallButton(buttonLabel))
                            { selectedFontIndex = i; }
                        }
                        
                        ImGui.Separator();
                        ImGui.SliderFloat("Size", ref fontSizeCurrent, 12f, 42f);
                        ImGui.SliderFloat("Opacity", ref windowOpacity, 0.1f, 1.0f);
                        ImGui.Separator();
                        // Display the selected font name
                        ImString selectedText = new ImString($"Selected: {fontNames[selectedFontIndex]}");
                        ImGui.Text(selectedText); ImGui.Text(" ");
                        ImGui.PopFont();
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                    }
                    

                }
                else
                {
                    // Sets a default font if no fonts were loaded
                    ImGui.Text("No fonts found in Fonts folder - using Default font");
                }

                // Always render categories (with or without custom fonts)
                PushTheFont(1);
   
                // Category selection buttons
                ImGui.Text("Astronomicals Categories:");
                
                // Pop the large font and push smaller font for category buttons
                ImGui.PopFont();
                PushTheFont(0.75f);
                
                // Makes the category buttons by using which keys exist in the buttonsCatsTree dictionary,
                // using the categoryKeys list made just after making buttonCatsTree
                // If there is a Planets category, make sure it is the first entry.  After that list all of the other categories alphabetically.
                List<string> sortedCategoryKeys = new List<string>(categoryKeys);
                if (sortedCategoryKeys.Contains("Planets"))
                {
                    sortedCategoryKeys.Remove("Planets");
                    sortedCategoryKeys.Sort();
                    sortedCategoryKeys.Insert(0, "Planets");
                }
                else
                {
                    sortedCategoryKeys.Sort();
                }
                foreach (var categoryKey in sortedCategoryKeys)
                {
                    ImString buttonLabel = new ImString(categoryKey);
                    
                    if (ImGui.Button(buttonLabel))
                    {
                        selectedCategoryKey = categoryKey;
                        selectedCategoryIndex = sortedCategoryKeys.IndexOf(categoryKey);
                    }

                }

                // Pop small font and restore large font
                ImGui.PopFont();
                PushTheFont(1);

                ImGui.Separator();
                ImString selectedCategory = new ImString($"Selected: {selectedCategoryKey}");

                ImGui.Text(" ");
                ImGui.Text(selectedCategory);
                DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                ImGui.Text(" ");

                PrintSelectedCategoryByKey(selectedCategoryKey);

                ImGui.PopFont();

                ImGui.EndChild();
                
                // Resizable splitter
                ImGui.SameLine();
                ImGui.Button("##splitter", new float2(8f, -1));
                if (ImGui.IsItemActive())
                {
                    mainContentWidth += ImGui.GetIO().MouseDelta.X;
                    // Clamp to reasonable values
                    if (mainContentWidth < 200f) mainContentWidth = 200f;
                    if (mainContentWidth > windowSize.X - 250f) mainContentWidth = windowSize.X - 250f;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                }
                
                // Side pane - calculate remaining width
                ImGui.SameLine();
                float sidePaneWidth = windowSize.X - mainContentWidth - 16f; // Account for splitter and spacing
                ImGui.BeginChild("SidePane", new float2(sidePaneWidth, 0));
                
                
                ImGui.Separator();
                // pushes a larger font only for the selected celestial id display
                PushTheFont(1.7f);
                ImString selectedIdText = new ImString($"{selectedCelestialId}");
                ImGui.Text(selectedIdText);
                ImGui.PopFont();
                PushTheFont(1);
                ImGui.Text(" ");
                
                // Find the selected celestial object from Universe.WorldSun tree
                Celestial? selectedCelestial = null;
                if (selectedCelestialId != "None" && Universe.WorldSun != null)
                {
                    var allCelestials = new List<Celestial>();
                    CollectAllCelestials(Universe.WorldSun, allCelestials);
                    selectedCelestial = allCelestials.FirstOrDefault(c => c.Id == selectedCelestialId);
                }

                // Try to get the JSON data for the selected celestial loaded into a bodyJson object.
                // Try system-specific data first, then fall back to default "Compendium" data
                CompendiumData? bodyJson = null;
                if (selectedCelestial != null) 
                { 
                    string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                    
                    // Try system-specific key first (e.g., "SolarSystem.Mercury")
                    if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{selectedCelestial.Id}", out bodyJson))
                    {
                        // Fall back to default "Compendium" key (e.g., "Compendium.Mercury")
                        Compendium.bodyJsonDict.TryGetValue($"Compendium.{selectedCelestial.Id}", out bodyJson);
                    }
                    
                }
 
                // If selectedCelestial was found, display its information, checking at each step if the specific data being looked for is available.
                if (selectedCelestial != null)
                {
                    // Show URL link if JSON data with URL is available
                    if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.WikipediaUrl))
                    { 
                        ImString linkText = new ImString($"https://en.wikipedia.org/wiki/{bodyJson.WikipediaUrl}");
                        ImGui.TextLinkOpenURL(linkText);
                    }
                    
                    // Prints the radius
                    float radiusKm = selectedCelestial.ObjectRadius / 1000f;
                    ImString radiusText = new ImString($"Radius: {radiusKm} km");
                    ImGui.Text(radiusText);

                    // now gets the mass of the selected celestial object with appropriate SI prefix unit
                    string massWithSIPrefix = FormatMassWithUnit(selectedCelestial.Mass);
                    // now gets a value for how many Earth masses the selected celestial object is, or if it's 1 percent or less of that, use Lunar masses
                    double earthMasses = selectedCelestial.Mass / 5.972168e24;
                    double lunarMasses = selectedCelestial.Mass / 7.342e22;
                    ImString massText;
                    if (earthMasses > 0.01) { massText = new ImString($"Mass: {selectedCelestial.Mass:E2} Kg / ({massWithSIPrefix}) / ({earthMasses:F3} Earths)"); }
                    else if (lunarMasses > 0.00001) { massText = new ImString($"Mass: {selectedCelestial.Mass:E2} Kg / ({massWithSIPrefix}) / ({lunarMasses:F5} Lunas)"); }
                    else { massText = new ImString($"Mass: {selectedCelestial.Mass:E2} Kg / ({massWithSIPrefix})"); }
                    ImGui.Text(massText);

                    // Calculates gravity using the formula: g = G * M / R^2 and then what the surface gravity would be.
                    // where G is the gravitational constant (6.67430 × 10^-11 m^3 kg^-1 s^-2), M is the mass in kg, R is the radius in meters.
                    double gravity = 6.67430e-11 * selectedCelestial.Mass / (selectedCelestial.ObjectRadius * selectedCelestial.ObjectRadius);
                    // if the gravity just found is less than 0.001 m/s², display it as up to six decimal places.
                    ImString gravityText;
                    if (gravity < 0.001) { gravityText = new ImString($"Gravity: {gravity:F6} m/s²");}
                    else { gravityText = new ImString($"Gravity: {gravity:F3} m/s²"); }
                    ImGui.Text(gravityText);

                    // calculates the escape velocity using the formula: v = sqrt(2 * G * M / R)
                    double escapeVelocity = Math.Sqrt(2 * 6.67430e-11 * selectedCelestial.Mass / selectedCelestial.ObjectRadius);
                    ImString escapeVelocityText = new ImString($"Escape Velocity: {escapeVelocity / 1000f:F3} km/s");
                    ImGui.Text(escapeVelocityText);

                                        
                    DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                    ImGui.Text(" ");
                    // Makes a button to focus on the selected celestial objeect, but does not put a newline after it
                    ImString focusText = new ImString($"Focus Camera on {selectedCelestial.Id}");
                    if (ImGui.Button(focusText))
                    {
                        KSA.Universe.MoveCameraTo(selectedCelestial);
                    }
                    ImGui.SameLine(); ImGui.Text(" | "); ImGui.SameLine();
                    // Tries to make a button which toggles on/off the orbit lines for the selected celestial object
                    ImString orbitText = new ImString("Orbit Line"); 
                    if (ImGui.Button(orbitText))
                    {
                        selectedCelestial.ShowOrbit = !selectedCelestial.ShowOrbit;
                    }
                    ImGui.SameLine();
                    // Makes a checkbox that has an x to show the current state of the orbit line visibility
                    ImGui.Checkbox("##orbitCheckbox", ref selectedCelestial.ShowOrbit);
                    // If the selectedcelestial has moons/children, make a button which toggles all of their orbit lines on/off
                    if (selectedCelestial.Children.Count > 0)
                    {
                        ImGui.Text(" ");
                        ImString toggleMoonsText = new ImString("Toggle All Satellite Body Orbits:  ");
                        ImGui.Text(toggleMoonsText); ImGui.SameLine();
                        // Makes two buttons, one which turns on all the orbit lines of the moons, and one which turns them all off
                        ImString moonsOnText = new ImString("< On >");
                        if (ImGui.Button(moonsOnText))
                        {
                            foreach (var child in selectedCelestial.Children)
                            {
                                if (child is Celestial cel)
                                {
                                    cel.ShowOrbit = true;
                                }
                            }
                        }
                        ImGui.SameLine();
                        ImString moonsOffText = new ImString("< Off >");
                        if (ImGui.Button(moonsOffText))
                        {
                            foreach (var child in selectedCelestial.Children)
                            {
                                if (child is Celestial cel)
                                {
                                    cel.ShowOrbit = false;
                                }
                            }
                        }
                    }
                    // Now checks to see if the body is a Parent and has more than one group of Child OrbitLineGroups.  If so, make buttons to toggle each group on/off
                    var orbitLineGroups = new HashSet<string>();
                    foreach (var child in selectedCelestial.Children)
                    {
                        if (child is Celestial cel)
                        {
                            // Try to get the OrbitLineGroup from the JSON data for this child celestial
                            CompendiumData? childBodyJson = null;
                            string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                            if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                            {
                                Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson);
                            }
                            if (childBodyJson != null && !string.IsNullOrEmpty(childBodyJson.OrbitLineGroup))
                            {
                                orbitLineGroups.Add(childBodyJson.OrbitLineGroup);
                            }
                        }
                    }
                    if (orbitLineGroups.Count > 1)
                    {
                        ImGui.Text(" ");
                        ImString orbitGroupText = new ImString("Toggle Orbit Lines by Group:  ");
                        ImGui.Text(orbitGroupText);
                        ImGui.Separator();
                        foreach (var group in orbitLineGroups)
                        {
                            ImGui.BulletText($" "); ImGui.SameLine();
                            ImString groupOnText = new ImString($"< On >##groupOn_{group}");
         
                            if (ImGui.Button(groupOnText))
                            {
                                foreach (var child in selectedCelestial.Children)
                                {
                                    if (child is Celestial cel)
                                    {
                                        // Get the OrbitLineGroup from JSON
                                        CompendiumData? childBodyJson = null;
                                        string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                                        if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                        {
                                            Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson);
                                        }
                                        if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                        {
                                            cel.ShowOrbit = true;
                                        }
                                    }
                                }
                            }
                            ImGui.SameLine();
                            ImString groupOffText = new ImString($"< Off >##groupOff_{group}");
                            if (ImGui.Button(groupOffText))
                            {
                                foreach (var child in selectedCelestial.Children)
                                {
                                    if (child is Celestial cel)
                                    {
                                        // Get the OrbitLineGroup from JSON
                                        CompendiumData? childBodyJson = null;
                                        string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
                                        if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                        {
                                            Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson);
                                        }
                                        if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                        {
                                            cel.ShowOrbit = false;
                                        }
                                    }
                                    
                                }
                                
                            }
                            ImGui.SameLine();
                            ImString groupText = new ImString($"{group}");
                            ImGui.Text(groupText);

                        }

                    }
                    ImGui.Text(" ");

                    // After displaying the celestial properties, show JSON compendium data if available
                    if (bodyJson != null)
                    {
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                        if (bodyJson.Text != null)
                        {
                            ImGui.TextWrapped(bodyJson.Text);
                        }
                        
                        if (bodyJson.Factoids != null && bodyJson.Factoids.Count > 0)
                        {
                            ImGui.Text(" ");
                            ImGui.Text("Factoids:");
                            foreach (var factoid in bodyJson.Factoids)
                            {
                                ImGui.BulletText(factoid);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.Text("Celestial not found");
                }
                
                ImGui.PopFont();
                
                ImGui.EndChild(); // End side pane
                
                ImGui.End();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compendium OnAfterUi error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        [StarMapImmediateLoad]
        public void OnImmediateLoad(Mod mod)
        {
            try
            {
                Console.WriteLine("=== Compendium - OnImmediateLoad START ===");

                // Gets the current working directory path (of the DLL)
                dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // Loads fonts from the Fonts folder
                LoadFonts(dllDir, fontSizeCurrent);

                if (dllDir != null && Directory.Exists(dllDir))
                {
                    // Load celestial body descriptions from JSON files - pass dllDir since LoadCompendiumJsonData will search subdirectories
                    LoadCompendiumJsonData(dllDir);
                }
                
                Console.WriteLine("=== Compendium - OnImmediateLoad END ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Compendium - OnImmediateLoad EXCEPTION ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        

        [StarMapAllModsLoaded]
        public void OnFullyLoaded()
        {
            try
            {
                // Loads body categories from the loaded JSON data - this determines buttons to make.
                CategoryLoader();

                categoryKeys = GetCategoryKeys();

                // Makes logging to show each thing added to the buttonsCatsTree dictionary
                if (buttonsCatsTree == null)
                {
                    Console.WriteLine("buttonsCatsTree is null");
                    return;
                }
                
                foreach (var category in buttonsCatsTree)
                {
                    Console.WriteLine($"Category: {category.Key}");
                    var categoryData = category.Value;
                    foreach (var parentBody in categoryData)
                    {
                        Console.WriteLine($"  Parent Body: {parentBody.Key}");
                        var parentData = (Dictionary<string, object>)parentBody.Value;
                        var children = (List<string>)parentData["Children"];
                        Console.WriteLine($"    Children: {string.Join(", ", children)}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Compendium - OnFullyLoaded EXCEPTION ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            //Patcher.Patch();
        }


        [StarMapUnload]
        public void Unload()
        {
            Console.WriteLine("Compendium - Unload");
            //Patcher.Unload();
        }
    }
}