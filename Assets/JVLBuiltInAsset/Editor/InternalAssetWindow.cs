using System;
using System.Linq;
using System.Reflection;
using System.Text;
using JVLBuiltInAsset.Editor.TabsEditor;
using UnityEditor;
using UnityEngine;

namespace JVLBuiltInAsset.Editor {
    public class InternalAssetWindow : EditorWindow {
        private AssetBundle m_internalAssetBundle;
        private string[] m_assetsName;
        private string[] m_materialsPath;
        private string[] m_shadersPath;

        private static InternalAssetWindow _window;
        Vector2 m_scrollPos;
        private int m_selectedTab;

        private GUIStyle m_tabStyle;
        private GUIStyle m_infoStyle;

        private ImageTab m_imageTab;

        private int m_bundleAssetCnt;
        private int m_imgAssetCnt;
        private int m_matAssetCnt;
        private int m_shaderAssetCnt;
        private int m_unknowAssetCnt;


        [MenuItem("JvL Tools/Builtin Asset Editor", priority = 0)]
        public static void OpenWindow() {
//            m_window = CreateInstance<InternalAssetWindow>();
//            m_window.ShowUtility();

            _window = GetWindow<InternalAssetWindow>();
            _window.titleContent = EditorGUIUtility.TrTextContent("JvL Internal Asset");
            _window.minSize = new Vector2(800, 800);
            _window.maxSize = new Vector2(800, 800f);

            _window.InitToolResources();
            _window.GetInternalAssetBundle();

            _window.GetAllAssetToLog();
            _window.InitTabEditor();
        }

        public static InternalAssetWindow Window {
            get { return _window; }
        }

        private void InitToolResources() {
            m_tabStyle = new GUIStyle(EditorStyles.toolbar) {fixedHeight = 25f};
            m_infoStyle = new GUIStyle(EditorStyles.toolbar) {fixedHeight = 70f};
        }

        private void GetInternalAssetBundle() {
            var t = Type.GetType("UnityEditor.EditorGUIUtility,UnityEditor.dll");
            if (t != null) {
                var m = t.GetMethod("GetEditorAssetBundle", BindingFlags.Static | BindingFlags.NonPublic);
                if (m != null) 
                    m_internalAssetBundle = m.Invoke(null, null) as AssetBundle;
            }
        }

        private void InitTabEditor() {
            m_imageTab = new ImageTab(m_internalAssetBundle, this);
        }

        private void GetAllAssetToLog() {
            if (m_internalAssetBundle != null) m_assetsName = m_internalAssetBundle.GetAllAssetNames();
            m_imgAssetCnt = m_assetsName.Count(x => x.EndsWith(".asset") || x.EndsWith(".png"));
            m_matAssetCnt = m_assetsName.Count(x => x.EndsWith(".mat"));
            m_shaderAssetCnt = m_assetsName.Count(x => x.EndsWith(".shader"));
            m_unknowAssetCnt = m_assetsName.Length - m_imgAssetCnt - m_matAssetCnt - m_shaderAssetCnt;
        }

        private void OnGUI() {
            if (m_imageTab == null)
                m_imageTab = new ImageTab(m_internalAssetBundle, this);

            StringBuilder sb = new StringBuilder("Found ").Append(m_assetsName.Length).AppendLine(" assets")
                .Append("Found Images    :").Append(m_imgAssetCnt).AppendLine()
                .Append("Found materials :").Append(m_matAssetCnt).AppendLine()
                .Append("Found shaders   :").Append(m_shaderAssetCnt).AppendLine()
                .Append("Found Unknow   :").Append(m_unknowAssetCnt);
            string info = sb.ToString();

            EditorGUILayout.BeginHorizontal(m_tabStyle);
            {
                GUILayoutOption[] para = {GUILayout.Width(300)};
                GUILayout.FlexibleSpace();
//                m_selectedTab = GUILayout.Toolbar(m_selectedTab, new[] {"Image", "Material", "Shader", "Unknow"}, para);
                m_selectedTab = GUILayout.Toolbar(m_selectedTab, new[] {"Image"}, para);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(m_infoStyle);
            EditorGUILayout.HelpBox(info, MessageType.Info);
            EditorGUILayout.EndHorizontal();


            Event e = Event.current;

            switch (m_selectedTab) {
                case 0:
                    m_imageTab.Draw();
                    m_imageTab.ProccessEvent(e);
                    break;
            }
        }
    }
}