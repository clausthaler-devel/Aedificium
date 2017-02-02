using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColossalFramework.UI;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public class HotLoader
    {
        public static HotLoader Loader;
        public static void SetupWatcher()
        {
            try
            {
                SubscriptionManager.Register();
                HotloadManager.Register();
                CustomDeserializer.Hook();
                Loader = new HotLoader();
                //UIView.library.ShowModal<MessageBoxPanel>( "MessageBoxPanel" );

                // Even though both events fire when an install / download is made, 
                // it wont be loaded twice since eventWorkshopSubscriptionChanged
                // fires first and that HotLoad gracefully fails because the package files
                // haven't been downloaded by then
                PlatformService.workshop.eventWorkshopItemInstalled += ( PublishedFileId packageId ) => {
                    Profiler.Trace( "eventWorkshopItemInstalled {0}", packageId );
                    HotloadItem( packageId );
                };

                // need for deferred loading
                PlatformService.workshop.eventWorkshopSubscriptionChanged += ( PublishedFileId packageId, bool subscribed ) => {
                    Profiler.Trace( "eventWorkshopSubscriptionChanged {0} {1}", packageId, subscribed );
                    if ( subscribed )
                        HotloadItem( packageId );
                };
            }
            catch ( Exception ex )
            {
                Profiler.Error( "SetupWatcher", ex );
            }
        }

        public static void HotloadItem( PublishedFileId packageId, bool reloaded = false )
        {
            Profiler.Info( "New Workshop-Item installed: {0} (reloaded: {1})", packageId, reloaded );

            try
            {
                if ( Settings.instance.installSubscriptions )
                {
                    if ( Loader.HotloadPackage( packageId ) )
                    {
                        Profiler.Info( "Hotloaded package {0}", packageId );
                        Loader.PostInstall();
                        HotloadManager.ReportProcessed( packageId );
                    }
                    else
                    {
                        Profiler.Trace( "Hotload failed with {0} dependencies", AssetLoader.instance.notFoundIndirect.Count );

                        // in case of zero dependencies the loading really failed
                        if ( AssetLoader.instance.notFoundIndirect.Count == 0 )
                        {
                            Profiler.Trace( "no deps" );
                            // so we need to get if out of the dependency lists
                            HotloadManager.ReportProcessed( packageId );
                        }
                    }
                }
                else
                {
                    Profiler.Info( "Skipped package {0} due to settings", packageId );
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "HotloadItem", ex );
            }
            
        }

        private bool HotloadPackage( PublishedFileId packageId )
        {
            try
            {
                Profiler.Info( "Loading Package {0}", packageId );
                PackageManager.instance.LoadPackages( packageId );
                var package = PackageManager.GetPackage( packageId.ToString() );

                if ( package == null )
                    return false;
                //Profiler.Dump( package );
                return AssetLoader.instance.LoadCustomContent( package.FilterAssets( UserAssetType.CustomAssetMetaData ).ToList() );
            }
            catch ( Exception ex )
            {
                Profiler.Error( "Error loading package " + packageId.ToString(), ex );
                return false;
            }
        }

        void PostInstall()
        {
            try
            {
                UpdateTelemetry();
                UpdatePrefabs();
                UpdateUI();
                LowerBridges();
            }
            catch ( Exception ex )
            {
                Profiler.Error( "Error during PostInstall", ex );
            }
        }

        void UpdateUI()
        {
            UI.NotifyDownload( AssetLoader.instance.loadedPrefabs );
            UI.CreateButtons( AssetLoader.instance.loadedPrefabs );
        }
        void UpdatePrefabs()
        {
            BindPrefabs();
            RenderManager.Managers_CheckReferences();
            RenderManager.Managers_InitRenderData();
        }

        void LowerBridges()
        {
            if ( Settings.instance.enableRico )
                new RicoBridge().RicofyPrefabs( AssetLoader.instance.loadedPrefabs );
            new FindItBridge().UpdateAssets();
            new MoreBeautificationBridge().UpdateAssets();
        }

        void UpdateTelemetry()
        {
            if ( Singleton<TelemetryManager>.exists )
                UsedAssets.Refresh();

            Singleton<TelemetryManager>.instance.CustomContentInfo(
               buildingsCount: UsedAssets.instance.buildingAssets.Count(),
               propsCount: UsedAssets.instance.propAssets.Count(),
               treeCount: UsedAssets.instance.treeAssets.Count(),
               vehicleCount: UsedAssets.instance.vehicleAssets.Count() );
        }

        void BindPrefabs()
        {
            PrefabCollection<VehicleInfo>.BindPrefabs();
            PrefabCollection<BuildingInfo>.BindPrefabs();
            PrefabCollection<PropInfo>.BindPrefabs();
            PrefabCollection<TreeInfo>.BindPrefabs();
        }

        void PerformSubscriptions()
        {
            //Profiler.Trace( "PerformSubscriptions {0}", SubscribeToItems.Count() );
            //if ( Settings.settings.installMissingAssets )
            //{
            //    SubscribeToMissingAssets();
            //    SubscribeToItems.Clear();
            //}
        }

        void SubscribeToMissingAssets()
        {
            //Profiler.Trace( "SubscribeToMissingAssets {0}", SubscribeToItems.Count() );
            //var items = SubscribeToItems.ToList();

            //new Thread( () =>
            //    SubscribeToMissingAssets<BuildingInfo>( items )
            //).Start();
        }

        void SubscribeToMissingAssets<P>( List<string> customAssets ) where P : PrefabInfo
        {
            Profiler.Trace( "SubscribeToMissingAssetsThread {0} {1}", customAssets.Count, string.Join( ";", customAssets.ToArray() ) );
            try
            {
                foreach ( string name in customAssets )
                {
                    //Profiler.Trace( "name {0}", name );
                    if ( PrefabCollection<P>.FindLoaded( name ) == null )
                    {
                        // Profiler.Trace( "null" );
                        if ( name.matches( @"^\d{9}" ) )
                        {
                            //Profiler.Trace( "match" );
                            if ( ColossalFramework.PlatformServices.PlatformService.workshop.Subscribe( new PublishedFileId( Convert.ToUInt64( name.Substring( 0, 9 ) ) ) ) )
                            {
                                Profiler.Trace( "subscribed to {0}", name );
                            }
                            else
                            {
                                Profiler.Warning( "Cannot subscribe to {0}", name );
                            }
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                UnityEngine.Debug.LogException( e );
            }
        }
        //void foo()
        //{ 
        //    Profiler.Trace( "SetupWatcher()" );

        //    new AssetReport();
        //    UsedAssets.Create().Hook(); 
        //    PlatformService.workshop.eventWorkshopItemInstalled += ( PublishedFileId packageId ) => {
        //        Profiler.Trace( "eventWorkshopItemInstalled( {0} )", packageId );

        //        if ( !Settings.settings.hotloadSubscriptions )
        //            return;

        //        try
        //        {
        //            Profiler.Trace( "HL1" );
        //            PackageManager.instance.LoadPackages( packageId );
        //            Profiler.Trace( "HL2" );
        //            if ( AssetLoader.instance.HotloadPackage( packageId ) )
        //            {
        //                Profiler.Trace( "HL3" );

        //                var prefabs = Resources
        //                    .FindObjectsOfTypeAll<PrefabInfo>()
        //                    .Where( p => p.name.Contains( packageId.ToString() ) );

        //                Profiler.Trace( "Hotloaded: {0}", prefabs.Count() );

        //                Notifier.NotifyDownload( prefabs );
        //                Notifier.CreateButtons( prefabs );

        //                if ( Settings.settings.installDependencies )
        //                {
        //                    Profiler.Trace( "* install dependencies " );
        //                    foreach ( var s in SubscribeToItems )
        //                    {
        //                        Profiler.Trace( "s {0}", s );
        //                    }
        //                    // PerformSubscriptions();
        //                }
        //            }

        //            RenderManager.Managers_CheckReferences();
        //            RenderManager.Managers_InitRenderData();
        //            new FindItBridge().Call();
        //        }
        //        catch ( Exception ex )
        //        {
        //            Profiler.Error( "SetupWatcher()", ex );
        //        }
        //    };
        //}

        //public bool HotloadPackage( PublishedFileId packageId, bool preview = false )
        //{
        //    Profiler.Trace( "HotloadPackage( {0} )", packageId );

        //    try
        //    {
        //        PackageManager.instance.LoadPackages( packageId );

        //        var package = PackageManager.GetPackage( packageId.ToString() );

        //        Profiler.Trace( "package: {0}", package );

        //        if ( package != null )
        //        {

        //            var filteredAssets = EnabledAssetsFirst( package.FilterAssets( UserAssetType.CustomAssetMetaData ) );
        //            Profiler.Trace( "*A" );
        //            List<Package.Asset>[] queues = GetLoadQueues( filteredAssets );
        //            Profiler.Trace( "*B" );
        //            foreach ( List<Package.Asset> assets in queues )
        //            {
        //                Profiler.Trace( "Q" );
        //                foreach ( Package.Asset asset in assets )
        //                {
        //                    Profiler.Trace( "load asset: {0}", asset );
        //                    Load( asset, preview );
        //                }
        //            }

        //            if ( !preview )
        //            {
        //                Profiler.Trace( "Binding prefabs" );
        //            }

        //            return true;
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

    }
}
