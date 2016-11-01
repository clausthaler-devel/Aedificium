using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using ColossalFramework.Packaging;
using UnityEngine;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public sealed class AssetLoader
    {
        public static AssetLoader instance;
        HashSet<string> failedAssets = new HashSet<string>(), loadedProps = new HashSet<string>(), loadedTrees = new HashSet<string>(),
            loadedBuildings = new HashSet<string>(), loadedVehicles = new HashSet<string>(), loadedIntersections = new HashSet<string>();
        internal string currentFullName;

        PrefabInfo loadedPrefab;
        SteamHelper.DLC_BitMask notMask;
        int propCount, treeCount, buildingCount, vehicleCount;
        readonly bool loadEnabled = true, loadUsed = true;
        public bool hasStarted, hasFinished;
        internal const int yieldInterval = 200;
        internal HashSet<string> Props => loadedProps;
        internal HashSet<string> Trees => loadedTrees;
        internal HashSet<string> Buildings => loadedBuildings;
        internal HashSet<string> Vehicles => loadedVehicles;
        internal bool IsIntersection( string fullName ) => loadedIntersections.Contains( fullName );
        internal HashSet<string> SubscribeToItems = new HashSet<string>();

        public AssetLoader()
        {
            instance = this;
            hasStarted = hasFinished = false;
        }

        public void Setup()
        {
            notMask = ~SteamHelper.GetOwnedDLCMask();
        }

        public void Dispose()
        {
            failedAssets.Clear(); loadedProps.Clear(); loadedTrees.Clear(); loadedBuildings.Clear(); loadedVehicles.Clear(); loadedIntersections.Clear();
            instance = null; failedAssets = null; loadedProps = null; loadedTrees = null; loadedBuildings = null; loadedVehicles = null; loadedIntersections = null;
        }

        public PrefabInfo LoadPrefab( Package.Asset asset ) 
        {
            if ( PropTreeTrailer( asset ) || BuildingVehicle( asset, false ) )
            {
                if ( loadedPrefab != null )
                {
                    Profiler.Trace( "Loaded prefab {0} ", loadedPrefab.name );
                    loadedPrefab.InitializePrefab();

                    if ( loadedPrefab is BuildingInfo )
                        PrefabCollection<BuildingInfo>.BindPrefabs();
                    else if ( loadedPrefab is PropInfo )
                        PrefabCollection<PropInfo>.BindPrefabs();
                    else if ( loadedPrefab is TreeInfo )
                        PrefabCollection<TreeInfo>.BindPrefabs();
                }
                else
                {
                    Profiler.Warning( "Asset {0} can't be loaded." );
                }

                return loadedPrefab;
            }
            return null;
        }


        public bool PropTreeTrailer( Package.Asset asset )
        {
            CustomAssetMetaData assetMetaData = null;

            try
            {
                bool wantBecauseEnabled = loadEnabled && IsEnabled(asset);

                if ( !wantBecauseEnabled )
                    return false;

                assetMetaData = asset.Instantiate<CustomAssetMetaData>();
                CustomAssetMetaData.Type type = assetMetaData.type;

                if ( type == CustomAssetMetaData.Type.Building || type == CustomAssetMetaData.Type.Vehicle || type == CustomAssetMetaData.Type.Unknown ||
                    ( AssetImporterAssetTemplate.GetAssetDLCMask( assetMetaData ) & notMask ) != 0 )
                    return false;

                // Always remember: assetRef may point to another package because the deserialization method accepts any asset with a matching checksum.
                string fullName = asset.package.packageName + "." + assetMetaData.assetRef.name;
                HashSet<string> alreadyLoaded;
                bool wanted;

                switch ( type )
                {
                    case CustomAssetMetaData.Type.Prop:
                        wanted = wantBecauseEnabled || loadUsed;
                        alreadyLoaded = loadedProps;
                        break;

                    case CustomAssetMetaData.Type.Tree:
                        wanted = wantBecauseEnabled || loadUsed;
                        alreadyLoaded = loadedTrees;
                        break;

                    case CustomAssetMetaData.Type.Trailer:
                        wanted = wantBecauseEnabled || loadUsed;
                        alreadyLoaded = loadedVehicles;
                        break;

                    default:
                        return false;
                }

                if ( wanted && !IsDuplicate( fullName, alreadyLoaded, asset.package ) )
                    PropTreeTrailerImpl( fullName, assetMetaData.assetRef );
            }
            catch ( Exception ex )
            {
                Failed( assetMetaData?.assetRef, ex );
                // CODebugBase<LogChannel>.Warn(LogChannel.Modding, string.Concat(new object[] { ex.GetType(), ": Loading custom asset failed[", asset, "]\n", ex.Message }));
            }

            return true;
        }

        internal void PropTreeTrailerImpl( string fullName, Package.Asset data )
        {
            try
            {
                LoadingManager.instance.m_loadingProfilerCustomAsset.BeginLoading( AssetName( data.name ) );
                // CODebugBase<LogChannel>.Log(LogChannel.Modding, string.Concat("Loading custom asset ", assetMetaData.name, " from ", asset));

                GameObject go = data.Instantiate<GameObject>();
                go.name = fullName;
                go.SetActive( true );
                PrefabInfo info = go.GetComponent<PrefabInfo>();
                info.m_isCustomContent = true;

                if ( info.m_Atlas != null && info.m_InfoTooltipThumbnail != null && info.m_InfoTooltipThumbnail != string.Empty && info.m_Atlas[info.m_InfoTooltipThumbnail] != null )
                    info.m_InfoTooltipAtlas = info.m_Atlas;

                PropInfo pi = go.GetComponent<PropInfo>();

                if ( pi != null && loadedProps.Add( fullName ) )
                {
                    if ( pi.m_lodObject != null )
                        pi.m_lodObject.SetActive( true );
                    try
                    {
                        AddToPrefabCollection( pi );
                        loadedPrefab = pi;
                        propCount++;
                    }
                    catch ( Exception e )
                    {
                        Profiler.Error( "PropTreeTrailerImpl (prop)", e );
                    }
                }

                TreeInfo ti = go.GetComponent<TreeInfo>();

                if ( ti != null && loadedTrees.Add( fullName ) )
                {
                    try
                    {
                        AddToPrefabCollection( ti );
                        loadedPrefab = ti;
                        treeCount++;
                    }
                    catch ( Exception e )
                    {
                        Profiler.Error( "PropTreeTrailerImpl (tree)", e );
                    }
                }

                // Trailers, this way.
                VehicleInfo vi = go.GetComponent<VehicleInfo>();

                if ( vi != null )
                {
                    try
                    {
                        if ( loadedVehicles.Add( fullName ) )
                        {
                            loadedPrefab = vi;
                            AddToPrefabCollection( vi );
                        }

                        if ( vi.m_lodObject != null )
                            vi.m_lodObject.SetActive( true );
                    }
                    catch ( Exception e )
                    {
                        Profiler.Error( "PropTreeTrailerImpl (trailer)", e );
                    }
                }
            }
            catch ( Exception e )
            {
                Profiler.Error( "PropTreeTrailerImpl (unexpected)", e );
            }
            finally
            {
                LoadingManager.instance.m_loadingProfilerCustomAsset.EndLoading();
            }
        }

        public bool BuildingVehicle( Package.Asset asset, bool includedInStyle )
        {
            CustomAssetMetaData assetMetaData = null;
            try
            {

                bool wantBecauseEnabled = loadEnabled && IsEnabled(asset);

                if ( !includedInStyle && !wantBecauseEnabled )
                    return false;

                assetMetaData = asset.Instantiate<CustomAssetMetaData>();
                CustomAssetMetaData.Type type = assetMetaData.type;

                if ( type != CustomAssetMetaData.Type.Building && type != CustomAssetMetaData.Type.Vehicle && type != CustomAssetMetaData.Type.Unknown ||
                    ( AssetImporterAssetTemplate.GetAssetDLCMask( assetMetaData ) & notMask ) != 0 )
                    return false;

                // Always remember: assetRef may point to another package because the deserialization method accepts any asset with a matching checksum.
                string fullName = asset.package.packageName + "." + assetMetaData.assetRef.name;
                HashSet<string> alreadyLoaded;
                bool wanted;

                switch ( type )
                {
                    case CustomAssetMetaData.Type.Building:
                        wanted = wantBecauseEnabled;
                        alreadyLoaded = loadedBuildings;
                        break;

                    case CustomAssetMetaData.Type.Vehicle:
                        wanted = wantBecauseEnabled;
                        alreadyLoaded = loadedVehicles;
                        break;

                    case CustomAssetMetaData.Type.Unknown:
                        wanted = wantBecauseEnabled;
                        alreadyLoaded = new HashSet<string>();
                        break;

                    default:
                        return false;
                }

                if ( ( includedInStyle || wanted ) && !IsDuplicate( fullName, alreadyLoaded, asset.package ) )
                    BuildingVehicleImpl( fullName, assetMetaData.assetRef, wanted );
            }
            catch ( Exception ex )
            {
                Failed( assetMetaData?.assetRef, ex );
                // CODebugBase<LogChannel>.Warn(LogChannel.Modding, string.Concat(new object[] { ex.GetType(), ": Loading custom asset failed:[", asset, "]\n", ex.Message }));
            }

            return true;
        }

        void BuildingVehicleImpl( string fullName, Package.Asset data, bool wanted )
        {
            Profiler.Trace( "BuildingVehicleImpl {0}, {1}", data, wanted );
            try
            {
                LoadingManager.instance.m_loadingProfilerCustomAsset.BeginLoading( AssetName( data.name ) );
                // CODebugBase<LogChannel>.Log(LogChannel.Modding, string.Concat("Loading custom asset ", assetMetaData.name, " from ", asset));

                currentFullName = fullName;
                GameObject go = data.Instantiate<GameObject>();
                go.name = fullName;
                go.SetActive( true );
                PrefabInfo info = go.GetComponent<PrefabInfo>();
                info.m_isCustomContent = true;
                
                if ( info.m_Atlas != null && info.m_InfoTooltipThumbnail != null && info.m_InfoTooltipThumbnail != string.Empty && info.m_Atlas[info.m_InfoTooltipThumbnail] != null )
                    info.m_InfoTooltipAtlas = info.m_Atlas;

                BuildingInfo bi = go.GetComponent<BuildingInfo>();

                Profiler.Trace( "BuildingVehicleImpl bi {0}", bi );
                
                if ( bi != null )
                {
                    if ( bi.m_lodObject != null )
                        bi.m_lodObject.SetActive( true );

                    if ( loadedBuildings.Add( fullName ) )
                    {
                        try
                        {
                            AddToPrefabCollection( bi );
                            loadedPrefab = bi;
                            bi.m_dontSpawnNormally = !wanted;
                            buildingCount++;

                            if ( bi.GetAI() is IntersectionAI )
                                loadedIntersections.Add( fullName );
                        }
                        catch ( Exception e )
                        {
                            Profiler.Error( "BuildingVehicleImpl (building)", e );
                        }
                    }
                }

                VehicleInfo vi = go.GetComponent<VehicleInfo>();

                if ( vi != null )
                {
                    try
                    {
                        if ( loadedVehicles.Add( fullName ) )
                        {
                            AddToPrefabCollection( vi );
                            loadedPrefab = vi;
                            vehicleCount++;
                        }

                        if ( vi.m_lodObject != null )
                            vi.m_lodObject.SetActive( true );
                    }
                    catch ( Exception e )
                    {
                        Profiler.Error( "BuildingVehicleImpl (vehicle)", e );
                    }
                }
            }
            catch (Exception e)
            {
                Profiler.Error( "BuildingVehicleImpl (unexpected)", e );
            }
            finally
            {
                currentFullName = null;
                LoadingManager.instance.m_loadingProfilerCustomAsset.EndLoading();
            }
        }

        void AddToPrefabCollection<T>( T data ) where T : PrefabInfo
        {
            object prefabLock =  null;

            try
            {
                Type type = typeof(PrefabCollection<T>);
                FieldInfo fi = type.GetField( "m_prefabLock", BindingFlags.NonPublic | BindingFlags.Static );
                prefabLock = fi.GetValue( null );

                Profiler.Trace( "Waiting for lock" );
                while ( !Monitor.TryEnter( prefabLock, SimulationManager.SYNCHRONIZE_TIMEOUT ) )
                {
                    //Profiler.Trace( "Still waiting" );
                }

                PrefabCollection<T>.InitializePrefabs( "Custom Assets", data, null );
            }
            catch ( Exception e )
            {
                throw ( e );
            }
            finally
            {
                if ( prefabLock != null )
                    try { Monitor.Exit( prefabLock ); } catch { }
            }
        }

        // There is an interesting bug in the package manager: secondary CustomAssetMetaDatas in a crp are considered always enabled.
        // As a result, the game loads all vehicle trailers, no matter if they are enabled or not. This is the fix.
        static bool IsEnabled( Package.Asset asset )
        {
            if ( asset.isMainAsset )
                return asset.isEnabled;

            Package.Asset main = asset.package.Find(asset.package.packageMainAsset);
            return main?.isEnabled ?? false;
        }

        internal static string AssetName( string name_Data ) => name_Data.Length > 5 && name_Data.EndsWith( "_Data" ) ? name_Data.Substring( 0, name_Data.Length - 5 ) : name_Data;

        static string ShortenAssetName( string fullName_Data )
        {
            int j = fullName_Data.IndexOf('.');

            if ( j >= 0 && j < fullName_Data.Length - 1 )
                fullName_Data = fullName_Data.Substring( j + 1 );

            return AssetName( fullName_Data );
        }

        internal void Failed( Package.Asset data, Exception e )
        {
            string name = data?.name;

            if ( e != null )
                UnityEngine.Debug.LogException( e );
        }

        internal void Duplicate( string name, Package package )
        {
            string path = package.packagePath ?? "Path unknown";

            name = ShortenAssetName( name );
            LoadingManager.instance.m_loadingProfilerCustomAsset.BeginLoading( name );
            LoadingManager.instance.m_loadingProfilerCustomAsset.EndLoading();
        }

        bool IsDuplicate( string fullName, HashSet<string> alreadyLoaded, Package package )
        {
            if ( alreadyLoaded.Contains( fullName ) )
            {
                Duplicate( fullName, package );
                return true;
            }
            else
                return false;
        }

        internal static bool IsWorkshopPackage( string fullName, out ulong id )
        {
            int j = fullName.IndexOf('.');

            if ( j <= 0 || j >= fullName.Length - 1 )
            {
                id = 0;
                return false;
            }

            string p = fullName.Substring(0, j);
            return ulong.TryParse( p, out id ) && id > 999999;
        }

        internal static bool IsPrivatePackage( string fullName )
        {
            ulong id;

            // Private: a local asset created by the player (not from the workshop).
            // My rationale is the following:
            // 43453453.Name -> Workshop
            // Name.Name     -> Private
            // Name          -> Either an old-format (early 2015) reference, or something from DLC/Deluxe packs.
            //                  If loading is not successful then cannot tell for sure, assumed DLC/Deluxe when reported as not found.

            if ( IsWorkshopPackage( fullName, out id ) )
                return false;
            else
                return fullName.IndexOf( '.' ) >= 0;
        }
    }
}
