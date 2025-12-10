using Brutal.ImGuiApi;
using System;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    public partial class Compendium
    {
        public void PrintSelectedCategoryByKey(string categoryKey)
        {
            var logFile = @"C:\temp\compendium_debug.log";
            
            try
            {
                System.IO.File.AppendAllText(logFile, $"PrintSelectedCategoryByKey called with: '{categoryKey}'\n");
                
                if (string.IsNullOrEmpty(categoryKey) || buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey))
                {
                    System.IO.File.AppendAllText(logFile, $"Category check failed - IsEmpty:{string.IsNullOrEmpty(categoryKey)}, TreeNull:{buttonsCatsTree == null}, HasKey:{buttonsCatsTree?.ContainsKey(categoryKey)}\n");
                    ImGui.Text("No data for this category");
                    return;
                }

                // Get the category data from buttonsCatsTree
                var categoryData = buttonsCatsTree[categoryKey];
                System.IO.File.AppendAllText(logFile, $"Category data count: {categoryData.Count}\n");
            
            if (categoryData.Count > 0)
            {
                System.IO.File.AppendAllText(logFile, "Starting foreach loop over categoryData\n");
                
                // Iterate through each parent body in this category
                foreach (var parentEntry in categoryData)
                {
                    string parentBodyId = parentEntry.Key;
                    System.IO.File.AppendAllText(logFile, $"  Processing body: {parentBodyId}\n");
                    var parentData = (Dictionary<string, object>)parentEntry.Value;
                    var childrenIds = (List<string>)parentData["Children"];
                    System.IO.File.AppendAllText(logFile, $"    Children count: {childrenIds.Count}\n");
                    
                    if (childrenIds.Count > 0)
                    {
                        // Has children, show as collapsing header with black background
                        PushTheColor("blackstuff");

                        ImString headerLabel = new ImString($"      {parentBodyId} ({childrenIds.Count} satellites)");
                        if (ImGui.CollapsingHeader(headerLabel))
                        {
                            ImGui.Indent();
                            
                            // Add the parent celestial as a selectable entry first
                            ImString parentId = new ImString($"          ({parentBodyId})##parent_{parentBodyId}");
                            bool isParentSelected = selectedCelestialId == parentBodyId;
                            
                            if (isParentSelected)
                            { PushTheColor("ltblue"); }
                            
                            if (ImGui.Selectable(parentId, isParentSelected))
                            { selectedCelestialId = parentBodyId; }
                            
                            if (isParentSelected)
                            { ImGui.PopStyleColor(1); }
                            
                            ImGui.Separator();
                            
                            // Then show the children
                            foreach (var childId in childrenIds)
                            {
                                ImString childIdStr = new ImString($"          {childId}##child_{childId}");
                                bool isChildSelected = selectedCelestialId == childId;
                                
                                if (isChildSelected)
                                { PushTheColor("ltblue"); }
                                
                                if (ImGui.Selectable(childIdStr, isChildSelected))
                                { selectedCelestialId = childId; }
                                
                                if (isChildSelected)
                                { ImGui.PopStyleColor(1); }
                            }
                            
                            ImGui.Unindent();
                        }
                        
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
                        { selectedCelestialId = parentBodyId; }

                        if (isCelestialSelected)
                        { ImGui.PopStyleColor(1); }
                    }
                }
            }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logFile, $"PrintSelectedCategoryByKey EXCEPTION: {ex.Message}\n{ex.StackTrace}\n");
                ImString errorMsg = new ImString($"Error: {ex.Message}");
                ImGui.Text(errorMsg);
            }
        }
    }
}