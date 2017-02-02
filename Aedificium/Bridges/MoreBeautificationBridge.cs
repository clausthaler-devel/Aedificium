using System;
using UnityEngine;
using MoreBeautification;
using ColossalFramework.UI;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    class MoreBeautificationBridge : BridgeBase
    {
        public const ulong ModSteamId = 505480567;

        public MoreBeautificationBridge() : base( ModSteamId )
        {
        }

        public void UpdateAssets()
        {
            if ( ModIsLoaded )
            {
                //foreach ( var x in UIView.GetAView().GetComponentsInChildren( typeof( EditorPropsPanel ) ) )
                //{
                //    Profiler.Trace( "C1 {1} {0}", x.name, x.GetType() );
                //}

                //var c = UIView.GetAView().FindUIComponent( "PropsCommon" );
                //Profiler.Trace( "C2 {1} {0}", c.name, c.GetType() );

                
            
                //var Obj = GameObject.FindObjectOfType( typeof ( MoreBeautification.Initializer ) );
                //Profiler.Trace( "Inirializer {0}", Obj );

                //var initializer = GameObject.Find("MoreBeautificationInitializer");
                //Profiler.Trace( "Inirializer2 {0}", initializer );
                //foreach ( var c in initializer.GetComponents( typeof( Initializer ) ) )
                //    Profiler.Trace( "C 1 {0}" );
                //foreach ( var c in initializer.GetComponents<Initializer>() )
                //    Profiler.Trace( "C 2 {0}" );
                //foreach ( var c in initializer.GetComponentsInChildren( typeof(Initializer) ) )
                //    Profiler.Trace( "C 3 {0}" );
                //foreach ( var c in initializer.GetComponentsInChildren<Initializer>() )
                //    Profiler.Trace( "C 4 {0}" );

                //if ( Obj != null )
                //{
                //    try
                //    {
                //        var Init = (Initializer) Obj;
                //        Init.Update();
                //    }
                //    catch
                //    {
                //        Profiler.Trace( "Arrgh" );
                //    }
                //}
            }
        }
    }
}