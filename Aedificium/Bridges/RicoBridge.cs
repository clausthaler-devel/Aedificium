using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using ICities;
using ColossalFramework;
using UnityEngine;
using ColossalFramework.Packaging;
using ColossalFramework.DataBinding;
using ColossalFramework.UI;

namespace Aedificium
{
    class RicoBridge : BridgeBase
    {
        public const ulong ModSteamId = 837734529;

        public RicoBridge() : base( ModSteamId )
        {
        }

        public bool RicofyPrefabs( List<PrefabInfo> prefabs )
        { 
            if ( !ModIsLoaded )
                return false;

            try
            {
                var asset = PackageManager.FindAssetByName(prefabs[0].name);
                var ricoDefPath = Path.Combine(Path.GetDirectoryName( asset.package.packagePath ), "PloppableRICODefinition.xml");

                if ( !File.Exists( ricoDefPath ) )
                    return true;

                var ricoDef = PloppableRICO.RICOReader.ParseRICODefinition( asset.package.packageName, ricoDefPath );

                if ( ricoDef == null )
                    return false;
                
                foreach ( var prefab in prefabs )
                { 
                        var category = new PloppableRICO.XMLManager().Invoke("AssignCategory", prefab);

                        var buildingData = new PloppableRICO.BuildingData
                            {
                            prefab = (BuildingInfo) prefab,
                            name = prefab.name,
                            category = (PloppableRICO.Category) category,
                            hasAuthor = true
                        };

                        PloppableRICO.XMLManager.prefabList.Add( buildingData );
                        PloppableRICO.XMLManager.prefabHash[buildingData.prefab] = buildingData;

                        foreach ( var buildingDef in ricoDef.Buildings )
                        {
                            var pf = FindPrefab( buildingDef, asset);

                            //Add asset author settings to dictionary.
                            if ( PloppableRICO.XMLManager.prefabHash.ContainsKey( pf ) )
                            {
                                PloppableRICO.XMLManager.prefabHash[pf].local = buildingDef;
                                PloppableRICO.XMLManager.prefabHash[pf].hasLocal = true;
                            }

                            if ( buildingData.local.ricoEnabled )
                                try
                                {
                                    new PloppableRICO.ConvertPrefabs().ConvertPrefab( buildingData.local, buildingData.name );
                                    var cat = UICategoryOf( buildingDef.service, buildingDef.subService );
                                    DrawRicoBuildingButton( buildingData.prefab, cat );
                                    RemoveVanillaUIButton( buildingData.prefab );

                                }
                                catch ( Exception e )
                                {
                                    Profiler.Error( "Cant convert prefab", e );
                                }
                        }
                    }

                    return true;
                
            }
            catch ( Exception e )
            {
                Profiler.Error( "Cant rerun rico", e );
            }

            return false;
        }

        static void RemoveVanillaUIButton( BuildingInfo prefab )
        {
            var uiView = UIView.GetAView();

            var refButton = uiView.FindUIComponent(prefab.name);

            if ( refButton != null )
            {
                try
                {
                    refButton.isVisible = false;
                    GameObject.Destroy( refButton.gameObject );
                    Profiler.Trace( "Vanilla Button Destroyed for {0}", prefab.name );
                }
                catch ( Exception ex )
                {
                    Profiler.Error( "RicoBridge.RemoveVanillaUIButton", ex );
                }
            }
        }

        BuildingInfo FindPrefab( PloppableRICO.RICOBuilding buildingDef, Package.Asset asset )
        {
            BuildingInfo pf;

            pf = PloppableRICO.Util.FindPrefab( buildingDef.name, asset.package.packageName );

            if ( pf == null )
            {
                try
                {
                    pf = PloppableRICO.XMLManager.prefabHash.Values
                        .Select( ( p ) => p.prefab )
                        .First( ( p ) => p.name.StartsWith( asset.package.packageName ) );
                }
                catch { }
            }

            if ( pf == null )
                Profiler.Error( String.Format( "Error while processing RICO - file {0}. ({1})", asset.package.packageName, "Building has not been loaded. Either it is broken, deactivated or not subscribed to." + buildingDef.name + " not loaded. (" + asset.package.packageMainAsset + ")" ) );

            return pf;
        }


        // From here on houses Rico
        static string[] Names = new string[]{
            "ResidentialLow", "ResidentialHigh", "CommercialLow", "CommercialHigh",
            "Office", "Industrial", "Farming", "Forest", "Oil", "Ore", "Leisure", "Tourist",        };

        static UIView uiview = UIView.GetAView();
        static private List<UIScrollablePanel> _BuildingPanels;
        
        static private List<UIScrollablePanel> BuildingPanels
        {
            get
            {
                if ( _BuildingPanels == null )
                    _BuildingPanels = Names.Select( n => uiview.FindUIComponent<UIScrollablePanel>( $"{n}Panel" ) ).ToList();
                
                return _BuildingPanels;
            }
        }

        static Dictionary<string, int> _panelIndices = new Dictionary<string, int>()
        {
            { "reslow", 0 }, { "reshigh", 1 }, { "comlow", 2 }, { "comhigh", 3 },
            { "office", 4 }, { "industrial", 5 }, { "farming", 6 }, { "oil", 7 },
            { "forest", 8 }, { "ore", 9 }, { "leisure", 10 }, { "tourist", 11 },
        };

        static UIScrollablePanel FindRicoPanel( string category )
        {
            if ( _panelIndices.ContainsKey( category ) )
                return BuildingPanels[_panelIndices[category] ];

            return null;
        }

        public static string UICategoryOf( string service, string subservice )
        {
            var category = "";
            if ( service == "" || subservice == "" )
                return "";

            switch ( service )
            {
                case "residential": category = subservice == "high" ? "reshigh" : "reslow"; break;
                case "commercial": category = subservice == "high" ? "comhigh" : "comlow"; break;
                case "office": category = "office"; break;
                case "industrial": category = subservice == "generic" ? "industrial" : subservice; break;
                case "extractor": category = subservice; break;
                default:
                    category = service; break;
            }
            return category;
        }

        public static bool DrawRicoBuildingButton( BuildingInfo prefab, string category  )
        {
            try
            {
                Profiler.Trace( "Drawing Button for {0} ( category: {1} )", prefab.name, category );
                var panel = FindRicoPanel( category );
                Profiler.Trace( "Drawing on panel " + panel?.name );
                if ( panel != null )
                    return DrawBuildingButtonOnPanel( prefab, panel );
            }
            catch ( Exception e )
            {
                Profiler.Error( "Can't attach BuildingButton\r\n{0}\r\n{1}", e.Message, e.StackTrace );
            }

            return false;
        }

        private static bool DrawBuildingButtonOnPanel( BuildingInfo prefab, UIScrollablePanel panel )
        {
            int _UIBaseHeight = 109;

            var BuildingButton = panel.AddUIComponent<UIButton>();

            BuildingButton.size = new Vector2( _UIBaseHeight, 100 ); //apply settings to building buttons. 
            BuildingButton.atlas = prefab.m_Atlas;

            BuildingButton.normalFgSprite = prefab.m_Thumbnail;
            BuildingButton.focusedFgSprite = prefab.m_Thumbnail + "Focused";
            BuildingButton.hoveredFgSprite = prefab.m_Thumbnail + "Hovered";
            BuildingButton.pressedFgSprite = prefab.m_Thumbnail + "Pressed";
            BuildingButton.disabledFgSprite = prefab.m_Thumbnail + "Disabled";

            if ( prefab.m_Thumbnail == null || prefab.m_Thumbnail == "" )
                BuildingButton.normalFgSprite = "ToolbarIconProps";

            BuildingButton.objectUserData = prefab;
            BuildingButton.horizontalAlignment = UIHorizontalAlignment.Center;
            BuildingButton.verticalAlignment = UIVerticalAlignment.Middle;
            BuildingButton.pivot = UIPivotPoint.TopCenter;

            //if ( Category == "education" )
            //    BuildingButton.verticalAlignment = UIVerticalAlignment.Bottom;

            string localizedTooltip = prefab.GetLocalizedTooltip();
            int hashCode = TooltipHelper.GetHashCode(localizedTooltip);
            UIComponent tooltipBox = GeneratedPanel.GetTooltipBox(hashCode);

            BuildingButton.tooltipAnchor = UITooltipAnchor.Anchored;
            BuildingButton.isEnabled = true;
            BuildingButton.tooltip = localizedTooltip;
            BuildingButton.tooltipBox = tooltipBox;
            BuildingButton.eventClick += UI.PrefabButtonClicked;
            return true;
        }
    }
}
