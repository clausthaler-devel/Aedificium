using System;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using ColossalFramework.PlatformServices;

namespace Aedificium
{
    public static class Util
    {

        public static object GetField(object instance, string field)
        {
            return instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        }

        public static object GetStaticField(Type type, string field)
        {
            return type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        public static PublishedFileId GetPackageId(string packageName)
        {
            ulong id;

            return
                packageName.Length >= 9 && ulong.TryParse( packageName.Substring(0,9), out id ) ? 
                new PublishedFileId( id ) : 
                PublishedFileId.invalid;
        }
    }
}
