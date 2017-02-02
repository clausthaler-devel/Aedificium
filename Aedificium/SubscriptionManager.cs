using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using ColossalFramework.PlatformServices;


namespace Aedificium
{
    public class SubscriptionManager : MonoBehaviour, ISimulationManager
    {
        private List<PublishedFileId> SubscriptionCandidates = new List<PublishedFileId>();
        private object threadLock = new object();
        bool WorkToDo = false;

        void Tick()
        {
            try
            {
                lock ( threadLock )
                {
                    Profiler.Trace( "Lock aquired, {0} subscription candidates", SubscriptionCandidates.Count );
                    foreach ( var package in SubscriptionCandidates )
                    {
                        Profiler.Trace( "Ticking Subs {0} of {1} candidates", package, SubscriptionCandidates.Count );
                        if ( PlatformService.workshop.Subscribe( package ) )
                        {
                            Profiler.Trace( "Subscribed" );
                            SubscriptionCandidates.Remove( package );
                            if ( SubscriptionCandidates.Count == 0 )
                            {
                                Profiler.Trace( "All items subscribed" );
                                WorkToDo = false;
                            }
                            return;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "SubscriptionManager.Tick ", ex );
            }
        }

        public static void AddSubscriptions( List<string> subscriptions )
        {
            Profiler.Trace( "AddSubscriptions before " + String.Join( " // ", subscriptions.ToArray() ) );
            var ids = subscriptions.Select( s => Util.GetPackageId( s ) ).ToList();
            Profiler.Trace( "AddSubscriptions after " + String.Join( " // ", ids.Select( n => n.ToString() ).ToArray() ) );
            AddSubscriptions( ids );
        }

        public static void AddSubscriptions( List<PublishedFileId> subscriptions )
        {
            try
            {
                lock ( instance.threadLock )
                {
                    foreach ( var candidate in subscriptions )
                        AddSubscription( candidate );
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "SubscriptionManager.AddSubscriptions", ex );
            }
        }

        public static void AddSubscription( PublishedFileId id )
        {
            try
            {
                lock ( instance.threadLock )
                {
                    if ( !instance.SubscriptionCandidates.Contains( id ) )
                    {
                        Profiler.Trace( "Adding subscription {0}", id.ToString() );
                        instance.SubscriptionCandidates.Add( id );
                        instance.WorkToDo = true;
                    }
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "SubscriptionManager.AddSubscription", ex );
            }

        }


        // Boring boilerplate

        static SubscriptionManager instance;

        public static void Register()
        {
            instance = new SubscriptionManager();
            SimulationManager.RegisterSimulationManager( instance );
        }

        public virtual void GetData( FastList<ColossalFramework.IO.IDataContainer> data ) { }
        public virtual string GetName() { return gameObject.name; }
        public virtual ThreadProfiler GetSimulationProfiler() { return new ThreadProfiler(); }
        public virtual void LateUpdateData( SimulationManager.UpdateMode mode ) { }
        public virtual void UpdateData( SimulationManager.UpdateMode mode ) { }
        public virtual void EarlyUpdateData() { }

        int ticker;
        public virtual void SimulationStep( int subStep )
        {
            if ( WorkToDo && ++ticker % 50 == 0 )
                try { ticker = 0; Profiler.Trace( "Tick SubsManager" ); Tick(); }
                catch ( Exception ex ) { Profiler.Error( "SubscriptionManager Error", ex ); }
        }
    }
}
