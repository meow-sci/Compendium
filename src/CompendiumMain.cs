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
        public static string? dllDir;
        public static float fontSizeCurrent = 30f;
        public static float windowOpacity = 1.0f;
        public static bool CompendiumWindow = true;
        public static int selectedFontIndex = 0;
        public static int previousFontIndex = -1;
        public static int selectedCategoryIndex = -1;
        public static string selectedCategoryKey = "";
        public static bool showFontSettings = false;
        public static string selectedCelestialId = "Compendium Astronomicals Database";
        public static float mainContentWidth = 400f;
        public static List<string> categoryKeys = new List<string>();
        private static string justSelected = "";
        private static string systemName = Universe.CurrentSystem?.Id ?? "Dummy";

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
        public void OnAfterUi(double dt)
        {
            systemName = Universe.CurrentSystem?.Id ?? "Dummy";
            try
            {
                if (!CompendiumWindow)
                {
                    return;
                }
                
                // Wait for universe to be fully loaded
                
                if (Universe.WorldSun == null)
                {
                    ImGui.SetNextWindowBgAlpha(windowOpacity);
                    if (ImGui.Begin("Compendium"))
                    {
                        ImGui.Text("Waiting for universe to load...");
                        ImGui.End();
                    }
                    return;
                }                
                
                // Build category tree on first render when Universe is loaded, or rebuild if it was built without celestials
                if (buttonsCatsTree == null || buttonsCatsTree.Count == 0 || !Compendium.categoriesBuiltWithCelestials)
                {
                    var worldSun = Universe.WorldSun;
                    CategoryLoader(worldSun);
                    categoryKeys = GetCategoryKeys();
                    
                    // Initialize selectedCategoryKey with first non-Moons category
                    if (categoryKeys != null && categoryKeys.Count > 0)
                    {
                        foreach (var key in categoryKeys)
                        {
                            if (string.IsNullOrEmpty(selectedCategoryKey) && key != "Moons")
                            {
                                selectedCategoryKey = key;
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
                        PushTheFont(1);
                        ImGui.Text("Font Choices:");

                        // Use the full font names for display
                        string[] fontStyles = new string[fontNames.Length];
                        for (int i = 0; i < fontNames.Length; i++)
                        {
                            fontStyles[i] = fontNames[i];
                        }
                        // Makes a dropdown selection for the font styles instead of buttons
                        
                        string combinedFontStyles = string.Join("\0", fontStyles) + "\0\0";
                        ImString fontStylesImString = new ImString(combinedFontStyles);
                        ImGui.Combo(" ", ref selectedFontIndex, fontStylesImString, fontStyles.Length);
                        ImGui.Text(" ");

                        //for (int i = 0; i < fontStyles.Length && i < fontNames.Length; i++)
                        //{
                        //    ImString buttonLabel = new ImString(fontStyles[i]);
                        //    if (ImGui.SmallButton(buttonLabel))
                        //    { selectedFontIndex = i; }
                        //}
                        
                        ImGui.Separator();
                        ImGui.SliderFloat("Size", ref fontSizeCurrent, 16f, 46f);
                        ImGui.SliderFloat("Opacity", ref windowOpacity, 0.1f, 1.0f);
                        ImGui.Separator();
                        ImGui.Text(" ");
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
                
                List<string> sortedCategoryKeys = new List<string>(categoryKeys);
                // If there is a Planets category, make sure it is the first entry.
                // IF there is a category named 'Other', make sure it is the last entry - but only if it exists in sortedCategoryKeys.
                // Otherwise everything else is sorted alphabetically.
                sortedCategoryKeys.Sort();
                if (sortedCategoryKeys.Contains("Planets"))
                {
                    sortedCategoryKeys.Remove("Planets");
                    sortedCategoryKeys.Insert(0, "Planets");
                }
                if (sortedCategoryKeys.Contains("Other"))
                {
                    sortedCategoryKeys.Remove("Other");
                    sortedCategoryKeys.Add("Other");
                }

                //  After that list all of the categories listed as the list was just made.
                //  If the category is selected, set selectedCategoryKey to that key to make it's special window show.
                foreach (var categoryKey in sortedCategoryKeys)
                {
                    ImString buttonLabel = new ImString(categoryKey);
                    
                    if (ImGui.Button(buttonLabel))
                    {
                        selectedCategoryKey = categoryKey;
                        selectedCategoryIndex = sortedCategoryKeys.IndexOf(categoryKey);
                        justSelected = categoryKey;;
                    }
                }
                // If the button wasn't pushed and the selectedCategoryIndex is -1, set it to 0 and select the first category by default.
                if (selectedCategoryIndex == -1 && sortedCategoryKeys.Count > 0)
                {
                 //   selectedCategoryIndex = 0;
                    selectedCategoryKey = sortedCategoryKeys[0];
                  //  justSelected = selectedCategoryKey;
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
                
                // First regular bodies if selected, then if category was just selected make special text for the category and it's information / data.
                if (justSelected != selectedCategoryKey)
                {
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
                            ImString labelText = new ImString($"Wikipedia Page: {bodyJson.WikipediaUrl}");
                            ImGui.TextLinkOpenURL(labelText, linkText);
                        }
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                        // Prints the radius
                        float radiusKm = selectedCelestial.ObjectRadius / 1000f;
                        ImString radiusText = new ImString($"Mean Radius: {radiusKm} km");
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
                        ImGui.Text(" ");
                        // Makes a button to toggle showing or hiding a subsection for displaying the orbital properties of the selected celestial object.
                        // Always starts hidden until button is clicked.
                        bool showOrbitalProperties = false;
                        ImString orbitalHeader = new ImString("More Orbital Properties");
                        if (ImGui.CollapsingHeader(orbitalHeader))
                        {
                            showOrbitalProperties = true;
                        }
                        if (showOrbitalProperties)
                        {
                            // Makes a smaller font size for the orbital properties section
                            PushTheFont(0.8f);

                            
                            // Figures out the timescale we want to use for displaying the orbital period.  If it's more than 2 years, use years.  If it's more than 2 days, use days.  Otherwise use hours.
                            double orbitalPeriodSeconds = selectedCelestial.Orbit.Period;
                            string orbitalDisplay;
                            
                            if (orbitalPeriodSeconds >= 63072000) // More than 2 years
                            {
                                double orbitalVal = orbitalPeriodSeconds / 31536000;
                                orbitalDisplay = orbitalVal.ToString("F2") + " years";
                            }
                            else if (orbitalPeriodSeconds >= 172800) // More than 2 days
                            {
                                double orbitalVal = orbitalPeriodSeconds / 86400;
                                orbitalDisplay = orbitalVal.ToString("F2") + " days";
                            }
                            else // Use hours
                            {
                                double orbitalVal = orbitalPeriodSeconds / 3600;
                                orbitalDisplay = orbitalVal.ToString("F2") + " hours";
                            }

                            // Gets the axial tilt values depending on whether the selected celestial's parent is the sun or another body.
                            double thisTilt;
                            ImString thisTiltText;
                            if (selectedCelestial.Parent == Universe.WorldSun)
                            { 
                                thisTilt = selectedCelestial.GetCce2Cci().ToXyzRadians().X * (180.0 / Math.PI);
                                thisTiltText = new ImString($"{thisTilt:F2}°");
                            }
                            else
                            {
                                thisTilt = selectedCelestial.GetCci2Orb().Inverse().ToXyzRadians().X * (180.0 / Math.PI);
                                string parentName = selectedCelestial.Parent.Id;
                                thisTiltText = new ImString($"{thisTilt:F2}° ( Relative to {selectedCelestial.Parent.Id} )");
                            } 

                            ImString axialTiltText = new ImString($"Axial Tilt: {thisTiltText}");
                            ImGui.Text(axialTiltText);
                            ImString eccentricityText = new ImString($"Eccentricity: {selectedCelestial.Eccentricity:F4}");
                            ImGui.Text(eccentricityText);
                            ImString inclinationText = new ImString($"Inclination: {selectedCelestial.Inclination:F2}°");
                            ImGui.Text(inclinationText);
                            ImString orbitalPeriodText = new ImString($"Orbital Period: {orbitalDisplay}");
                            ImGui.Text(orbitalPeriodText);


                            // Gets the Semi-Major and Semi-Minor axes in AU for display if they are large enough - we only need a float.
                            // Keep in mind that both values are saved in game as meters, so we need divide by the appropritate factor to get m to AU.  1 AU = 1.496e+11 m
                            float semiMajorAxisAU = (float)selectedCelestial.SemiMajorAxis / 1.496e+11f; // Convert m to AU
                            float semiMinorAxisAU = (float)selectedCelestial.SemiMinorAxis / 1.496e+11f; // Convert m to AU
                            // Next saves the value of each in km as strings adding thousands separators for easier reading.
                            // Clamps the decimal places to 1 for cleaner display.
                            string semiMajorAxisKm = (selectedCelestial.SemiMajorAxis / 1000f).ToString("N1");
                            string semiMinorAxisKm = (selectedCelestial.SemiMinorAxis / 1000f).ToString("N1");

                            // If the semi-major axis is less than 0.1 AU, display it in AU - otherwise display with both km and then AU. Use the same semimajor test for both it and semiminor printing
                            if (semiMajorAxisAU < 0.1f)
                            {
                                ImString semiMajorAxisAUText = new ImString($"Semi-Major Axis: {semiMajorAxisKm} km");
                                ImGui.Text(semiMajorAxisAUText);
                                ImString semiMinorAxisAUText = new ImString($"Semi-Minor Axis: {semiMinorAxisKm} km");
                                ImGui.Text(semiMinorAxisAUText);
                            }
                            else
                            {
                                ImString semiMajorAxisBothText = new ImString($"Semi-Major Axis: {semiMajorAxisKm} km / {semiMajorAxisAU:F3} AU");
                                ImGui.Text(semiMajorAxisBothText);
                                ImString semiMinorAxisBothText = new ImString($"Semi-Minor Axis: {semiMinorAxisKm} km / {semiMinorAxisAU:F3} AU");
                                ImGui.Text(semiMinorAxisBothText);
                            }

                            if (selectedCelestial.Orbit.GetType().Name != "Elliptical")
                            {
                                ImString orbitTypeText = new ImString($"Orbit Type: {selectedCelestial.Orbit.GetType().Name}");
                                ImGui.Text(orbitTypeText);
                            }
                            ImGui.PopFont();
                        }
                                            
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
                            // If the bodyJson has Factoids, display them as a pseudo-bulleted list
                            if (bodyJson.Factoids != null && bodyJson.Factoids.Count > 0)
                            {
                                ImGui.Text(" ");
                                ImGui.SeparatorText("Factoids:"); ImGui.Text(" ");
                                foreach (var factoid in bodyJson.Factoids)
                                {
                                    // Uses the unicode bullet point character for factoids and then the factoid text to make wrapping work properly
                                    ImString factoidText = new ImString($"• {factoid}\n\n");
                                    ImGui.TextWrapped(factoidText);

                                    //ImGui.BulletText(factoid);
                                }
                            }
                            // If the bodyJson has a VisitedBy list, display it as a bulleted list
                            if (bodyJson.VisitedBy != null && bodyJson.VisitedBy.Count > 0)
                            {
                                ImGui.Text(" ");
                                ImGui.SeparatorText("Visited By:"); ImGui.Text(" ");
                                foreach (var visitor in bodyJson.VisitedBy)
                                {
                                    ImString visitorText = new ImString($"{visitor}");
                                    ImGui.BulletText(visitorText);
                                }
                                ImGui.Text(" ");
                            }
                            ImGui.Text(" ");
                        }
                    }
                    else
                    {
                        ImGui.Text("\nSelect a category and celestial body!");
                    }
                    
                    ImGui.PopFont();
                }
                else
                {
                    // Pushes font for category-level data display
                    PushTheFont(1.9f);
                    ImString categoryTitle = new ImString($"{selectedCategoryKey}");
                    ImGui.Text(categoryTitle);
                    ImGui.PopFont();
                    DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color

                    // Now makes Buttons that can < on > and < off > all orbit lines for all bodies in this category
                    
                    PushTheFont(1.2f);
                    ImGui.Text(" ");
                    ImString toggleCategoryOrbitsText = new ImString($"Toggle ALL Orbit Lines for {selectedCategoryKey}");
                    ImGui.Text(toggleCategoryOrbitsText); ImGui.Text(" ");
                    
                    // Makes two buttons, one which turns on all the orbit lines of the category bodies, and one which turns them all off. Uses the celestials stored in buttonsCatsTree.  It stores both parents and children.
                    ImString categoryOnText = new ImString("< On >");
                    if (ImGui.Button(categoryOnText))
                    { ToggleCategoryOrbits(selectedCategoryKey, true); }
                    ImGui.SameLine();

                    ImString categoryOffText = new ImString("< Off >");
                    if (ImGui.Button(categoryOffText))
                    { ToggleCategoryOrbits(selectedCategoryKey, false); }
                    ImGui.Text(" ");
                    
                    ImGui.PopFont();
                    // Category was just selected - show category-level data if available
                    CompendiumData? categoryData = null;
                    // Try to get the ListGroupsData container, checking system-specific first, then default
                    CompendiumData? listGroupsContainer = null;
                    
                    // Try system-specific key first
                    if (!bodyJsonDict.TryGetValue($"{systemName}.ListGroupsData", out listGroupsContainer))
                    {
                        // Fall back to default key
                        bodyJsonDict.TryGetValue("Compendium.ListGroupsData", out listGroupsContainer);
                    }
                    
                    // If ListGroupsData was found, try to get the specific category from it
                    if (listGroupsContainer?.ListGroupsData != null)
                    {
                        listGroupsContainer.ListGroupsData.TryGetValue(selectedCategoryKey, out categoryData);
                    }

                    // If category data was found, display its text and Wikipedia URL if available
                    if (categoryData != null)
                    {
                        PushTheFont(1.2f);
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                        if (categoryData.CatText != null)
                        {
                            ImGui.TextWrapped(categoryData.CatText);
                        }
                        // Only show Wikipedia URL if CatText is also present
                        if (categoryData.CatText != null)
                        {
                            ImGui.Text(" ");
                            if (categoryData.CatWikipediaUrl != null)
                            {
                                ImGui.Text("Wikipedia Url:");
                                ImString linkText = new ImString($"https://en.wikipedia.org/wiki/{categoryData.CatWikipediaUrl}");
                                ImGui.TextLinkOpenURL(linkText);
                                ImGui.Text(" ");
                            }
                        }
                        ImGui.PopFont();
                    }
                }
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
                //Console.WriteLine("=== Compendium - OnImmediateLoad START ===");

                // Gets the current working directory path (of the DLL)
                dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // Loads fonts from the Fonts folder
                LoadFonts(dllDir, fontSizeCurrent);

                if (dllDir != null && Directory.Exists(dllDir))
                {
                    // Load celestial body descriptions from JSON files - pass dllDir since LoadCompendiumJsonData will search subdirectories
                    LoadCompendiumJsonData(dllDir);
                }
                
               // Console.WriteLine("=== Compendium - OnImmediateLoad END ===");
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
                // Makes a single string containing all of the categories in buttonsCatsTree
                string allCategories = string.Join(", ", buttonsCatsTree.Keys);
                Console.WriteLine($"Compendium: All categories loaded: {allCategories}");
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