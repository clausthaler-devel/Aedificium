using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;


namespace Aedificium
{
//#if DEBUG
//    [ProfilerAspect()]
//#endif
    /// <summary>
    /// LoadCustomContent coroutine from LoadingManager.
    /// </summary>
    public partial class AssetLoader
    {
        public static AssetLoader _instance;

        public List<PrefabInfo> loadedPrefabs = new List<PrefabInfo>();
        public HashSet<string> 
            failedAssets = new HashSet<string>(), 
            loadedProps = new HashSet<string>(), 
            loadedTrees = new HashSet<string>(),
            loadedBuildings = new HashSet<string>(), 
            loadedVehicles = new HashSet<string>(), 
            loadedIntersections = new HashSet<string>();
        
        public static Stack<string> stack = new Stack<string>(4); // the asset loading stack

        public static string Current => stack.Count > 0 ? stack.Peek() : string.Empty;

        public static AssetLoader instance
        {
            get
            {
                if ( _instance == null )
                    _instance = new AssetLoader();

                return _instance;
            }
        }
        
        public void Dispose()
        {
            Profiler.Trace( "AssetLoader.Dispose" );
            failedAssets.Clear(); loadedProps.Clear(); loadedTrees.Clear(); loadedBuildings.Clear(); loadedVehicles.Clear(); loadedIntersections.Clear();
            failedAssets = null; loadedProps = null; loadedTrees = null; loadedBuildings = null; loadedVehicles = null; loadedIntersections = null;
        }

        public List<DistrictStyle> districtStyles = new List<DistrictStyle>();
        public HashSet<string> styleBuildings = new HashSet<string>();
        public FastList<DistrictStyleMetaData> districtStyleMetaDatas = new FastList<DistrictStyleMetaData>();
        public FastList<Package> districtStylePackages = new FastList<Package>();

        void PreprocessDistricStyles()
        {
            Package.Asset europeanStyles = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanStyleName);

            if ( europeanStyles != null && europeanStyles.isEnabled )
            {
                DistrictStyle districtStyle = new DistrictStyle(DistrictStyle.kEuropeanStyleName, true);
                districtStyles.Add( districtStyle );
            }
            
            foreach ( Package.Asset asset in PackageManager.FilterAssets( UserAssetType.DistrictStyleMetaData ) )
                SetupAssetDistrictStyle( asset );
        }

        void SetupAssetDistrictStyle( Package.Asset asset )
        {
            int i;

            try
            {
                if ( asset != null && asset.isEnabled )
                {
                    DistrictStyleMetaData districtStyleMetaData = asset.Instantiate<DistrictStyleMetaData>();

                    if ( districtStyleMetaData != null && !districtStyleMetaData.builtin )
                    {
                        districtStyleMetaDatas.Add( districtStyleMetaData );
                        districtStylePackages.Add( asset.package );

                        if ( districtStyleMetaData.assets != null )
                            for ( i = 0 ; i < districtStyleMetaData.assets.Length ; i++ )
                                styleBuildings.Add( districtStyleMetaData.assets[i] );
                    }
                }
            }
            catch ( Exception ex )
            {
                CODebugBase<LogChannel>.Warn( LogChannel.Modding, string.Concat( new object[] { ex.GetType(), ": Loading custom district style failed[", asset, "]\n", ex.Message } ) );
            }
        }


        public bool LoadCustomContent(List<Package.Asset> assets = null )
        {
            try
            {
                Profiler.Info( "Loading {0} assets", assets != null ? assets?.Count() : 0 );

                loadedPrefabs.Clear();
                
                //Profiler.Trace( "PreprocessDistricStyles" );
                //PreprocessDistricStyles();

                // Load custom assets.
                foreach ( var queue in GetLoadQueues( assets ) )
                    foreach ( var asset in queue )
                    {
                        notFound.Clear(); notFoundIndirect.Clear();
                        return LoadAsset( asset.fullName, asset, true );
                    }

                //Profiler.Trace( "PostprocessDistricStyles" ); 
                //PostprocessDistricStyles();

                return true;
            }
            catch( Exception e)
            {
                Profiler.Error( "Can't load custom content:", e );
                return false;
            }
        }
        

        void PostprocessDistricStyles()
        {
            int i,j;

            for (i = 0; i<districtStyleMetaDatas.m_size; i++)
            {
                try
                {
                    DistrictStyleMetaData districtStyleMetaData = districtStyleMetaDatas.m_buffer[i];
                    DistrictStyle districtStyle = new DistrictStyle(districtStyleMetaData.name, false);

                    if (districtStylePackages.m_buffer[i].GetPublishedFileID() != PublishedFileId.invalid)
                        districtStyle.PackageName = districtStylePackages.m_buffer[i].packageName;

                    if (districtStyleMetaData.assets != null)
                    {
                        for(j = 0; j<districtStyleMetaData.assets.Length; j++)
                        {
                            BuildingInfo bi = PrefabCollection<BuildingInfo>.FindLoaded(districtStyleMetaData.assets[j] + "_Data");

                            if (bi != null)
                            {
                                districtStyle.Add(bi);

                                if (districtStyleMetaData.builtin) // this is always false
                                    bi.m_dontSpawnNormally = !districtStyleMetaData.assetRef.isEnabled;
                            }
                            else
                                CODebugBase<LogChannel>.Warn(LogChannel.Modding, "Warning: Missing asset (" + districtStyleMetaData.assets[i] + ") in style " + districtStyleMetaData.name);
                        }

                        districtStyles.Add(districtStyle);
                    }
                }
                catch (Exception ex)
                {
                    CODebugBase<LogChannel>.Warn(LogChannel.Modding, ex.GetType() + ": Loading district style failed\n" + ex.Message);
                }
            }

            Singleton<DistrictManager>.instance.m_Styles = districtStyles.ToArray();

            if (Singleton<BuildingManager>.exists)
                Singleton<BuildingManager>.instance.InitializeStyleArray(districtStyles.Count);
    }

        public PrefabInfo InstantiateAssetPrefab( GameObject go )
        {
            PrefabInfo info = go.GetComponent<PrefabInfo>();
            info.m_isCustomContent = true;

            if ( info.m_Atlas != null && !string.IsNullOrEmpty( info.m_InfoTooltipThumbnail ) && info.m_Atlas[info.m_InfoTooltipThumbnail] != null )
                info.m_InfoTooltipAtlas = info.m_Atlas;

            return info;
        }

        public CustomAssetMetaData InstantiateAssetMetaData( Package.Asset asset )
        {
            //Profiler.Trace( "Instantiate asset start" );
            CustomAssetMetaData assetMetaData = asset.Instantiate<CustomAssetMetaData>();
            //Profiler.Trace( "Instantiate asset end" );
            return assetMetaData;
        }

        public GameObject InstantiateAssetGameObject( CustomAssetMetaData assetMetaData )
        {
            //Profiler.Trace( "Instantiate asset game object start {0}", notFound.Count() );
            var go = assetMetaData.assetRef.Instantiate<GameObject>();
            //Profiler.Trace( "Instantiate asset game object {0} end {1}", go, notFound.Count() );
            go.name = assetMetaData.assetRef.package.packageName + "." + assetMetaData.assetRef.name;
            go.SetActive( false );
            return go;
        }

        bool IsCommonBuilding(String fullName, Package.Asset asset, CustomAssetMetaData assetMetaData)
        {
            CustomAssetMetaData.Type type = assetMetaData.type;
            return (type == CustomAssetMetaData.Type.Building || type == CustomAssetMetaData.Type.SubBuilding);
        }

        public enum LoadStatus
        {
            Deferred,
            Loaded,
            Failed
        }


        public bool LoadAsset( string fullName, Package.Asset asset, bool mainAsset = false )
        {

            if ( asset == null )
                return StoreNotFoundName( fullName );

            Profiler.Info( "Loading asset {0}", asset.fullName );

            try
            {
                Profiler.Trace( "Clearing stack" );
                stack.Clear();

                if ( mainAsset )
                {
                    Profiler.Info( "Pushing {0} on stack", fullName );
                    stack.Push( fullName );
                }

                Profiler.Info( "Stack count {0}, current {1}", stack.Count, Current );

                CustomAssetMetaData assetMetaData = InstantiateAssetMetaData( asset );

                // Always remember: assetRef may point to another package because the deserialization method accepts any asset with a matching checksum.
                // There is a bug in the 1.6.0 game update in this.
                fullName = asset.package.packageName + "." + assetMetaData.assetRef.name;

                //impl
                GameObject assetGameObject = InstantiateAssetGameObject( assetMetaData );
                assetGameObject.name = fullName;
                
                PrefabInfo assetPrefabInfo = InstantiateAssetPrefab( assetGameObject );

                if ( mainAsset && Settings.instance.installDependencies  && notFoundIndirect.Count > 0 )
                    return HotloadManager.DeferLoad( fullName, notFoundIndirect.Keys.ToList() );
               

                RegisterPrefab( fullName, asset, assetMetaData, assetPrefabInfo, assetGameObject );
                //impl
                if ( mainAsset )
                {
                    Profiler.Trace( "Popping {0} from stack", stack.Peek() );
                    stack.Pop();
                }
                Profiler.Info( "Stack count {0}", stack.Count );

                return StorePrefabInfo( assetPrefabInfo );
            }
            catch ( Exception e )
            {
                if ( mainAsset )
                {
                    Profiler.Trace( "Popping {0} from stack due to error {1}", stack.Peek(), e.Message );
                    stack.Pop();
                }
                Profiler.Error( "Cannot load " + fullName, e );
                StoreFailedAssetName( fullName ?? asset.fullName, e );
                return false;
            }
        }

        bool RegisterPrefab( String fullName, Package.Asset asset, CustomAssetMetaData metaData, PrefabInfo info, GameObject go )
        {
            PropInfo pi = go.GetComponent<PropInfo>();
            if ( pi != null )
            {
                if ( pi.m_lodObject != null )
                    pi.m_lodObject.SetActive( false );

                if ( StorePropName( fullName ) )
                {
                    PrefabCollection<PropInfo>.InitializePrefabs( "Custom Assets", pi, null );
                    return true;
                }
            }

            TreeInfo ti = go.GetComponent<TreeInfo>();

            if ( ti != null && StoreTreeName( fullName ) )
            {
                PrefabCollection<TreeInfo>.InitializePrefabs( "Custom Assets", ti, null );
                return true;
            }

            BuildingInfo bi = go.GetComponent<BuildingInfo>();

            if ( bi != null )
            {
                if ( bi.m_lodObject != null )
                    bi.m_lodObject.SetActive( false );

                if ( StoreBuildingName( fullName ) )
                {
                    PrefabCollection<BuildingInfo>.InitializePrefabs( "Custom Assets", bi, null );
                    bi.m_dontSpawnNormally = ! IsCommonBuilding( fullName, asset, metaData );

                    if ( bi.GetAI() is IntersectionAI )
                        loadedIntersections.Add( fullName );

                    return true;
                }
            }

            VehicleInfo vi = go.GetComponent<VehicleInfo>();

            if ( vi != null )
            {
                if ( vi.m_lodObject != null )
                    vi.m_lodObject.SetActive( false );

                if ( StoreVehicleName( fullName ) )
                {
                    PrefabCollection<VehicleInfo>.InitializePrefabs( "Custom Assets", vi, null );
                    return true;
                }
            }

            return false;
        }


        List<Package.Asset>[] GetLoadQueues()
        {
            return GetLoadQueues( AssetsByType(UserAssetType.CustomAssetMetaData) );
        }


        List<Package.Asset>[] GetLoadQueues( IEnumerable<Package.Asset> assets )
        {
            Profiler.Info( "Create Load queue for {0} assets", assets.Count() );
            List<Package.Asset>[] queues = { new List<Package.Asset>(4), new List<Package.Asset>(64), new List<Package.Asset>(4), new List<Package.Asset>(64) };
            SteamHelper.DLC_BitMask notMask = ~SteamHelper.GetOwnedDLCMask();

            foreach ( Package.Asset asset in assets )
            {
                string fullName = null;
                // Profiler.Trace( "Q {0} - {1} - {2} - {3}", asset, loadEnabled, IsEnabled( asset ), styleBuildings.Contains( asset.fullName ) );
                //Profiler.Trace( "Q {0} - {1} - {2}", asset, IsEnabled( asset ), styleBuildings);
                try
                {
                   
                    //triggers calls to CustomSerialize
                    CustomAssetMetaData assetMetaData = asset.Instantiate<CustomAssetMetaData>();
                    //Profiler.Trace( "meta {0}", assetMetaData );
                    // Always remember: assetRef may point to another package because the deserialization method accepts any asset with the same checksum.
                    // Think of identical vehicle trailers in different crp's.
                    // There is a bug in the 1.6.0 game update in this.
                    fullName = asset.package.packageName + "." + assetMetaData.assetRef.name;

                    if ((AssetImporterAssetTemplate.GetAssetDLCMask(assetMetaData) & notMask) == 0)
                        switch (assetMetaData.type)
                        {
                            case CustomAssetMetaData.Type.Building:
                                if (!IsDuplicate(fullName, loadedBuildings, asset.package))
                                    queues[3].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.Prop:
                                if (!IsDuplicate(fullName, loadedProps, asset.package))
                                    queues[1].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.Tree:
                                if (!IsDuplicate(fullName, loadedTrees, asset.package))
                                    queues[1].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.Vehicle:
                                if (!IsDuplicate(fullName, loadedVehicles, asset.package))
                                    queues[3].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.Trailer:
                                if (!IsDuplicate(fullName, loadedVehicles, asset.package))
                                    queues[1].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.Unknown:
                                queues[3].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.SubBuilding:
                                if (!IsDuplicate(fullName, loadedBuildings, asset.package))
                                    queues[2].Add(asset);
                                break;

                            case CustomAssetMetaData.Type.PropVariation:
                                if (!IsDuplicate(fullName, loadedProps, asset.package))
                                    queues[0].Add(asset);
                                break;
                        }
                }
                catch (Exception e)
                {
                    Profiler.Error( "GetLoadQueues", e );
                    StoreFailedAssetName(fullName ?? asset.fullName, e);
                }
            }

            return queues;
        }

        public static IEnumerable<Package.Asset> AssetsByType( Package.AssetType assetType )
        {
            return EnabledAssetsFirst( PackageManager.FilterAssets( assetType ) );
        }

        static IEnumerable<Package.Asset> EnabledAssetsFirst( IEnumerable<Package.Asset> assets )
        {
            List<Package.Asset> enabled = new List<Package.Asset>(64), notEnabled = new List<Package.Asset>(64);

            foreach (Package.Asset asset in assets )
                if (asset != null)
                    if (IsEnabled(asset))
                        enabled.Add(asset);
                    else
                        notEnabled.Add(asset);

            // Why enabled assets first? Because in duplicate prefab name situations, I want the enabled one to get through.

            List<Package.Asset> ret = new List<Package.Asset>();
            ret.AddRange( enabled );
            ret.AddRange( notEnabled );

            return ret;
        }

        // There is an interesting bug in the package manager: secondary CustomAssetMetaDatas in a crp are considered always enabled.
        // As a result, the game loads all vehicle trailers, no matter if they are enabled or not. This is the fix.
        static bool IsEnabled(Package.Asset asset)
        {
            if (asset.isMainAsset)
                return asset.isEnabled;

            Package.Asset main = asset.package.Find(asset.package.packageMainAsset);
            return main?.isEnabled ?? false;
        }

        static string AssetName(string name_Data) => name_Data.Length > 5 && name_Data.EndsWith("_Data") ? name_Data.Substring(0, name_Data.Length - 5) : name_Data;

        static string ShorterAssetName(string fullName_Data)
        {
            Profiler.Trace( "ShorterAssetName {0}", fullName_Data );
            int j = fullName_Data.IndexOf('.');

            if (j >= 0 && j < fullName_Data.Length - 1)
                fullName_Data = fullName_Data.Substring(j + 1);

            Profiler.Trace( "ShorterAssetName = {0}", fullName_Data );
            return AssetName(fullName_Data);
        }
        List<string> failed = new List<string>();

        Dictionary<string, List<string>> duplicate = new Dictionary<string, List<string>>();

        void Duplicate(string name, string path)
        {
            List<string> list;

            if (duplicate.TryGetValue(name, out list) && list != null)
                list.Add(path);
            else
                duplicate[name] = new List<string> { path };
        }

        public bool IsDuplicate(string fullName, HashSet<string> alreadyLoaded, Package package)
        {
            if (alreadyLoaded.Contains(fullName))
            {
                Duplicate( fullName, package.packagePath ?? "Path unknown" );
                return true;
            }
            else
                return false;
        }

        static bool IsWorkshopPackage(string fullName, out ulong id)
        {
            int j = fullName.IndexOf('.');

            if (j <= 0 || j >= fullName.Length - 1)
            {
                id = 0;
                return false;
            }

            string p = fullName.Substring(0, j);
            return ulong.TryParse(p, out id) && id > 999999;
        }

        static bool IsPrivatePackage(string fullName)
        {
            ulong id;

            // Private: a local asset created by the player (not from the workshop).
            // My rationale is the following:
            // 43453453.Name -> Workshop
            // Name.Name     -> Private
            // Name          -> Either an old-format (early 2015) reference, or something from DLC/Deluxe packs.
            //                  If loading is not successful then cannot tell for sure, assumed DLC/Deluxe when reported as not found.

            if (IsWorkshopPackage(fullName, out id))
                return false;
            else
                return fullName.IndexOf('.') >= 0;
        }

    }
}
