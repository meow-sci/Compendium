using Brutal.ImGuiApi;
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
        private static string? dllDir;
        private static float fontSizeCurrent = 30f;
        private static float windowOpacity = 1.0f;
        private static bool CompendiumWindow = true;
        private static int selectedFontIndex = 0;
        private static int previousFontIndex = -1;
        private static int selectedCategoryIndex = -1;
        private static string selectedCategoryKey = "None";
        private static string showWindow = "None"; // "None", "Category", "Celestial", "Terms"
        private static bool showFontSettings = false;
        private static bool showOrbitLineSettings = false;
        private static Dictionary<string, bool>? showOrbitGroupColor;
        private static bool showOrbitCategoryColor = false;
        private static string selectedCelestialId = "Compendium Astronomicals Database";
        private static float mainContentWidth = 400f;
        private static List<string> categoryKeys = new List<string>();
        //private static string justSelected = "";
        private static string systemName = Universe.CurrentSystem?.Id ?? "Dummy";
        private static Dictionary<string, CompendiumData> bodyJsonDict = new Dictionary<string, CompendiumData>();
        private static string? parentDir;
        private static bool processedBodyJsonDict = false;
        private static StellarBody? worldSun = Universe.WorldSun;

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

            Celestial? selectedCelestial = null;
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
                } else if (!processedBodyJsonDict)
                {
                    AttachInfoBodyJsonDict();
                    processedBodyJsonDict = true;

                    // now that the full jsonBodyDict is processed, for debugging purposes export it as a json named "ProcessedBodyJsonDict.json" in the Compendium folder
                    //string processedJsonPath = Path.Combine(parentDir ?? "", "ProcessedBodyJsonDict.json");
                    //string processedJsonContent = JsonSerializer.Serialize(Compendium.bodyJsonDict, new JsonSerializerOptions { WriteIndented = true });
                    //File.WriteAllText(processedJsonPath, processedJsonContent);
                }
                
                // Build category tree on first render when Universe is loaded, or rebuild if it was built without celestials
                if (buttonsCatsTree == null || buttonsCatsTree.Count == 0 || !Compendium.categoriesBuiltWithCelestials)
                {
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

                    // // for debugging purposes writes to console each body in each category
                    // if (buttonsCatsTree != null)
                    // {
                    //     foreach (var category in buttonsCatsTree)
                    //     {
                    //         Console.WriteLine($"Category: {category.Key}");
                    //         foreach (var parentEntry in category.Value)
                    //         {
                    //             if (parentEntry.Key == "Data") continue;
                    //             Console.WriteLine($"  Parent: {parentEntry.Key}");
                    //             var parentEntryData = (Dictionary<string, object>)parentEntry.Value;
                    //             var childrenIds = (List<string>)parentEntryData["Children"];
                    //             foreach (var childId in childrenIds)
                    //             {
                    //                 Console.WriteLine($"    Child: {childId}");
                    //             }
                    //         }
                    //     }
                    // }
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
                    if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("Font Settings");
                            ImGui.EndTooltip();
                        }
                    ImGui.SameLine();

                    // Makes a Terms button AND a button which focuses on the Sun but make it so that it displays on the same line as the font settings arrow button, with them on the right side of the window.
                    // Make it so that the first button is the S button, and the second is the D button for Definitions/Terms (all the way on the right)
                    float buttonMWidth = ImGui.CalcTextSize(" MN ").X;
                    float buttonSWidth = ImGui.CalcTextSize(" FC ").X;
                    float buttonDWidth = ImGui.CalcTextSize(" TD ").X;
                    
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonMWidth - buttonSWidth - buttonDWidth - 8); // 8 for spacing

                    // Makes a button to make the display window go back to the original state on startup
                    if (ImGui.Button(" M ##MainButton", new float2(buttonMWidth, 0)))
                    {
                        showWindow = "None";
                        selectedCategoryIndex = -1;
                        selectedCategoryKey = "None";
                        selectedCelestial = null;
                        selectedCelestialId = "Compendium Astronomicals Database";
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"Main Menu Screen");
                        ImGui.EndTooltip();
                    }
                    
                    // Makes the FC button to focus on the Sun.  Starts by putting the cursor at the right location.
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonSWidth - buttonDWidth -4 );

                    if (ImGui.Button(" S ##SunButton", new float2(buttonSWidth, 0)))
                    {
                        // Focus camera on the Sun
                        var sun = Universe.WorldSun;
                        if (sun != null)
                        {
                            KSA.Universe.MoveCameraTo(sun);
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"Focus Camera on Star - {Universe.WorldSun.Id}");
                        ImGui.EndTooltip();
                    }
                    // Now make the D button for Definitions/Terms all the way to the right
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonDWidth);

                    if (ImGui.Button(" D ##TermsButton", new float2(buttonDWidth, 0)))
                    {
                        showWindow = "Terms";
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Definitions");
                        ImGui.EndTooltip();
                    }

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
                        
                        ImGui.Separator();
                        ImGui.SliderFloat("Size", ref fontSizeCurrent, 16f, 46f);
                        ImGui.SliderFloat("Opacity", ref windowOpacity, 0.1f, 1.0f);
                        ImGui.Separator();
                        ImGui.Text(" ");
                        // Display the selected font name
                        ImString selectedText = new ImString($"Selected: {fontNames[selectedFontIndex]}");
                        ImGui.Text(selectedText); ImGui.Text(" ");
                        PopTheFont();
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
                ImGui.Text("\nAstronomicals Categories:");
                
                // Pop the large font and push smaller font for category buttons
                PopTheFont();
                PushTheFont(0.75f);
                
                // Makes the category buttons by using which keys exist in the buttonsCatsTree dictionary,
                // using the categoryKeys list made just after making buttonCatsTree
                
                List<string> sortedCategoryKeys = new List<string>(categoryKeys);
                
                
                // IF there is a category named 'Other', make sure it is the last entry - but only if it exists in sortedCategoryKeys.
                // Otherwise everything else is sorted alphabetically.
                sortedCategoryKeys.Sort();
                // If there is a Planets category, make it the first entry.
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
                        selectedCelestial = null;
                        selectedCelestialId = "Category";
                        showWindow = "Category";
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

                PopTheFont();
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

                

                // First check if the category selected is 'Terms' and make the terms information display
                if (showWindow == "Terms")
                {
                    selectedCategoryIndex = -2;
                    PrintTermsCategory();
                    ImGui.EndChild(); // End side pane
                    ImGui.End();
                    return;
                }

                // If no category is selected yet, show a placeholder message
                if (showWindow == "None")
                {
                    PushTheFont(2f);
                    ImGui.Text("Compendium");
                    DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                    PopTheFont();
                    PushTheFont(1.4f);
                    ImGui.Text("\n\n\nAstronomicals Information\n\nPlease select a category, astronomical, or option!\n");

                    PopTheFont();
                    //ImTextureDataPtr photoPath = Path.Combine(parentDir ?? "", "Photos", "KSA_Logo.png");
                    //Brutal.ImGuiApi.ImTextureRef myTextureRef = (Path.Combine(parentDir ?? "", "Photos", "KSA_Logo.png"));
                    //ImGui.ImageWithBg(myTextureRef, new float2(sidePaneWidth - 20f, (sidePaneWidth - 20f) * 0.6f));
                    // Puts up a logo image if it exists in the Photos folder - if we can figure out how to do that



                    // var texture = new TextureAsset(
                    // "Content/Compendium/Photos/lol.jpg",
                    // new(new StbTexture.LoadSettings { ForceChannels = 4 }));
                    // var renderer = KSA.Program.GetRenderer();
                    // SimpleVkTexture vkTex;
                    // using (var stagingPool = renderer.Device.CreateStagingPool(renderer.Graphics, 1))
                    // {
                    // vkTex = new SimpleVkTexture(renderer.Device, stagingPool, texture);
                    // }
                    // var sampler = renderer.Device.CreateSampler(Presets.Sampler.SamplerPointClamped, null);
                    // texID = ImGuiBackend.Vulkan.AddTexture(sampler, vkTex.ImageView);

                    // Gets the current pane height and sets the image size to fit within the side pane while maintaining aspect ratio

                    // Checks if the image texture was loaded successfully
                

                    float paneHeight = ImGui.GetContentRegionAvail().Y;
                    float imageSize = Math.Min(sidePaneWidth - 20f, paneHeight);

                    ImGui.Image(texID, new(imageSize, imageSize), new(0f, 0f), new(1f, 1f));



                    ImGui.EndChild(); // End side pane
                    ImGui.End();
                    return;
                }

                // Next if regular bodies if selected, then later in 'else' meaning the category was just selected, make special text for the category and it's information / data.
                if (showWindow == "Celestial")
                {
                    ImGui.Separator();
                    // pushes a larger font only for the selected celestial id display
                    PushTheFont(1.7f);
                    ImString selectedIdText = new ImString($"{selectedCelestialId}");
                    ImGui.Text(selectedIdText);
                    PopTheFont();
                    PushTheFont(1);
                    ImGui.Text(" ");
                    
                    // Find the selected celestial object from Universe.WorldSun tree
                    
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
                    if (selectedCelestialId != "None")
                    {
                        if (selectedCelestial != null)
                        {
                        var celestial = selectedCelestial;
                        IReadOnlyList<IOrbiter>? children = null;
                        if (celestial is IParentBody parentBody && parentBody.Children != null)
                        {
                            children = parentBody.Children;
                        }

                        // Show URL link if JSON data with URL is available
                        if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.WikipediaUrl))
                        { 
                            ImString linkText = new ImString($"https://en.wikipedia.org/wiki/{bodyJson.WikipediaUrl}");
                            ImString labelText = new ImString($"Wikipedia Page: {bodyJson.WikipediaUrl}");
                            ImGui.TextLinkOpenURL(labelText, linkText);
                        }
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                        // Mean Radius
                        if (bodyJson != null && !float.IsNaN(bodyJson.RadiusKm))
                        { ImString radiusText = new ImString($"Mean Radius: {bodyJson.RadiusKm} km");
                          ImGui.Text(radiusText); }
                        else { ImGui.Text("Mean Radius: N/A"); }
                        // Mass
                        if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.MassText))
                        { ImGui.Text(bodyJson.MassText); }
                        else
                        { ImGui.Text("Mass: N/A"); }
                        // Surface Gravity
                        if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.GravityText))
                        { ImGui.Text(bodyJson.GravityText); }
                        else
                        { ImGui.Text("Gravity (Surface): N/A"); }
                        // Escape Velocity
                        if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.EscapeVelocityText))
                        { ImGui.Text(bodyJson.EscapeVelocityText); }
                        else
                        { ImGui.Text("Escape Velocity: N/A"); }

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
                            PushTheFont(1.0f);
                            ImGui.Text(" ");             
                            // Axial Tilt
                            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.ThisTiltText))
                            { ImGui.Text(bodyJson.ThisTiltText); }
                            else
                            { ImGui.Text("Axial Tilt: N/A"); }
                            // Eccentricity
                            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.EccentricityText))
                            { ImGui.Text(bodyJson.EccentricityText); }
                            else
                            { ImGui.Text("Eccentricity: N/A"); }
                            // Inclination
                            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.InclinationText))
                            { ImGui.Text(bodyJson.InclinationText); }
                            else
                            { ImGui.Text("Inclination: N/A"); }
                            // Orbital Period
                            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.OrbitalPeriod))
                            { ImGui.Text("Orbital period: " + bodyJson.OrbitalPeriod); }
                            else
                            { ImGui.Text("Orbital Period: N/A"); }
                            // Sidereal Period / Tidal Lock
                            if (bodyJson != null)
                            {
                                if (bodyJson.TidalLockText == "False" && !string.IsNullOrEmpty(bodyJson.SiderealPeriodText))
                                { ImGui.Text(bodyJson.SiderealPeriodText); }
                                else if (bodyJson.TidalLockText == "True" || bodyJson.TidalLockText == "Tidally locked rotation")
                                { ImGui.Text(bodyJson.TidalLockText); }
                                else 
                                { ImGui.Text("Sidereal Period: N/A"); }
                            }
                            // Semi-Major Axis / Semi-Minor Axis / Orbit Type
                            if (bodyJson != null)
                            {
                                if (!string.IsNullOrEmpty(bodyJson.SemiMajorAxisText))
                                { ImGui.Text(bodyJson.SemiMajorAxisText); }
                                else
                                { ImGui.Text("Semi-Major Axis: N/A"); }

                                if (!string.IsNullOrEmpty(bodyJson.SemiMinorAxisText))
                                { ImGui.Text(bodyJson.SemiMinorAxisText); }
                                else
                                { ImGui.Text("Semi-Minor Axis: N/A"); }

                                if (!string.IsNullOrEmpty(bodyJson.OrbitTypeText) && bodyJson.OrbitTypeText != "Elliptical")
                                { ImGui.Text(bodyJson.OrbitTypeText); }
                            }

                            // Atmosphere Height & Sea Level Pressure
                            if (bodyJson != null)
                            {
                                if (bodyJson.HasAtmosphere)
                                {
                                    // Atmosphere height
                                    if (!string.IsNullOrEmpty(bodyJson.AtmosphereHeightText))
                                    { ImGui.Text(bodyJson.AtmosphereHeightText); }
                                    else
                                    { ImGui.Text("Has Atmosphere"); }
                                    // Sea Level Pressure
                                    if (!string.IsNullOrEmpty(bodyJson.SLPressureText))
                                    { ImGui.Text(bodyJson.SLPressureText); }
                                    else
                                    { ImGui.Text("Sea Level Pressure: N/A"); }

                                }
                                else
                                { ImGui.Text("Atmosphere: None"); }
                            }



                            // Sphere of Influence
                            if (bodyJson != null && !string.IsNullOrEmpty(bodyJson.SphereOfInfluenceText))
                            { ImGui.Text(bodyJson.SphereOfInfluenceText); }
                            else
                            { ImGui.Text("Sphere of Influence: N/A"); }

                            // current speed

                            ImGui.Text("Current Speed: " + DistanceReference.FromMeters(celestial.OrbitalSpeed).ToNearestPerSecond());
  
                            PopTheFont();
                        }
                                            
                        DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                        ImGui.Text(" ");
                        // Makes a button to focus on the selected celestial objeect, but does not put a newline after it
                        ImString focusText = new ImString($"Focus Camera on {celestial.Id}");
                        if (ImGui.Button(focusText))
                        {
                            KSA.Universe.MoveCameraTo(celestial);
                        }
                        ImGui.SameLine(); ImGui.Text(" | "); ImGui.SameLine();
                        // Tries to make a button which toggles on/off the orbit lines for the selected celestial object
                        ImString orbitText = new ImString("Orbit Line"); 
                        if (ImGui.Button(orbitText))
                        {
                            celestial.ShowOrbit = !celestial.ShowOrbit;
                        }
                        ImGui.SameLine();
                        // Makes a checkbox that has an x to show the current state of the orbit line visibility
                        ImGui.Checkbox("##orbitCheckbox", ref celestial.ShowOrbit);

                        // Gets the current vessel ID as a string if there is a controlled vehicle.
                        string thisVehicle = Program.ControlledVehicle != null ? Program.ControlledVehicle.Id.ToString() : "None";

                        // Makes a button to set the current celestial as the Target for the currently controlled vehicle, if there is one.
                        ImString targetText = new ImString($"Select {celestial.Id} as Target");
                        if (ImGui.Button(targetText))
                        {
                            // Toggles setting or unsetting the target for the controlled vehicle
                            if (thisVehicle != "None")
                            {
                                if (!selectedCelestial.TargetOfControlledVehicle)
                                { KSA.Universe.SetTargetCommand(thisVehicle, celestial.Id.ToString()); }
                                else
                                {  KSA.Universe.UnsetTargetCommand(thisVehicle); }
                            }
                        }

                        // If the selected celestial is targeted by the controlled vehicle show indicator text
                        if (thisVehicle != "None" && selectedCelestial.TargetOfControlledVehicle)
                        {
                            ImGui.SameLine();
                            //ImString targetIndicatorText = new ImString(" >> Current Target <<");
                            //ImGui.TextColored(new Brutal.Numerics.float4(0.0f, 1.0f, 0.0f, 1.0f), targetIndicatorText);
                            // test indicator button colored red, which does nothing
                            ImGui.SameLine();
                            ImString colorbuttontext = new ImString(" >> Current Target << ##colorbutton");
                            ImGui.PushStyleColor(ImGuiCol.Button, new Brutal.Numerics.float4(0.0f, 0.0f, 0.0f, 1.0f));
                            ImGui.PushStyleColor(ImGuiCol.Text, new Brutal.Numerics.float4(0.0f, 1.0f, 0.0f, 1.0f));
                            if (ImGui.Button(colorbuttontext))
                            {
                                KSA.Universe.UnsetTargetCommand(thisVehicle);
                            }
                            ImGui.PopStyleColor();
                            ImGui.PopStyleColor();
                        }
                        // If there is no controlled vehicle, show indicator text
                        else if (thisVehicle == "None")
                        {
                            ImGui.SameLine();
                            ImString noVesselText = new ImString(" ( No Controlled Vehicle )");
                            ImGui.TextColored(new Brutal.Numerics.float4(1.0f, 1.0f, 0.0f, 1.0f), noVesselText);
                        }

                        // If the selectedcelestial has moons/children, make a button which toggles all of their orbit lines on/off
                        if (children != null && children.Count > 0)
                        {
                            ImGui.Text(" ");
                            ImString toggleMoonsText = new ImString("Toggle All Satellite Body Orbits:  ");
                            ImGui.Text(toggleMoonsText); ImGui.SameLine();
                            // Makes two buttons, one which turns on all the orbit lines of the moons, and one which turns them all off
                            ImString moonsOnText = new ImString("< On >");
                            if (ImGui.Button(moonsOnText))
                            {
                                foreach (var child in children)
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
                                foreach (var child in children)
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
                        if (children != null)
                        {
                        foreach (var child in children)
                        {
                            if (child is Celestial cel)
                            {
                                // Try to get the OrbitLineGroup from the JSON data for this child celestial
                                CompendiumData? childBodyJson = null;

                                if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                    { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                if (childBodyJson != null && !string.IsNullOrEmpty(childBodyJson.OrbitLineGroup))
                                    { orbitLineGroups.Add(childBodyJson.OrbitLineGroup); }
                            }
                        }
                        }
                        if (orbitLineGroups.Count > 1)
                        {
                            ImGui.Text(" ");
                            ImString orbitGroupText = new ImString("Orbit Lines by Group:  ");
                            ImGui.Text(orbitGroupText);
                            ImGui.Separator();
                            foreach (var group in orbitLineGroups)
                            {
                                ImGui.BulletText($" "); ImGui.SameLine();
                                ImString groupOnText = new ImString($"< On >##groupOn_{group}");
            
                                if (ImGui.Button(groupOnText))
                                {
                                    if (children != null)
                                    foreach (var child in children)
                                    {
                                        if (child is Celestial cel)
                                        {
                                            // Get the OrbitLineGroup from JSON
                                            CompendiumData? childBodyJson = null;

                                            if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                                { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                            if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                                { cel.ShowOrbit = true; }
                                        }
                                    }
                                }
                                ImGui.SameLine();
                                ImString groupOffText = new ImString($"< Off >##groupOff_{group}");
                                if (ImGui.Button(groupOffText))
                                {
                                    if (children != null)
                                    foreach (var child in children)
                                    {
                                        if (child is Celestial cel)
                                        {
                                            // Get the OrbitLineGroup from JSON
                                            CompendiumData? childBodyJson = null;

                                            if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                                { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                            if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                                { cel.ShowOrbit = false; }
                                        }
                                    }
                                }
                                ImGui.SameLine();
                                if (showOrbitGroupColor == null)
                                    showOrbitGroupColor = new Dictionary<string, bool>();
                                if (!showOrbitGroupColor.ContainsKey(group))
                                    showOrbitGroupColor[group] = false;

                                if (ImGui.ArrowButton($"OrbitGroupColorArrow##{group}", showOrbitGroupColor[group] ? ImGuiDir.Up : ImGuiDir.Down))
                                { 
                                    // Toggles visibility of this group's color picker and sliders
                                    showOrbitGroupColor[group] = !showOrbitGroupColor[group]; 
                                    // Toggles off other groups' color pickers
                                    foreach (var otherGroup in orbitLineGroups)
                                    { if (otherGroup != group) { showOrbitGroupColor[otherGroup] = false; } }
                                }

                                ImGui.SameLine();
                                ImString groupText = new ImString($"{group}");
                                ImGui.Text(groupText);

                                if (showOrbitGroupColor[group])
                                {
                                    
                                    // Color picker for this orbit group
                                    ImGui.Separator();
                                    BigIndent(); ImGui.SameLine();ImGui.Text("Orbit Line Color:");
                                    BigIndent(); ImGui.SameLine();
                                    ImString colorPickerLabel = new ImString($"##colorPicker_{group}");
                                    if (ImGui.BeginCombo(colorPickerLabel, "Select Color", ImGuiComboFlags.WidthFitPreview))
                                    {
                                        foreach (var colorEntry in orbitLineColors)
                                        {
                                            ImString colorOptionLabel = new ImString($"{colorEntry.Key}");
                                            bool isSelected = false;
                                            if (ImGui.Selectable(colorOptionLabel, isSelected))
                                            {
                                                if (children != null)
                                                foreach (var child in children)
                                                {
                                                    if (child is Celestial cel)
                                                    {
                                                        // Get the OrbitLineGroup from JSON
                                                        CompendiumData? childBodyJson = null;

                                                        if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                                            { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                                        if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                                        {
                                                            if (cel.Orbit != null)
                                                            {
                                                                var orbitColor = cel.Orbit.OrbitLineColor;
                                                                orbitColor.RGB = new Brutal.Numerics.byte3(
                                                                    (byte)(colorEntry.Value.X * 255),
                                                                    (byte)(colorEntry.Value.Y * 255),
                                                                    (byte)(colorEntry.Value.Z * 255)
                                                                );
                                                                cel.Orbit.OrbitLineColor = orbitColor;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        ImGui.EndCombo();
                                    }
                                    // Now makes 3 sliders populated with the current RGB values of the orbit line color for this orbit group, which when moved adjust the color also
                                    // First, get the current color of the first celestial in this group to use as reference
                                    Brutal.Numerics.byte3? currentColor = null;
                                    if (children != null)
                                    foreach (var child in children)
                                    {
                                        if (child is Celestial cel)
                                        {
                                            // Get the OrbitLineGroup from JSON
                                            CompendiumData? childBodyJson = null;

                                            if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                                { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                            if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                            {
                                                if (cel.Orbit != null)
                                                {
                                                    currentColor = cel.Orbit.OrbitLineColor.RGB;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    if (currentColor != null)
                                    {
                                        float r = currentColor.Value.X / 255f;
                                        float g = currentColor.Value.Y / 255f;
                                        float b = currentColor.Value.Z / 255f;
                                        
                                        ImGui.Text(" "); BigIndent(); ImGui.Text("Adjust Color:");
                                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.25f); // Set item width
                                        
                                        BigIndent(); ImGui.SliderFloat($"R##{group}", ref r, 0f, 1f);
                                        BigIndent(); ImGui.SliderFloat($"G##{group}", ref g, 0f, 1f);
                                        BigIndent(); ImGui.SliderFloat($"B##{group}", ref b, 0f, 1f);
                                        ImGui.PopItemWidth();
                                        
                                        // After sliders, set the orbit line color to the new RGB values for all celestials in this group
                                        if (children != null)
                                        foreach (var child in children)
                                        {
                                            if (child is Celestial cel)
                                            {
                                                // Get the OrbitLineGroup from JSON
                                                CompendiumData? childBodyJson = null;

                                                if (!Compendium.bodyJsonDict.TryGetValue($"{systemName}.{cel.Id}", out childBodyJson))
                                                    { Compendium.bodyJsonDict.TryGetValue($"Compendium.{cel.Id}", out childBodyJson); }
                                                if (childBodyJson != null && childBodyJson.OrbitLineGroup == group)
                                                {
                                                    if (cel.Orbit != null)
                                                    {
                                                        var orbitColor2 = cel.Orbit.OrbitLineColor;
                                                        orbitColor2.RGB = new Brutal.Numerics.byte3(
                                                            (byte)(r * 255),
                                                            (byte)(g * 255),
                                                            (byte)(b * 255)
                                                        );
                                                        cel.Orbit.OrbitLineColor = orbitColor2;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    ImGui.Separator();
                                }
                            }
                        }
                        ImGui.Text(" ");

                    // Makes a dropdown color picker to set the orbit line color for this group.  The dropdown list is the entries in orbitLineColors dictionary.

                    // Puts all of the following color picking business inside a dropdown
                    if (ImGui.ArrowButton("OrbitSettingsArrow", showOrbitLineSettings ? ImGuiDir.Up : ImGuiDir.Down))
                    { showOrbitLineSettings = !showOrbitLineSettings; }

                    ImGui.SameLine();
                    ImGui.Text(" Orbit Line Color");

                    if (showOrbitLineSettings)
                    {
                        ImGui.Separator();
                        if (celestial.Orbit == null)
                        {
                            ImGui.Text("Orbit: N/A");
                        }
                        else
                        {
                        var orbit = celestial.Orbit;
                        BigIndent(); ImGui.SameLine(); ImGui.Text("Orbit Line Color:");
                        BigIndent(); ImGui.SameLine();
                        ImString colorPickerLabel = new ImString($"##colorPicker");
                        if (ImGui.BeginCombo(colorPickerLabel, "Select Color", ImGuiComboFlags.WidthFitPreview))
                        {
                            foreach (var colorEntry in orbitLineColors)
                            {
                                ImString colorOptionLabel = new ImString($"{colorEntry.Key}");
                                bool isSelected = false;
                                if (ImGui.Selectable(colorOptionLabel, isSelected))
                                {
                                    var orbitColor = orbit.OrbitLineColor;
                                    orbitColor.RGB = new Brutal.Numerics.byte3(
                                        (byte)(colorEntry.Value.X * 255),
                                        (byte)(colorEntry.Value.Y * 255),
                                        (byte)(colorEntry.Value.Z * 255)
                                    );
                                    orbit.OrbitLineColor = orbitColor;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        
                        // Makes 3 sliders populated with the current RGB values of the orbit line color for this celestial, which when moved adjust the color also
                        float r = orbit.OrbitLineColor.RGB.X / 255f;
                        float g = orbit.OrbitLineColor.RGB.Y / 255f;
                        float b = orbit.OrbitLineColor.RGB.Z / 255f;
                        ImGui.Text(" "); BigIndent(); ImGui.Text("Adjust Color:");
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.25f); // Set item width
                        BigIndent(); ImGui.SliderFloat("R", ref r, 0f, 1f);
                        BigIndent(); ImGui.SliderFloat("G", ref g, 0f, 1f);
                        BigIndent(); ImGui.SliderFloat("B", ref b, 0f, 1f);
                        ImGui.PopItemWidth();
                        // After sliders, set the orbit line color to the new RGB values
                        var orbitColor2 = orbit.OrbitLineColor;
                        orbitColor2.RGB = new Brutal.Numerics.byte3(
                            (byte)(r * 255),
                            (byte)(g * 255),
                            (byte)(b * 255)
                        );
                        orbit.OrbitLineColor = orbitColor2;
                        }
                    }
                        // After displaying the celestial properties, show JSON compendium data if available
                        if (bodyJson != null)
                        {
                            DrawBoldSeparator(2.0f, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White color
                            ImGui.Text(" ");
                            if (bodyJson.Text != null)
                                { ImGui.TextWrapped(bodyJson.Text); }
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
                    }
                    else
                        { ImGui.Text("\nSelect a category and celestial body!"); }
                    
                    PopTheFont();
                }
                if (showWindow == "Category")
                {
                    // This means category was selected but a body was not selected yet
                    // This is effectively the "category option / information" screen.

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

                    // Now on sameline makes an arrow button to show/hide the category-level orbit line color picker and sliders, and then make the color picker and sliders similar to the other times it was done above.
                    // In order to change the orbitlinecolors of each body in the category we need to iterate over all celestials in the loaded system's Universe.WorldSun.Children and apply the orbitlinecolor to each one of those which is in this category of the buttonsCatsTree.
                    if (ImGui.ArrowButton("CategoryOrbitColorArrow", showOrbitCategoryColor ? ImGuiDir.Up : ImGuiDir.Down))
                    { showOrbitCategoryColor = !showOrbitCategoryColor; }
                    ImGui.SameLine();
                    ImString categoryColorText = new ImString(" Set Orbit Line Color for Category Bodies");
                    ImGui.Text(categoryColorText);
                    if (showOrbitCategoryColor)
                    {
                        ImGui.Separator();
                        BigIndent(); ImGui.SameLine(); ImGui.Text("\nOrbit Line Color:");
                        BigIndent(); ImGui.SameLine();
                        ImString colorPickerLabel = new ImString($"##categoryColorPicker");
                        if (ImGui.BeginCombo(colorPickerLabel, "Select Color", ImGuiComboFlags.WidthFitPreview))
                        {
                            foreach (var colorEntry in orbitLineColors)
                            {
                                ImString colorOptionLabel = new ImString($"{colorEntry.Key}");
                                bool isSelected = false;
                                if (ImGui.Selectable(colorOptionLabel, isSelected))
                                {
                                    // Iterate over all celestials in the current system and set orbit line color if in this category
                                    if (KSA.Universe.WorldSun is IParentBody worldParent && worldParent.Children != null)
                                    {
                                        foreach (var celestial in worldParent.Children)
                                        {
                                            if (celestial is Celestial cel &&
                                                buttonsCatsTree != null &&
                                                buttonsCatsTree.ContainsKey(selectedCategoryKey) &&
                                                buttonsCatsTree[selectedCategoryKey].ContainsKey(celestial.Id))

                                            {
                                                var orbitColor = cel.Orbit.OrbitLineColor;
                                                orbitColor.RGB = new Brutal.Numerics.byte3(
                                                    (byte)(colorEntry.Value.X * 255),
                                                    (byte)(colorEntry.Value.Y * 255),
                                                    (byte)(colorEntry.Value.Z * 255)
                                                );
                                                cel.Orbit.OrbitLineColor = orbitColor;
                                            }
                                            
                                        }
                                    }
                                }
                            }
                            ImGui.EndCombo();
                        }
                        
                        // Makes 3 sliders populated with the current RGB values of the orbit line color for the first celestial found in this category, which when moved adjust the color also
                        // Declare and initialize currentColor before using it
                        Brutal.Numerics.byte3? currentColor = null;
                        // First, get the current color of the first celestial in this category to use as reference
                        if (KSA.Universe.WorldSun is IParentBody worldParent2 && worldParent2.Children != null)
                        {
                            foreach (var celestial in worldParent2.Children)
                            {
                                if (celestial is Celestial cel &&
                                    buttonsCatsTree != null &&
                                    buttonsCatsTree.ContainsKey(selectedCategoryKey) &&
                                    buttonsCatsTree[selectedCategoryKey].ContainsKey(celestial.Id))
                                {
                                    currentColor = cel.Orbit.OrbitLineColor.RGB;
                                    break;
                                }
                            }
                        }
                        if (currentColor != null)
                        {
                            float r = currentColor.Value.X / 255f;
                            float g = currentColor.Value.Y / 255f;
                            float b = currentColor.Value.Z / 255f;

                            ImGui.Text(" "); BigIndent(); ImGui.Text("Adjust Color:");
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * 0.25f); // Set item width

                            BigIndent(); bool changedR = ImGui.SliderFloat($"R##category", ref r, 0f, 1f);
                            BigIndent(); bool changedG = ImGui.SliderFloat($"G##category", ref g, 0f, 1f);
                            BigIndent(); bool changedB = ImGui.SliderFloat($"B##category", ref b, 0f, 1f);
                            ImGui.PopItemWidth();

                            // Only update if any slider was changed
                            if (changedR || changedG || changedB)
                            {
                                if (buttonsCatsTree != null && buttonsCatsTree.ContainsKey(selectedCategoryKey))
                                {
                                    var categoryEntry = buttonsCatsTree[selectedCategoryKey];
                                    if (categoryEntry is Dictionary<string, object> parentEntryDict &&
                                        KSA.Universe.WorldSun is IParentBody worldParent3 && worldParent3.Children != null)
                                    {
                                        foreach (var kvp in parentEntryDict)
                                        {
                                            string celestialId = kvp.Key;
                                            // Find the celestial by id in the world's children
                                            var celestial = worldParent3.Children.FirstOrDefault(c => c is Celestial cel && cel.Id == celestialId) as Celestial;
                                            if (celestial != null && celestial.Orbit != null)
                                            {
                                                var orbitColor2 = celestial.Orbit.OrbitLineColor;
                                                orbitColor2.RGB = new Brutal.Numerics.byte3(
                                                    (byte)(r * 255),
                                                    (byte)(g * 255),
                                                    (byte)(b * 255)
                                                );
                                                celestial.Orbit.OrbitLineColor = orbitColor2;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ImGui.PopFont();

                    // Add this at class level (private static):
                    // private static Dictionary<string, (float r, float g, float b)>? categoryOrbitColorDict;
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
                
                // Try to clean up ImGui state on exception
                try { ImGui.EndChild(); } catch { }
                try { ImGui.End(); } catch { }
            }

        }

        [StarMapImmediateLoad]
        public void OnImmediateLoad(Mod mod)
        {
            try
            {
                // Gets the current working directory path (of the DLL)
                dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // Loads fonts from the Fonts folder
                LoadFonts(dllDir, fontSizeCurrent);

                if (dllDir != null && Directory.Exists(dllDir))
                {
                    // Load celestial body descriptions from JSON files - pass dllDir since LoadCompendiumJsonData will search subdirectories
                    LoadCompendiumJsonData(dllDir);
                }

                // Loads photos found in the Photos folder
                RegisterPhotos();
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

                // Makes a single string containing all of the categories in buttonsCatsTree
                //string allCategories = string.Join(", ", buttonsCatsTree.Keys);
                //Console.WriteLine($"Compendium: All categories loaded: {allCategories}");
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