using ICities;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Packaging;

namespace Aedificium
{
    public sealed class Mod : IUserMod, ILoadingExtension
    {
        public string Name => "Aedificium";
        public string Description => "Load workshop items directly into your game.";

        public void OnEnabled() {}
        public void OnCreated(ILoading loading) {}
        public void OnDisabled() {}
        public void OnLevelUnloading() { }
        public void OnReleased() { }

        public void OnLevelLoaded(LoadMode mode)
        {
            Settings.helper = null;
            HotLoader.SetupWatcher();
            //GUI.DumpGameUI();
        }

        public void OnSettingsUI( UIHelperBase helper )
        {
            Settings.OnSettingsUI( helper );
        }


    }
}
