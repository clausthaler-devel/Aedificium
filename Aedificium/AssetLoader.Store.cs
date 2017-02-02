using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aedificium
{
    public partial class AssetLoader
    {
        void StoreFailedAssetName( string fullName, Exception e )
        {
            if ( fullName != null && StoreFailedAssetName( fullName ) )
            {
                Profiler.Trace( "Asset failed: {0}", fullName );
                failed.Add( ShorterAssetName( fullName ) );
            }

            if ( e != null )
                UnityEngine.Debug.LogException( e );
        }

        public List<string> notFound = new List<string>();
        public Dictionary<string, HashSet<string>> notFoundIndirect = new Dictionary<string, HashSet<string>>();


        bool StorePrefabInfo( PrefabInfo assetPrefabInfo )
        {
            loadedPrefabs.Add( assetPrefabInfo );
            return true;
        }

        bool StorePropName( String info )
        {
            loadedProps.Add( info );
            return true;
        }

        bool StoreTreeName( String info )
        {
            loadedTrees.Add( info );
            return true;
        }

        bool StoreBuildingName( String info )
        {
            loadedBuildings.Add( info );
            return true;
        }

        bool StoreIntersectionName( String info )
        {
            loadedIntersections.Add( info );
            return true;
        }

        bool StoreVehicleName( String info )
        {
            loadedVehicles.Add( info );
            return true;
        }

        bool StoreFailedAssetName( string fullName )
        {
            failedAssets.Add( fullName );
            return true;
        }

        void StoreNotFoundIndirectName( string name, string referencedBy )
        {
            HashSet<string> set;

            if ( notFoundIndirect.TryGetValue( name, out set ) && set != null )
                set.Add( referencedBy );
            else
                notFoundIndirect[name] = new HashSet<string> { referencedBy };
        }

        bool StoreNotFoundName( string fullName )
        {
            //Profiler.Info( "Can't find asset -{0}-{1}-", fullName, Current );
            if ( fullName != null )
            {
                if ( !string.IsNullOrEmpty( Current ) )
                {
                    Profiler.Info( "Missing indirect asset {0}", fullName );
                    StoreNotFoundIndirectName( fullName, Current );
                }
                else
                {
                    Profiler.Info( "Missing asset {0}", fullName );
                    notFound.Add( fullName );
                }


                StoreFailedAssetName( fullName );
            }
            return false;
        }

    }
}
