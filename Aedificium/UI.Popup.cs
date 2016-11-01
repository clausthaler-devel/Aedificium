using ColossalFramework;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Aedificium
{
#if DEBUG
    [ProfilerAspect()]
#endif
    public class PopupPanel : UIPanel
    {
        PrefabInfo currentPrefab;
        UIButton button;
        static GameObject _gameObject;
		static PopupPanel _instance;
        static PopupPanel instance
        {
            get
            {
                if ( _instance == null )
                    _instance = new PopupPanel();
                return _instance;
            }
        }

        public static void Initialize()
        {
            Profiler.Trace( "Initialize" );
            try
            {
                // Destroy the UI if already exists
                _gameObject = GameObject.Find( "IngamePackageImporterPopupPanel" );
                Destroy();

                // Creating our own gameObect, helps finding the UI in ModTools
                _gameObject = new GameObject( "IngamePackageImporterPopupPanel" );
                _gameObject.transform.parent = UIView.GetAView().transform;
                _instance = _gameObject.AddComponent<PopupPanel>();
            }
            catch ( Exception e )
            {
                Debug.LogException( e );
            }
            Profiler.Trace( "Done Initialize" );
        }

        public static void Destroy()
        {
            try
            {
                if ( _gameObject != null )
                    GameObject.Destroy( _gameObject );
            }
            catch ( Exception e )
            {
                Debug.LogException( e );
            }
        }

        public void Toggle()
        {
            Profiler.Trace( "Toggle" );
            if ( isVisible )
                Hide();
            else
                Show( true );
            Profiler.Trace( "Done Toggle" );
        }

        public void AutoToggle( int interval )
        {
            

            //var me = this;
            //var t = new Thread( () => {
            //    Thread.Sleep(interval);
            //    if ( me.Invoke( this.Invoke())
            //    Toggle();
            //});
            //t.Start();
        }

        public override void Start()
        {
            Profiler.Trace( "Start" );
            base.Start();

            try
            {
                backgroundSprite = "TutorialBubbleLeft";
                isVisible = false;
                canFocus = true;
                isInteractive = true;
                width = 256;
                height = 256;
                relativePosition = new Vector3( GetUIView().fixedWidth -300, GetUIView().fixedHeight - 300 );
                SetupControls();
            }
            catch (Exception e)
            {
                Profiler.Error( "Error Start", e );
                Destroy();
            }
            Profiler.Trace( "Done Start" );
        }

        private void SetupControls()
        {
            Profiler.Trace( "SetupControls" );
            createLabel();
            button = createButton();
            createCloseButton();

            Profiler.Trace( "Done SetupControls" );
        }

        public static void Show( PrefabInfo prefab )
        {
            Profiler.Trace( "Show Popup prefab {0}", prefab );
            instance.currentPrefab = prefab;
            instance.updateButton();
            instance.Show();
            Profiler.Trace( "Done Show Popup" );
            instance.Invoke( "Hide", 15000 );
            Profiler.Trace( "Set autotoggle" );
        }

		UILabel createLabel()
        {
            Profiler.Trace( "Create Label" );
            var label = this.AddUIComponent<UILabel>();
            label.name = "IngamePackageImporterLabel";
            label.size = new Vector2( this.width, 30f );
            label.textScale = 1f;
            label.height = 10;
            label.width = 108;
            label.textColor = new Color32( 21, 114, 37, 255 );
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.textAlignment = UIHorizontalAlignment.Left;
            label.text = "New workshop subscription!";
            label.relativePosition = new Vector3( 12, 8 );
            Profiler.Trace( "Done Create Label {0}", label );
            return label;
        }

        UIButton createCloseButton()
        {
            var button = this.AddUIComponent<UIButton>();
            button.atlas = Resources.FindObjectsOfTypeAll<UITextureAtlas>().FirstOrDefault( a => a.name == "Ingame" );
            button.name = "IngamePackageImporterCloseButton";
            button.normalFgSprite = "buttonclose";
            button.focusedFgSprite = "buttonclosehover";
            button.hoveredFgSprite = "buttonclosehover";
            button.pressedFgSprite = "buttonclosepressed";
            button.disabledFgSprite = "buttonclosepressed";
            button.normalBgSprite = "";
            button.size = new Vector2( 20, 20 );
            button.relativePosition = new Vector2( 228, 4 );
            button.eventClicked += ( UIComponent sender, UIMouseEventParameter ea ) => {
                Hide();
            };

            return button;
        }

        UIButton createButton()
        {
            Profiler.Trace( "Create Button" );

            var buttonPanel = this.AddUIComponent<UIPanel>();
            buttonPanel.size = new Vector2( 192, 192 );
            buttonPanel.relativePosition = new Vector2( 32, 32 );
            buttonPanel.atlas = Resources.FindObjectsOfTypeAll<UITextureAtlas>().FirstOrDefault( a => a.name == "Ingame" );
            buttonPanel.backgroundSprite = "ButtonMenu";

            var button = GUI.CreateBuildingButton( buttonPanel, "IngamePackageImporterPlopperButton" );
            button.size = new Vector2( 192, 192 );
            button.relativePosition = new Vector2( 0, 0 );
            Profiler.Trace( "Done Create Button {0}", button );

            button.eventClicked += ( UIComponent btn, UIMouseEventParameter e ) => {
                var buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
                buildingTool.m_prefab = (BuildingInfo) btn.objectUserData;
                buildingTool.m_relocate = 0;
            };

            return button;
        }

		void updateButton()
        {
            
            Profiler.Trace( "Update Button prefab {0}", currentPrefab );
            Profiler.Trace( "Update Button button {0}", button );
            GUI.UpdateBuildingButton( button, currentPrefab );
            Profiler.Trace( "Done Update Button" );
        }
    }
}
