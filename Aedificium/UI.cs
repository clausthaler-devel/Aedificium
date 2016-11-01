using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.DataBinding;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public class GUI
    {
        /// <summary>
        /// Gets the ingame name of the building tool panel for a given BuildingInfo object
        /// </summary>
        public static string GetPanelNameForBuilding( PrefabInfo prefabInfo )
        {
            Profiler.Trace( "GetPanelForBuilding {0}", prefabInfo );
            
            if ( prefabInfo is BuildingInfo )
            {
                var buildingInfo = (BuildingInfo)prefabInfo;

                if ( buildingInfo.GetAI() is IntersectionAI )
                    return "RoadsIntersectionPanel";
                else
                    return buildingInfo.GetValue<String>( "m_UICategory" ) + "Panel";
            }

            if ( prefabInfo is TreeInfo )
            {
                return "LandscapingTreesPanel";
            }


            if ( prefabInfo is PropInfo )
            {
                var propInfo = (PropInfo)prefabInfo;
                Profiler.Trace( "Panel for Prop {0}", propInfo );
                var category = propInfo.GetValue<String>( "m_UIEditorCategory" );

                if ( category.EndsWith("GroundTiles" ) )
                    return "PropsGroundTilesPanel";
                if ( category == "PropsCommonLights" )
                    return "PropsLightsPanel";
                
                Profiler.Trace( "Category {0}", category );
                // Only use the first two words of the value of UIEditorCategory
                // which is a camelcased String like PropsBillboardsSpecializedBillboards
                // which will then go into PropsBillboardsPanel
                category = Regex.Replace( category, @"^([A-Z][a-z]+[A-Z][a-z]+).+", m => m.Groups[1].Value );
                Profiler.Trace( "New Category {0}", category );
                return category + "Panel";
            }

            Profiler.Warning( "Unsupported Prefab: {0}", prefabInfo );

            return null;
        }


        /// <summary>
        /// Gets the building tool panel for a given BuildingInfo object
        /// </summary>
        public static UIScrollablePanel GetPanelForBuilding( PrefabInfo prefabInfo )
        {
            Profiler.Trace( "GetPanelForBuilding {0}", prefabInfo );
            var name = GetPanelNameForBuilding( prefabInfo );

            if ( name == null )
                return null;

            Profiler.Trace( "Panel name {0}", name );
            var uipanel = UIView.GetAView().FindUIComponent<UIPanel>( name );
            var spanel = uipanel.Find<UIScrollablePanel>("ScrollablePanel");
            return spanel;
        }

        /// <summary>
        /// Creates a new 'Assembly-CSharp.ItemClass' object
        /// </summary>
        public ItemClass NewItemClass( ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level )
        {
            var ic = new ItemClass();
            ic.m_service = service;
            ic.m_subService = subService;
            ic.m_level = level;

            return ic;
        }

        public static UIButton CreateBuildingButton( PrefabInfo prefabInfo )
        {
            var targetPanel = GUI.GetPanelForBuilding( prefabInfo );

            if ( targetPanel != null )
            {
                var button = CreateBuildingButton( targetPanel, prefabInfo.name );
                UpdateBuildingButton( button, prefabInfo );
                return button;
            }

            return null;
        }

        public static UIButton CreateBuildingButton( UIComponent targetPanel, string buttonName )
        {
            var button = targetPanel.AddUIComponent<UIButton>();
            button.name = buttonName;
            button.normalBgSprite = "";
            button.size = new Vector2( 109, 100 );
            button.relativePosition = new Vector3( 0, 5, 0 );
            button.horizontalAlignment = UIHorizontalAlignment.Center;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.pivot = UIPivotPoint.MiddleCenter;
            button.tooltipAnchor = UITooltipAnchor.Anchored;
            button.isEnabled = true;
            button.eventClick += PrefabButtonClicked;
            button.eventMouseHover += PrefabButtonHovered;

            return button;
        }

        public static void UpdateBuildingButton( UIButton button, PrefabInfo prefabInfo )
        { 
            button.objectUserData = prefabInfo;
            button.atlas = prefabInfo.m_Atlas;
            button.normalFgSprite   = prefabInfo.m_Thumbnail;
            button.focusedFgSprite  = prefabInfo.m_Thumbnail + "Focused";
            button.hoveredFgSprite  = prefabInfo.m_Thumbnail + "Hovered";
            button.pressedFgSprite  = prefabInfo.m_Thumbnail + "Pressed";
            button.disabledFgSprite = prefabInfo.m_Thumbnail + "Disabled";

            if ( prefabInfo.m_Thumbnail == null || prefabInfo.m_Thumbnail == "" )
                button.normalFgSprite = "ToolbarIconProps";

            string localizedTooltip = prefabInfo.GetLocalizedTooltip();
            int hashCode = TooltipHelper.GetHashCode(localizedTooltip);
            UIComponent tooltipBox = GeneratedPanel.GetTooltipBox(hashCode);

            button.tooltip = localizedTooltip;
            button.tooltipBox = tooltipBox;
        }

        static void PrefabButtonClicked( UIComponent component, UIMouseEventParameter eventParam )
        {
            if ( component.objectUserData is BuildingInfo )
            {
                var buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
                buildingTool.m_prefab = (BuildingInfo) component.objectUserData;
                buildingTool.m_relocate = 0;
            }
            if ( component.objectUserData is TreeInfo )
            {
                var treeTool = ToolsModifierControl.SetTool<TreeTool>();
                treeTool.m_prefab = (TreeInfo) component.objectUserData;
            }
            if ( component.objectUserData is PropInfo )
            {
                var propTool = ToolsModifierControl.SetTool<PropTool>();
                propTool.enabled = true;
                propTool.m_prefab = (PropInfo) component.objectUserData;
            }
        }

        static void PrefabButtonHovered( UIComponent component, UIMouseEventParameter eventParam )
        {
            var prefab = (PrefabInfo)component.objectUserData;
            var tooltipBoxa = UIView.GetAView().FindUIComponent<UIPanel>("InfoAdvancedTooltip");
            var tooltipBox = UIView.GetAView().FindUIComponent<UIPanel>("InfoAdvancedTooltipDetail");
            var spritea = tooltipBoxa.Find<UISprite>("Sprite");
            var sprite = tooltipBox.Find<UISprite>("Sprite");
            sprite.atlas = prefab.m_Atlas;
            spritea.atlas = prefab.m_Atlas;
        }


    }
}
