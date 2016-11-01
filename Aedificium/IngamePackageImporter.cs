using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    class IngamePackageImporter
    {
        public static bool ImportPackage( string path, out List<PrefabInfo> prefabs )
        {
            return ImportPackage( new DirectoryInfo( path ), out prefabs );
        }

        public static bool ImportPackage( DirectoryInfo path, out List<PrefabInfo> prefabs )
        {
            var idstr = path.Name;
            prefabs = new List<PrefabInfo>();

            Profiler.Trace( "Importing package {0}", path );
            try
            {
                var id = Convert.ToUInt64(idstr);

                foreach ( var file in path.GetFiles( "*.crp" ) )
                {
                    Profiler.Trace( "Loading asset {0}, {1}", id, file.FullName );
                    var bi = LoadPrefab( id, file.FullName );
                    Profiler.Trace( "Adding..." );
                    prefabs.Add( bi );
                }
            }
            catch ( Exception e )
            {
                Profiler.Error( "Can't load {0}\r\n{1}\r\n{2}", path, e.Message, e.StackTrace );
                return false;
            }

           Profiler.Trace( "Done Importing package" ); 
            return true;
        }

        static PrefabInfo LoadPrefab( ulong id, string crpFile )
        {
            var prefabs = Resources
                .FindObjectsOfTypeAll<PrefabInfo>()
                .Where( p => p.name.Contains( id.ToString() ) );

            if ( prefabs.Count() > 0 )
            {
                Profiler.Trace( "Prefab already loaded {0}, {1}", id, crpFile );
                return prefabs.First();
            }

            PackageManager.Update( new PublishedFileId( id ), crpFile );
            var package = PackageManager.GetPackage( id.ToString() );
            Profiler.Trace( "Loaded package {0}", package );

            var asset = package.Find( package.packageMainAsset );
            asset.isEnabled = true;
            Profiler.Trace( "Loaded asset {0}", asset );

            return new AssetLoader().LoadPrefab( asset );
        }
    }
}
