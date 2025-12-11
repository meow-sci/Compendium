using Brutal.ImGuiApi;
using KSA;
using StarMap.API;
using System;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    public partial class Compendium
    {
        public void PrintSelectedCategoryByKey(string categoryKey)
        {
            try
            {                
                if (string.IsNullOrEmpty(categoryKey) || buttonsCatsTree == null || !buttonsCatsTree.ContainsKey(categoryKey))
                {
                    ImGui.Text("No data for this category");
                    return;
                }

                // Get the category data from buttonsCatsTree
                var categoryData = buttonsCatsTree[categoryKey];
            
            if (categoryData.Count > 0)
            {
                
                // Iterate through each parent body in this category
                foreach (var parentEntry in categoryData)
                {
                    string parentBodyId = parentEntry.Key;
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
                                justSelected = parentBodyId;
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
                                                justSelected = childId;
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
                                        justSelected = childId;
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
                            justSelected = parentBodyId;
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
    }
}