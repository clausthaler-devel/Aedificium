using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.DataBinding;
using MoreBeautification;
using ICities;

namespace Aedificium
{
    public static class UI
    {
        public static void NotifyDownload( IEnumerable<PrefabInfo> prefabs )
        {
            Profiler.Trace( "Notify user of {0} new prefabs", prefabs.Count() );
            foreach ( PrefabInfo prefab in prefabs )
                NotifyDownload( prefab );
        }

        public static void NotifyDownload( PrefabInfo prefab )
        {
            Profiler.Info( "Notify user of new prefab: {0}", prefab.name );
            var list = UIView.GetAView().GetComponentsInChildren<ChirpPanel>();

            if ( list.Count() > 0 )
            {
                var panel = list[0];
                panel.AddMessage( new ChirperMessage( prefab ) );

                var msgsPanel = panel.GetFieldValue<UIScrollablePanel>("m_Container");
                var msgPanel = msgsPanel.components[ msgsPanel.components.Count() - 1];
                var msgButton = (UILabel) msgPanel.components[ 1 ];
                msgButton.clipChildren = false;

                var button = msgButton.AddUIComponent<UIButton>();
                button.height = 75; button.width = 75;
                button.relativePosition = new Vector3( 5, 20 );
                button.objectUserData = prefab;
                button.atlas = prefab.m_Atlas;
                button.normalFgSprite = prefab.m_Thumbnail;
                button.focusedFgSprite = prefab.m_Thumbnail + "Focused";
                button.hoveredFgSprite = prefab.m_Thumbnail + "Hovered";
                button.pressedFgSprite = prefab.m_Thumbnail + "Pressed";
                button.disabledFgSprite = prefab.m_Thumbnail + "Disabled";

                if ( prefab.m_Thumbnail == null || prefab.m_Thumbnail == "" )
                    button.normalFgSprite = "ToolbarIconProps";

                string localizedTooltip = prefab.GetLocalizedTooltip();
                int hashCode = TooltipHelper.GetHashCode(localizedTooltip);
                UIComponent tooltipBox = GeneratedPanel.GetTooltipBox(hashCode);

                button.tooltip = localizedTooltip;
                button.tooltipBox = tooltipBox;

                button.eventClick += PrefabButtonClicked;
            }
        }

        public static void CreateButtons( IEnumerable<PrefabInfo> prefabs )
        {
            foreach ( var p in UIView.GetAView().GetComponentsInChildren<GeneratedScrollPanel>() )
                p.RefreshPanel();
        }

        public static void PrefabButtonClicked( UIComponent component, UIMouseEventParameter eventParam )
        {
            Profiler.Trace( "PrefabButtonClicked" );
            if ( component.objectUserData is BuildingInfo )
            {
                var buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
                buildingTool.m_prefab = (BuildingInfo) component.objectUserData;
                buildingTool.m_relocate = 0;
                Profiler.Trace( "PrefabButtonClicked( Building, {0} )", buildingTool.m_prefab.name );
            }
            if ( component.objectUserData is TreeInfo )
            {
                var treeTool = ToolsModifierControl.SetTool<TreeTool>();
                treeTool.m_prefab = (TreeInfo) component.objectUserData;
                Profiler.Trace( "PrefabButtonClicked( Tree, {0} )", treeTool.m_prefab.name );
            }
            if ( component.objectUserData is PropInfo )
            {
                var propTool = ToolsModifierControl.SetTool<PropTool>();
                propTool.enabled = true;
                propTool.m_prefab = (PropInfo) component.objectUserData;
            }
        }
    }

}
