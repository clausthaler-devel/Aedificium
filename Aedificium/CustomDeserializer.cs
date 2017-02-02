using System;
using System.Collections.Generic;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using UnityEngine;

namespace Aedificium
{
    sealed class CustomDeserializer
    {
        public static PackageDeserializer.CustomDeserializeHandler OriginalDeserializer;

        public static void Hook()
        {
            if ( PackageDeserializer.customDeserializer != CustomDeserialize )
            {
                OriginalDeserializer = PackageDeserializer.customDeserializer;
                PackageDeserializer.SetCustomDeserializer( CustomDeserialize );
            }
        }

        public static void Unhook()
        {
            if ( PackageDeserializer.customDeserializer == CustomDeserialize )
            {
                PackageDeserializer.SetCustomDeserializer( OriginalDeserializer );
            }
        }

        static object CustomDeserialize( Package p, Type t, PackageReader r )
        {
            // First, make the common case fast.
            if ( t == typeof( float ) )
                return r.ReadSingle();
            if ( t == typeof( Vector2 ) )
                return r.ReadVector2();

            // Props and trees in buildings and parks.
            if ( t == typeof( BuildingInfo.Prop ) )
            {
                PropInfo pi = Get<PropInfo>(r.ReadString()); // old name format (without package name) is possible
                TreeInfo ti = Get<TreeInfo>(r.ReadString()); // old name format (without package name) is possible

                    if ( pi != null )
                    {
                        string n = pi.gameObject.name;

                        if ( !string.IsNullOrEmpty( n ) && n.Contains( "." ) )
                            UsedAssets.StoreIndirectPropName( n );
                    }

                    if ( ti != null )
                    {
                        string n = ti.gameObject.name;

                        if ( !string.IsNullOrEmpty( n ) && n.Contains( "." ) )
                            UsedAssets.StoreIndirectTreeName( n );
                }

                return new BuildingInfo.Prop {
                    m_prop = pi,
                    m_tree = ti,
                    m_position = r.ReadVector3(),
                    m_angle = r.ReadSingle(),
                    m_probability = r.ReadInt32(),
                    m_fixedHeight = r.ReadBoolean()
                };
            }

            // Prop variations in props.
            if ( t == typeof( PropInfo.Variation ) )
            {
                string name = r.ReadString();
                string fullName = p.packageName + "." + name;
                PropInfo pi = null;

                if ( fullName == AssetLoader.Current )
                    Profiler.Warning( "{0} wants to be a prop variation for itself.", fullName );
                else
                    pi = Get<PropInfo>( p, fullName, name, false );

                return new PropInfo.Variation {
                    m_prop = pi,
                    m_probability = r.ReadInt32()
                };
            }

            // Tree variations in trees.
            if ( t == typeof( TreeInfo.Variation ) )
            {
                string name = r.ReadString();
                string fullName = p.packageName + "." + name;
                TreeInfo ti = null;

                if ( fullName == AssetLoader.Current )
                    Profiler.Warning( "{0} wants to be a tree variation for itself.", fullName );
                else
                    ti = Get<TreeInfo>( p, fullName, name, false );

                return new TreeInfo.Variation {
                    m_tree = ti,
                    m_probability = r.ReadInt32()
                };
            }

            // It seems that trailers are listed in the save game so this is not necessary. Better to be safe however
            // because a missing trailer reference is fatal for the simulation thread.
            if ( t == typeof( VehicleInfo.VehicleTrailer ) )
            {
                string name = r.ReadString();
                string fullName = p.packageName + "." + name;
                VehicleInfo vi = Get<VehicleInfo>(p, fullName, name, false);

                VehicleInfo.VehicleTrailer trailer;
                trailer.m_info = vi;
                trailer.m_probability = r.ReadInt32();
                trailer.m_invertProbability = r.ReadInt32();
                return trailer;
            }

            // Sub-buildings in buildings.
            if ( t == typeof( BuildingInfo.SubInfo ) )
            {
                string name = r.ReadString();
                string fullName = p.packageName + "." + name;
                BuildingInfo bi = null;

                if ( fullName == AssetLoader.Current || name == AssetLoader.Current )
                    Profiler.Trace( "{0} wants to be a sub-building for itself.", fullName );
                else
                    bi = Get<BuildingInfo>( p, fullName, name, true );

                BuildingInfo.SubInfo subInfo = new BuildingInfo.SubInfo();
                subInfo.m_buildingInfo = bi;
                subInfo.m_position = r.ReadVector3();
                subInfo.m_angle = r.ReadSingle();
                subInfo.m_fixedHeight = r.ReadBoolean();
                return subInfo;
            }

            var ret = OriginalDeserializer(p, t, r);
            return ret;
        }

        // Works with (fullName = asset name), too.
        static T Get<T>( string fullName ) where T : PrefabInfo
        {
            if ( string.IsNullOrEmpty( fullName ) )
                return null;

            T info = PrefabCollection<T>.FindLoaded(fullName);

            if ( info == null && AssetLoader.instance.LoadAsset( fullName, UsedAssets.FindAsset( fullName ) ) )
                info = PrefabCollection<T>.FindLoaded( fullName );

            return info;
        }

        // For sub-buildings, name may be package.assetname.
        static T Get<T>( Package package, string fullName, string name, bool tryName ) where T : PrefabInfo
        {
            T info = PrefabCollection<T>.FindLoaded(fullName);

            if ( info == null && tryName )
                info = PrefabCollection<T>.FindLoaded( name );

            if ( info == null )
            {
                Package.Asset data = package.Find(name);

                if ( data == null && tryName )
                    data = UsedAssets.FindAsset( name ); // yes, name

                if ( data != null )
                    fullName = data.fullName;
                else if ( name.Contains( "." ) )
                    fullName = name;

                if ( AssetLoader.instance.LoadAsset( fullName, data ) )
                    info = PrefabCollection<T>.FindLoaded( fullName );
            }

            return info;
        }

    }
}
