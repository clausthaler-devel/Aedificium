using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using ICities;
using ColossalFramework.Plugins;
using ColossalFramework.PlatformServices;

namespace Aedificium
{
    abstract class BridgeBase
    {
        public ulong SteamId;

        private PluginManager.PluginInfo _pluginInfo;
        private PublishedFileId pluginId;
        private string modType;

        public BridgeBase( ulong pluginId )
        {
           this.pluginId = new PublishedFileId( pluginId );
        }
    
        public PluginManager.PluginInfo PluginInfo
        {
            get
            {
                if ( _pluginInfo == null && pluginId != PublishedFileId.invalid )
                    _pluginInfo = PluginManager.instance.GetPluginsInfo().FirstOrDefault(
                        m => ( m.publishedFileID == pluginId ) );

                return _pluginInfo;
            }

            set
            {
                _pluginInfo = value;
            }
        }

        public bool ModIsLoaded
        {
            get
            {
                return PluginInfo != null;
            }
        }
    }
}
