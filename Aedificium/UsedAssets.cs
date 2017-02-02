using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using UnityEngine;

namespace Aedificium
{
    sealed class UsedAssets
    {
        static UsedAssets _instance;

        public HashSet<string>
            allPackages = new HashSet<string>(),
            buildingAssets = new HashSet<string>(),
            propAssets = new HashSet<string>(),
            treeAssets = new HashSet<string>(),
            vehicleAssets = new HashSet<string>(),
            indirectProps = new HashSet<string>(),
            indirectTrees = new HashSet<string>(),
            buildingPrefabs = new HashSet<string>();

        IEnumerable<Package.Asset> assets;
        Dictionary<PublishedFileId, HashSet<string>> packagesToPaths;

        public static UsedAssets instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new UsedAssets();
                    _instance.LookupUsed();
                }

                return _instance;
            }
        }


        public static void Refresh()
        {
            // without the condition LookupUsed might be executed twice
            if ( _instance != null )
                instance.LookupUsed();
        }

        void LookupUsed()
        {
            LookupSimulationBuildings( allPackages, buildingAssets );
            LookupSimulationAssets<PropInfo>( allPackages, propAssets );
            LookupSimulationAssets<TreeInfo>( allPackages, treeAssets );
            LookupSimulationAssets<VehicleInfo>( allPackages, vehicleAssets );
        }

        void Dispose()
        {
            allPackages.Clear(); buildingAssets.Clear(); propAssets.Clear(); treeAssets.Clear(); vehicleAssets.Clear(); indirectProps.Clear(); indirectTrees.Clear(); buildingPrefabs.Clear();
            allPackages = null; buildingAssets = null; propAssets = null; treeAssets = null; vehicleAssets = null; indirectProps = null; indirectTrees = null; buildingPrefabs = null;
            _instance = null; assets = null;
        }

        public List<String> MissingAssets
        {
            get
            {
                var missingAssets = new List<string>();
                missingAssets.AddRange( MissingAssetsOfType<BuildingInfo>( buildingAssets ) );
                missingAssets.AddRange( MissingAssetsOfType<PropInfo>( propAssets ) );
                missingAssets.AddRange( MissingAssetsOfType<TreeInfo>( treeAssets ) );
                missingAssets.AddRange( MissingAssetsOfType<VehicleInfo>( vehicleAssets ) );
                return missingAssets;
            }
        }

        List<String> MissingAssetsOfType<P>(HashSet<string> customAssets) where P : PrefabInfo
        {
            try
            {
                return customAssets
                    .ToList()
                    .FindAll( n => PrefabCollection<P>.FindLoaded( n ) == null );
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return null;
            }
        }


        /// <summary>
        /// Looks up the custom assets placed in the city.
        /// </summary>
        void LookupSimulationAssets<P>(HashSet<string> packages, HashSet<string> assets) where P : PrefabInfo
        {
            try
            {
                int n = PrefabCollection<P>.PrefabCount();

                for (int i = 0; i < n; i++)
                    StorePackageAndAssetNames(PrefabCollection<P>.PrefabName((uint) i), packages, assets);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        /// <summary>
        /// BuildingInfos require more effort because the NotUsedGuide/UnlockMilestone stuff gets into way.
        /// </summary>
        void LookupSimulationBuildings(HashSet<string> packages, HashSet<string> assets)
        {
            try
            {
                Building[] buffer = BuildingManager.instance.m_buildings.m_buffer;
                int n = buffer.Length;
                
                for (int i = 1; i < n; i++)
                    if (buffer[i].m_flags != Building.Flags.None)
                        StorePackageAndAssetNames(PrefabCollection<BuildingInfo>.PrefabName(buffer[i].m_infoIndex), packages, assets, buildingPrefabs );
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        static void StorePackageAndAssetNames(string fullName, HashSet<string> packages, HashSet<string> assets, HashSet<string> prefabs = null)
        {
            if (!string.IsNullOrEmpty(fullName))
            {
                int j = fullName.IndexOf('.');

                // Recognize custom assets:
                if (j >= 0 && j < fullName.Length - 1)
                {
                    packages.Add(fullName.Substring(0, j)); // packagename (or pac in case the full name is pac.kagename.assetname)
                    assets.Add(fullName); // packagename.assetname
                }
            }
        }

        public static void StoreIndirectPropName( String prop )
        {
            instance.indirectProps.Add( prop );
        }

        public static void StoreIndirectTreeName( String prop )
        {
            instance.indirectProps.Add( prop );
        }

        // Works with (fullName = asset name), too.
        static T Get<T>(string fullName) where T : PrefabInfo
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            T info = PrefabCollection<T>.FindLoaded(fullName);

            if (info == null && AssetLoader.instance.LoadAsset( fullName, FindAsset(fullName) ) )
                info = PrefabCollection<T>.FindLoaded(fullName);

            return info;
        }

        // For sub-buildings, name may be package.assetname.
        static T Get<T>(Package package, string fullName, string name, bool tryName) where T : PrefabInfo
        {
            T info = PrefabCollection<T>.FindLoaded(fullName);

            if (info == null && tryName)
                info = PrefabCollection<T>.FindLoaded(name);

            if (info == null)
            {
                Package.Asset data = package.Find(name);

                if (data == null && tryName)
                    data = FindAsset(name); // yes, name

                if (data != null)
                    fullName = data.fullName;
                else if (name.Contains("."))
                    fullName = name;

                if ( AssetLoader.instance.LoadAsset( fullName, data ) )
                    info = PrefabCollection<T>.FindLoaded(fullName);
            }

            return info;
        }

        /// <summary>
        /// Given packagename.assetname, find the asset. Works with (fullName = asset name), too.
        /// </summary>
        public static Package.Asset FindAsset(string fullName)
        {
            try
            {
                int j = fullName.IndexOf('.');

                if (j > 0 && j < fullName.Length - 1)
                {
                    // The fast path.
                    Package.Asset asset = instance.FindByName(fullName.Substring(0, j), fullName.Substring(j + 1));

                    if (asset != null)
                        return asset;
                }

                // Fast fail.
                if ( AssetLoader.instance.failedAssets.Contains( fullName ) )
                    return null;

                IEnumerable<Package.Asset> assets = UsedAssets.instance.assets;

                if (assets == null)
                    assets = UsedAssets.instance.assets = AssetLoader.AssetsByType(Package.AssetType.Object);

                // We also try the old (early 2015) naming that does not contain the package name. FindLoaded does this, too.
                foreach (Package.Asset asset in assets)
                    if (fullName == asset.fullName || fullName == asset.name)
                        return asset;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return null;
        }

        Package.Asset FindByName(string packageName, string assetName)
        {
            PublishedFileId id = Util.GetPackageId(packageName);

            if (id != PublishedFileId.invalid)
            {
                if (packagesToPaths == null || packagesToPaths.Count == 0)
                    packagesToPaths = (Dictionary<PublishedFileId, HashSet<string>>) Util.GetStaticField(typeof(PackageManager), "m_PackagesSteamToPathsMap");

                HashSet<string> paths;

                if (packagesToPaths.TryGetValue(id, out paths))
                {
                    Package package; Package.Asset asset;

                    foreach (string path in paths)
                        if ((package = PackageManager.FindPackageAt(path)) != null && (asset = package.Find(assetName)) != null)
                            return asset;
                }
            }

            return null;
        }
    }
}
