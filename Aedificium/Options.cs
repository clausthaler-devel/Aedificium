using System;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework.UI;
using ICities;

namespace Aedificium
{
    public class Options
    {
        static string FILE = "Aedificium.options";
        static string PATH = Path.Combine(
            Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
            @"Colossal Order\Cities_Skylines"
        );

        static Options singleton;
        public bool enablePopups = true;

        public static string workshopPath
        {
            get
            {
                if ( _workshopPath == null )
                    _workshopPath = Path.Combine(
                        ProgramFilesx86(),
                        @"Steam\SteamApps\workshop\content\255710"
                    );

                return _workshopPath;
            }
        }

        static string _workshopPath;

        // always returns the path of 'Program Files (x86)' regardless of Windows architecture
        static string ProgramFilesx86()
        {
            if ( 8 == IntPtr.Size
                || ( !String.IsNullOrEmpty( Environment.GetEnvironmentVariable( "PROCESSOR_ARCHITEW6432" ) ) ) )
            {
                return Environment.GetEnvironmentVariable( "ProgramFiles(x86)" );
            }

            return Environment.GetEnvironmentVariable( "ProgramFiles" );
        }

        public static Options instance
        {
            get
            {
                if ( singleton == null )
                    singleton = Instance();

                return singleton;
            }
        }

        Options() { }

        static Options Instance()
        {
            Options newInstance;

            try
            {
                var serializer = new XmlSerializer(typeof(Options));
                var path = Path.Combine( PATH, FILE );

                if ( File.Exists( path ) )
                {
                    using ( StreamReader reader = new StreamReader( path ) )
                        newInstance = (Options) serializer.Deserialize( reader );

                    return newInstance;
                }
            }
            catch ( Exception ) { }

            newInstance = new Options();
            newInstance.enablePopups = true;

            return newInstance;
        }

        public void Save()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Options));

                using ( StreamWriter writer = new StreamWriter( Path.Combine( PATH, FILE ) ) )
                    serializer.Serialize( writer, this );
            }
            catch ( Exception e )
            {
                UnityEngine.Debug.LogException( e );
            }
        }

        internal void OnSettingsUI( UIHelperBase helper )
        {
            UIHelper group1 = helper.AddGroup("RICO Options") as UIHelper;
            Check( group1, "Enable Popups", "Displays a popup when a new asset is downloaded.", enablePopups, b => { enablePopups = b; Save(); } );
        }

        void Check( UIHelper group, string text, string tooltip, bool enabled, OnCheckChanged action )
        {
            try
            {
                UIComponent check = group.AddCheckbox(text, enabled, action) as UIComponent;

                if ( tooltip != null )
                    check.tooltip = tooltip;
            }
            catch ( Exception e )
            {
                UnityEngine.Debug.LogException( e );
            }
        }
    }
}
