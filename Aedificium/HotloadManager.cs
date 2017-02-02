using System;
using System.Collections.Generic;

using UnityEngine;
using ColossalFramework.PlatformServices;

namespace Aedificium
{

    public class HotloadManager : UnityEngine.MonoBehaviour, ISimulationManager
    {
        static HotloadManager instance;
        private List<HotloadCandidate> InstallCandidates = new List<HotloadCandidate>();
        private object threadLock = new object();
        bool WorkToDo = false;
        TimeSpan OneMinute = TimeSpan.FromSeconds( 60 );

        void Tick()
        {
            try
            {
                lock ( threadLock )
                {
                    foreach ( var candidate in InstallCandidates )
                    {
                        Profiler.Trace( "Tick Load candidate {0}, {1} dependencies left, {2} candidates total", candidate.PackageId, candidate.Dependencies.Count, InstallCandidates.Count );

                        // All dependencies loaded
                        if ( candidate.Dependencies.Count == 0 || DateTime.Now > candidate.LastTouched  + OneMinute )
                        {
                            Profiler.Trace( "Subscribing to {0}", candidate.PackageId );
                            InstallCandidates.Remove( candidate );
                            // Trigger Install
                            if ( !PlatformService.workshop.Subscribe( candidate.PackageId ) )
                            {
                                Profiler.Trace( "Cannot subscribe." );
                            }

                            if ( InstallCandidates.Count == 0 )
                            {
                                Profiler.Trace( "All candidates installed" );
                                instance.WorkToDo = false;
                            }

                            return;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "HotloadManager.Tick ", ex );
            }
        }

        public static bool DeferLoad( string fullName, List<string> dependencies )
        {
            try
            {
                lock ( instance.threadLock )
                {
                    Profiler.Trace( "Defer Load of {1} and {0} dependencies", dependencies.Count, fullName );

                    var candidate = new HotloadCandidate( fullName, dependencies );

                    Profiler.Trace( "Adding candidate" );
                    instance.InstallCandidates.Add( candidate );
                    instance.WorkToDo = true;
                    
                    if ( PlatformService.workshop.Unsubscribe( candidate.PackageId ) )
                        Profiler.Trace( "Unsubscribed from {0}", candidate.PackageId );

                    Profiler.Trace( "Adding {0} new subscriptions", dependencies.Count );
                    SubscriptionManager.AddSubscriptions( dependencies );
                }
            }
            catch ( Exception ex )
            {
                Profiler.Error( "DeferLoad " + fullName, ex );
            }

            return false;
        }

        public static void ReportProcessed( PublishedFileId id )
        {
            Profiler.Trace( "Reports as processed {0}", id );
            try
            {
                lock ( instance.threadLock )
                {
                    foreach ( var candidate in instance.InstallCandidates )
                    {
                        if ( candidate.Dependencies.Contains( id ) )
                        {
                            candidate.Dependencies.Remove( id );
                            candidate.LastTouched = DateTime.Now;
                        }
                    }
                }
            }
            catch ( Exception ex)
            {
                Profiler.Error( "ReportProcessed", ex );
            }
        }



        // Boring boilerplate

        ulong i;
                
        public static void Register()
        {
            instance = new HotloadManager();
            SimulationManager.RegisterSimulationManager( instance );
        }

        public virtual void GetData( FastList<ColossalFramework.IO.IDataContainer> data ) { }
        public virtual string GetName() { return gameObject.name; }
        public virtual ThreadProfiler GetSimulationProfiler() { return new ThreadProfiler(); }
        public virtual void LateUpdateData( SimulationManager.UpdateMode mode ) { }
        public virtual void UpdateData( SimulationManager.UpdateMode mode ) { }
        public virtual void EarlyUpdateData() { }

        int ticker;
        public virtual void SimulationStep( int subStep  )
        {
            if ( WorkToDo && ++ticker % 50 == 0 )
                try { ticker = 0; Profiler.Trace( "Tick HotloadManager" ); Tick(); }
                catch ( Exception ex ) { Profiler.Error( "SimulationStep", ex ); }
        }
    }
}
