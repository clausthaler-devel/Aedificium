using System;
using System.Linq;
using System.Collections.Generic;

using ColossalFramework.PlatformServices;

namespace Aedificium
{
    class HotloadCandidate
    {
        public PublishedFileId PackageId;
        public List<PublishedFileId> Dependencies;

        public String Name;
        public String FullName;
        public DateTime LastTouched;

        public HotloadCandidate( string fullName, List<string> dependencies )
        {
            // Silently filter for very old and private packages
            Dependencies = dependencies
                .Select( p => Util.GetPackageId( p ) ).ToList()
                .FindAll( p => p != PublishedFileId.invalid );

            FullName = fullName;
            PackageId = Util.GetPackageId( fullName );

            // Valid package so cut off the Name
            if ( PackageId != PublishedFileId.invalid )
                Name = fullName.Substring( 10 );

            LastTouched = DateTime.Now;
        }
    }
}

