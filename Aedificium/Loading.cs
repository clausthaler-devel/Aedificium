using System.Linq;
using ICities;
using ColossalFramework.UI;
using ColossalFramework.IO;
using ColossalFramework.Steamworks;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public class Loading : LoadingExtensionBase
    {
        public override void OnLevelLoaded( LoadMode mode )
        {
            base.OnLevelLoaded( mode );

            if ( mode != LoadMode.LoadGame && mode != LoadMode.NewGame )
                return;

            PopupPanel.Initialize();

            Steam.workshop.eventWorkshopItemInstalled += ( PublishedFileId packageId ) => 
            {
                try
                {
                    var packagePath = Path.Combine( Options.workshopPath, packageId.ToString() );
                    Profiler.Trace( "Loading Package {0} from {1}", packageId, packagePath );

                    List<PrefabInfo> prefabs;

                    if ( IngamePackageImporter.ImportPackage( packagePath, out prefabs ) )
                    {
                        PopupPanel.Show( prefabs[0] );
                        foreach ( var p in prefabs )
                            GUI.CreateBuildingButton( p );
                    }
                }
                catch ( Exception e)
                {
                    Profiler.Error( "Error installing package " + packageId.ToString(), e );
                }
            };

        }


        //public override void OnLevelUnloading()
        //{
        //}

        //public override void OnReleased()
        //{
        //}
    }
}
