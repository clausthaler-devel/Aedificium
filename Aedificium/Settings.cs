using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework.PlatformServices;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace Aedificium
{
    public class Settings
    {
        const string FILENAME = "Aedificium.xml";

        public int version = 1;
        public bool installSubscriptions = false;
        public bool installDependencies = false;
        public bool enableRico = false;
        public Profiler.DebugLevels DebugLevel = Profiler.DebugLevels.Warning;

        [XmlIgnoreAttribute]
        bool dirty = false;

        static Settings _instance;
        public static UIHelperBase helper;
        static bool Dirty => instance != null && instance.dirty;

        public static Settings instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();

                return _instance;
            }
        }

        Settings() { }

        static Settings Load()
        {
            Settings s;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));

                using (StreamReader reader = new StreamReader(FILENAME))
                    s = (Settings) serializer.Deserialize(reader);
            }
            catch (Exception) { s = new Settings(); }

            return s;
        }

        void Save()
        {
            try
            {
                dirty = false;
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));

                using (StreamWriter writer = new StreamWriter(FILENAME))
                    serializer.Serialize(writer, this);
            }
            catch (Exception e)
            {
                Profiler.Trace( "Settings.Save" );
                UnityEngine.Debug.LogException(e);
            }
        }

        public static void OnSettingsUI(UIHelperBase newHelper)
        {
            UIComponent comp = Self(helper);

            if (comp != null)
                comp.eventVisibilityChanged -= OnVisibilityChanged;

            helper = newHelper;
            comp = Self(newHelper);
            comp.eventVisibilityChanged -= OnVisibilityChanged;
            comp.eventVisibilityChanged += OnVisibilityChanged;
        }

        static UIComponent Self(UIHelperBase h) => ((UIHelper) h)?.self as UIComponent;

        static void OnVisibilityChanged(UIComponent comp, bool visible)
        {
            if (visible && comp == Self(helper) && comp.childCount == 0)
                instance.LateSettingsUI(helper);
            else if (!visible && Dirty)
                instance.Save();
        }

        UIHelper group;

        void LateSettingsUI(UIHelperBase helper)
        {
            group = CreateGroup(helper, "All hail the FSM", "For those who follow its teachings are generally nice people.");
            Check( group, "Hotload Workshop-Items", "Load items you fetched from the workshop directly into your game.", installSubscriptions, b => { installSubscriptions = b; dirty = true; } );
            Check( group, "Subscribe to dependencies.", "If a hotloaded item has dependencies, subscribe to - and thereby hotload - them too.", installDependencies, b => { installDependencies = b; dirty = true; } );
            Check( group, "Apply RICO settings (requires RICO mod).", "When disabled, RICO buildings are loaded as ordinary Unique Buildings.", enableRico, b => { enableRico  = b; dirty = true; } );
        }


        UIHelper CreateGroup(UIHelperBase parent, string name, string tooltip = null)
        {
            UIHelper group = parent.AddGroup(name) as UIHelper;
            UIPanel content = group.self as UIPanel;
            content.name = name + "Panel";
            UIPanel container = content?.parent as UIPanel;
            RectOffset rect = content?.autoLayoutPadding;

            if (rect != null)
                rect.bottom /= 2;

            rect = container?.autoLayoutPadding;

            if (rect != null)
                rect.bottom /= 4;

            if (!string.IsNullOrEmpty(tooltip))
            {
                UILabel label = container?.Find<UILabel>("Label");

                if (label != null)
                    label.tooltip = tooltip;
            }

            return group;
        }

        void Check(UIHelper group, string text, string tooltip, bool enabled, OnCheckChanged action )
        {
            try
            {
                UIComponent check = group.AddCheckbox( text, enabled, ( bool c ) => {  action( c );  } ) as UIComponent;

                if (tooltip != null)
                    check.tooltip = tooltip;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        void TextField(UIHelper group, string text, OnTextChanged action)
        {
            try
            {
                UITextField field = group.AddTextfield(" ", text, action, null) as UITextField;
                field.width *= 2.8f;
                UIComponent parent = field.parent;
                UILabel label = parent?.Find<UILabel>("Label");

                if (label != null)
                {
                    float h = label.height;
                    label.height = 0; label.Hide();
                    parent.height -= h;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        void Button(UIHelper group, string text, string tooltip, OnButtonClicked action)
        {
            try
            {
                UIButton button = group.AddButton(text, action) as UIButton;
                button.textScale = 0.875f;

                if (tooltip != null)
                    button.tooltip = tooltip;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

    }
}
