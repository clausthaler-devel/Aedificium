using ICities;
using UnityEngine;

namespace Aedificium 
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public class AedificiumMod : IUserMod
    {

        public void OnSettingsUI( UIHelperBase helper ) => Options.instance.OnSettingsUI( helper );

        public string Name
        {
            get
            {
                return "Aedificium";
            }
        }

        public string Description
        {
            get
            {
                return "Automatically loads newly subscribed to assets while you play.";
            }
        }
    }
}


