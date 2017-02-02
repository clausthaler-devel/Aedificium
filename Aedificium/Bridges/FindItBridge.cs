using System;
using System.Linq;
using System.Reflection;
using FindIt;

namespace Aedificium
{
    class FindItBridge : BridgeBase
    {
        public const ulong ModSteamId = 837734529;

        public FindItBridge() : base( ModSteamId )
        {
        }

        public void UpdateAssets()
        {
            if ( ModIsLoaded )
            {
                FindIt.FindIt.list.Init();
                FindIt.FindIt.instance?.searchBox?.Search();
            }
        }
    }
}
