using Brutal.ImGuiApi;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{

    public partial class Compendium
    {
        public static string[] fontFiles = Array.Empty<string>();
        public static string[] fontNames = Array.Empty<string>();
        public static Dictionary<string, ImFontPtr> loadedFonts = new Dictionary<string, ImFontPtr>();

        public static void LoadFonts(string? dllDir, float fontSizeCurrent)
        {
            if (!string.IsNullOrEmpty(dllDir))
            {
                string fontsDir = Path.Combine(dllDir, "Fonts");
                
                if (Directory.Exists(fontsDir))
                {
                    // Get all .ttf and .otf files from Fonts folder
                    var ttfFiles = Directory.GetFiles(fontsDir, "*.ttf");
                    var otfFiles = Directory.GetFiles(fontsDir, "*.otf");
                    fontFiles = ttfFiles.Concat(otfFiles).ToArray();
                    
                    if (fontFiles.Length > 0)
                    {
                        unsafe
                        {
                            ImGuiIO* io = ImGui.GetIO();
                            if (io != null)
                            {
                                ImFontAtlasPtr atlas = io->Fonts;
                                fontNames = new string[fontFiles.Length];
                                
                                for (int i = 0; i < fontFiles.Length; i++)
                                {
                                    string fontPath = fontFiles[i];
                                    string fontName = Path.GetFileNameWithoutExtension(fontPath);
                                    fontNames[i] = fontName;
                                    
                                    if (File.Exists(fontPath))
                                    {
                                        ImString fontPathStr = new ImString(fontPath);
                                        ImFontPtr font = atlas.AddFontFromFileTTF(fontPathStr, fontSizeCurrent);
                                        loadedFonts[fontName] = font;
                                        Console.WriteLine($"Loaded font: {fontName}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No font files found in Fonts folder");
                    }
                }
                else
                { Console.WriteLine($"Fonts directory not found at: {fontsDir}"); }
            }
        }
    }
}
    