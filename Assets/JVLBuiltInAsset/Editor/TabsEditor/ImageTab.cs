using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using EditorExt = UnityEditor.Editor;

namespace JVLBuiltInAsset.Editor.TabsEditor {
    public class ImageTab : TabBase {
        private readonly InternalAssetWindow m_window;
        private readonly AssetBundle m_bundle;
        private string[] m_imagePaths;

        private ImageInfo[] m_info;

        private int m_totalPage;
        private int m_pageIdx = 1;
        private int m_itemPerPage = 50;
        private float m_rowHeigh = 20f;

        private int m_from;
        private int m_to;

        private ImageInfo m_selectingInfo;

        private Color m_oddColor = new Color(0.32f, 0.32f, 0.32f, 0.25f);
        private Color m_evenColor = new Color(0.11f, 0.11f, 0.11f, 0.55f);
        private readonly Color m_selected = new Color(0.06f, 0.5f, 1f, 0.38f);
//        private readonly Color m_lineColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        private Rect m_gridViewRect;
        private Rect m_previewRect;

        private EditorExt m_editor;
        private bool m_disablePreviewArea;


        #region GUILayoutOption

        /*
         * To prevent the recreate of GUILayoutOtion array when call in OnGUI, I decide to cache all the format array
         */

        private readonly GUILayoutOption[] m_previewRectOpt = {GUILayout.Height(105), GUILayout.Width(790)};

        private readonly GUILayoutOption[] m_previewSelectedMesOpt = {
            GUILayout.Height(96),
            GUILayout.MinWidth(500),
            GUILayout.MaxWidth(550)
        };

        private readonly GUILayoutOption[] m_previewDefaultMesIconOpt = {GUILayout.Width(20), GUILayout.Height(40)};
        private GUILayoutOption[] m_gridCollumnOpt;
        private GUILayoutOption[] m_gridAreaOpt;
        private readonly GUILayoutOption[] m_nullParamOpt = new GUILayoutOption[0];
        private readonly GUILayoutOption[] m_buttonOpt = {GUILayout.Height(20)};
        private readonly GUILayoutOption[] m_previewImgRectOpt = {GUILayout.Width(100), GUILayout.Height(100)};

        #endregion

        public ImageTab(AssetBundle bundle, InternalAssetWindow window) {
            m_window = window;
            m_bundle = bundle;
            Init();
        }

        private class ImageInfo {
            public string BundlePath;
            public Texture2D Texture;
            public int OrderInBundle;
            public int OrderDisplay;
        }


        public override void ProccessEvent(Event e) {
            switch (e.type) {
                case EventType.MouseDown:
                    if (!m_gridViewRect.Contains(e.mousePosition) &&
                        !m_previewRect.Contains(e.mousePosition))
                        ResetSelected();
                    break;
                case EventType.KeyDown:
                    switch (e.keyCode) {
                        case KeyCode.UpArrow:
                            if (m_selectingInfo == null) {
                                int idx = m_pageIdx * m_itemPerPage - m_itemPerPage;
                                m_selectingInfo = m_info[idx];
                            }
                            else {
                                int idx = Array.IndexOf(m_info, m_selectingInfo);
                                idx = Mathf.Clamp(idx - 1, 0, m_imagePaths.Length - 1);
                                m_selectingInfo = m_info[idx];
                                m_pageIdx = Mathf.Clamp(
                                    idx % m_itemPerPage == 0
                                        ? Mathf.CeilToInt((idx + 1) / (float) m_itemPerPage)
                                        : Mathf.CeilToInt(idx / (float) m_itemPerPage), 1, m_totalPage);
                            }

                            m_window.Repaint();
                            break;
                        case KeyCode.DownArrow:
                            if (m_selectingInfo == null) {
                                int idx = m_pageIdx * m_itemPerPage - m_itemPerPage;
                                m_selectingInfo = m_info[idx];
                            }
                            else {
                                int idx = Array.IndexOf(m_info, m_selectingInfo);
                                idx = Mathf.Clamp(idx + 1, 0, m_imagePaths.Length - 1);
                                m_selectingInfo = m_info[idx];
                                m_pageIdx = Mathf.Clamp(
                                    m_selectingInfo.OrderDisplay == 1
                                        ? Mathf.CeilToInt((idx + 1) / (float) m_itemPerPage)
                                        : Mathf.CeilToInt(idx / (float) m_itemPerPage), 1, m_totalPage);

                                m_window.Repaint();
                            }

                            break;
                    }

                    break;
            }
        }

        private static class Style {
            public static readonly GUIStyle PreviewMesDefaultGuiStyle = new GUIStyle(EditorStyles.label)
                {wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = 15};

            public static readonly GUIStyle PreviewMessSelectGuiStyle = new GUIStyle(EditorStyles.helpBox)
                {wordWrap = true, fontSize = 11};

            public static readonly GUIStyle GridRowGuiStyle = new GUIStyle(EditorStyles.label)
                {alignment = TextAnchor.MiddleLeft};

            public static readonly GUIStyle PreviewImageRectStyle = new GUIStyle(EditorStyles.helpBox)
                {alignment = TextAnchor.MiddleCenter};
        }

        protected sealed override void Init() {
            m_imagePaths = m_bundle.GetAllAssetNames().Where(x => x.EndsWith(".asset") || x.EndsWith(".png")).ToArray();
            m_info = new ImageInfo[m_imagePaths.Length];
            int itemCnt = 1;
            for (var i = 0; i < m_imagePaths.Length; i++) {
                string path = m_imagePaths[i];
                Texture2D t = null;
                if (m_bundle != null) {
                    t = m_bundle.LoadAsset<Texture2D>(path);
                }

                ImageInfo info = new ImageInfo
                    {OrderInBundle = i + 1, BundlePath = path, OrderDisplay = itemCnt, Texture = t};

                m_info[i] = info;
                itemCnt++;
                if (itemCnt > m_itemPerPage)
                    itemCnt = 1;
            }

            m_totalPage = Mathf.CeilToInt(m_imagePaths.Length / (float) m_itemPerPage);
            m_oddColor = new Color(0.32f, 0.32f, 0.32f, 0.25f);
            m_evenColor = EditorGUIUtility.isProSkin
                ? new Color(0.11f, 0.11f, 0.11f, 0.55f)
                : new Color(0.82f, 0.82f, 0.82f, 0.55f);
            m_gridCollumnOpt = new[] {GUILayout.Height(m_rowHeigh * (m_itemPerPage / 2f)), GUILayout.Width(390)};
            m_gridAreaOpt = new[] {GUILayout.Width(800), GUILayout.Height(m_rowHeigh * (m_itemPerPage / 2f))};
        }

        /// <summary>
        /// Draw GUI, only call from OnGUI of InternalAssetWindow.cs
        /// </summary>
        public override void Draw() {
            DrawImageGrid();
            DrawPageCount();
            DrawPreviewArea();
        }

        /// <summary>
        /// Draw image list grid
        /// </summary>
        private void DrawImageGrid() {
            m_from = m_pageIdx == 1 ? 0 : m_pageIdx * m_itemPerPage - m_itemPerPage;
            m_to = m_pageIdx * m_itemPerPage;
            int f = m_from;
            int t = m_to - m_itemPerPage / 2;
            int f1 = t;
            int t1 = m_to;
            GUILayout.Space(5);
            m_gridViewRect = EditorGUILayout.BeginHorizontal(m_gridAreaOpt);
            GUILayout.Space(3);
            DrawListColumnByEditorGui(f, t);
            GUILayout.FlexibleSpace();
            DrawListColumnByEditorGui(f1, t1);
            GUILayout.Space(3);
            EditorGUILayout.EndHorizontal();

//            Handles.BeginGUI();
//            Handles.DrawSolidRectangleWithOutline(m_gridView, new Color(1, 1, 1, 0), m_lineColor);
//            Handles.EndGUI();
        }

        /// <summary>
        /// Draw image list grid collumn
        /// </summary>
        /// <param name="fromIdx">from index of the image info array</param>
        /// <param name="toIdx">to index of the image info array</param>
        private void DrawListColumnByEditorGui(int fromIdx, int toIdx) {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, m_gridCollumnOpt);
            for (var i = fromIdx; i < toIdx; i++) {
                if (i >= m_info.Length) {
                    break;
                }

                ImageInfo info = m_info[i];
                Color rowColor = i % 2 == 0 ? m_evenColor : m_oddColor;

                GUIContent orderDisplay = EditorGUIUtility.TrTextContent(info.OrderDisplay.ToString());
                GUIContent order = EditorGUIUtility.TrTextContent(info.OrderInBundle.ToString());
                GUIContent icon = EditorGUIUtility.TrIconContent(info.Texture);
                GUIContent path = EditorGUIUtility.TrTextContent(info.BundlePath);

                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(m_rowHeigh));
                if (m_selectingInfo == info) {
                    rowColor = m_selected;
                }

                Handles.DrawSolidRectangleWithOutline(rowRect, rowColor, new Color(1, 1, 1, 0));
                EditorGUILayout.LabelField(orderDisplay, Style.GridRowGuiStyle, GUILayout.Width(25));
                EditorGUILayout.LabelField(order, Style.GridRowGuiStyle, GUILayout.Width(35));
                EditorGUILayout.LabelField(icon, Style.GridRowGuiStyle, GUILayout.Width(50));
                EditorGUILayout.LabelField(path, Style.GridRowGuiStyle, m_nullParamOpt);

                EditorGUILayout.EndHorizontal();

                if (Event.current.type != EventType.MouseDown) continue;

                if (rowRect.Contains(Event.current.mousePosition)) {
                    m_selectingInfo = m_info[i];
                    m_window.Repaint();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Reset the selected image info
        /// </summary>
        private void ResetSelected() {
            m_selectingInfo = null;
            m_window.Repaint();
        }

        /// <summary>
        /// Draw page count
        /// </summary>
        private void DrawPageCount() {
            GUILayout.Space(3);
            GUILayout.BeginHorizontal(m_nullParamOpt);
            {
                GUILayout.FlexibleSpace();
                bool prevBtn = GUILayout.Button("<", m_nullParamOpt);
                if (prevBtn) {
                    m_pageIdx--;
                    ResetSelected();
                }

                m_pageIdx = EditorGUILayout.IntField(m_pageIdx, GUILayout.Width(30));

                GUILayout.Label("/ " + m_totalPage, GUILayout.Width(30));

                bool nextBtn = GUILayout.Button(">", m_nullParamOpt);
                if (nextBtn) {
                    m_pageIdx++;
                    ResetSelected();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.EndHorizontal();
            m_pageIdx = Mathf.Clamp(m_pageIdx, 1, m_totalPage);
        }


        /// <summary>
        /// Draw image preview area
        /// </summary>
        private void DrawPreviewArea() {
            Handles.BeginGUI();
            EditorGUI.BeginDisabledGroup(m_disablePreviewArea);
            GUILayout.BeginHorizontal(m_nullParamOpt);
            {
                m_previewRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox, m_previewRectOpt);
                {
//                    Handles.DrawSolidRectangleWithOutline(m_previewRect, new Color(1, 1, 1, 0), m_lineColor);
                    if (m_selectingInfo != null && m_selectingInfo.Texture != null) {
                        DrawPreviewSelected();
                    }
                    else {
                        DrawPreviewDefault();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            Handles.EndGUI();
        }

        /// <summary>
        /// Draw selected image info inside the preview area
        /// </summary>
        private void DrawPreviewSelected() {
            GUILayout.BeginHorizontal(m_nullParamOpt);
            {
                Texture2D t = m_selectingInfo.Texture;
                StringBuilder sb = new StringBuilder();
                bool isExportable = true;
                sb.Append("AssetPath: ")
                    .Append(m_selectingInfo.BundlePath).AppendLine()
                    .Append("Texture Info: ").AppendLine()
                    .Append("+ Format : ").Append(t.format).AppendLine()
                    .Append("+ Size      : ").Append(t.width).Append(" x ").Append(t.height).AppendLine()
                    .Append("+ Filter Mode   : ").Append(t.filterMode).AppendLine();
                if (t.format == TextureFormat.DXT1 || t.format == TextureFormat.DXT5) {
                    sb.Append(
                        "* Can not export this texture. The format of this texture is compressed or HDR texture formats ");
                    isExportable = false;
                }

                GUILayout.BeginVertical(m_nullParamOpt);
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(sb.ToString(), Style.PreviewMessSelectGuiStyle, m_previewSelectedMesOpt);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(m_nullParamOpt);
                {
                    GUILayout.FlexibleSpace();
                    if (t != null) {
                        GUIContent cont = EditorGUIUtility.TrIconContent(t);
                        GUILayout.Label(cont, Style.PreviewImageRectStyle, m_previewImgRectOpt);
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.Space(5);

                GUILayout.BeginVertical(m_nullParamOpt);
                {
                    GUILayout.Space(20);
                    EditorGUI.BeginDisabledGroup(!isExportable);
                    GUIContent btnContent = EditorGUIUtility.TrTextContent("Export Image",
                        "Export Uncompressed and non-HDR format image");
                    bool exportImage = GUILayout.Button(btnContent, EditorStyles.miniButton, m_buttonOpt);
                    if (exportImage) {
                        DoExportImage(m_selectingInfo);
                        AssetDatabase.Refresh();
                    }

                    EditorGUI.EndDisabledGroup();

                    btnContent = EditorGUIUtility.TrTextContent("Export All Image",
                        "Export all the exportable images, this process take times, so be patient when do this");
                    bool exportAllImg = GUILayout.Button(btnContent, EditorStyles.miniButton, m_buttonOpt);
                    if (exportAllImg) {
                        DoExportAllImage();
                        AssetDatabase.Refresh();
                    }

                    btnContent =
                        EditorGUIUtility.TrTextContent("Copy asset path to clipboard", "Copy builtin asset path");
                    bool copyPathToClipboard = GUILayout.Button(btnContent, EditorStyles.miniButton,
                        m_buttonOpt);
                    if (copyPathToClipboard) {
                        DoCopyPathToClipboard(m_selectingInfo.BundlePath);
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw default of the preview area
        /// </summary>
        private void DrawPreviewDefault() {
            GUIContent icon = EditorGUIUtility.TrIconContent("icons/d_console.infoicon.sml.png");
            GUIContent content =
                EditorGUIUtility.TrTextContent(
                    "Nothing to show, Please select the texture you want to work with");

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(m_nullParamOpt);
            {
                GUILayout.Space(6);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(icon, m_previewDefaultMesIconOpt);
                EditorGUILayout.LabelField(content, Style.PreviewMesDefaultGuiStyle, m_nullParamOpt);
                GUILayout.FlexibleSpace();
                GUILayout.Space(6);
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Excute export all the exportable image of builtin asset bundle
        /// </summary>
        private void DoExportAllImage() {
            int cnt = 0;
            int exportable = m_info.Count(x =>
                x.Texture != null && x.Texture.format != TextureFormat.DXT1 && x.Texture.format != TextureFormat.DXT5);
            for (var i = 0; i < m_info.Length; i++) {
                ImageInfo imageInfo = m_info[i];
                if (imageInfo.Texture == null ||
                    imageInfo.Texture.format == TextureFormat.DXT1 ||
                    imageInfo.Texture.format == TextureFormat.DXT5) {
                    continue;
                }

                DoExportImage(imageInfo);
                cnt++;
                if (cnt < exportable)
                    EditorUtility.DisplayProgressBar("Export Progress", cnt + "/" + exportable,
                        cnt / (float) exportable);
                else
                    EditorUtility.ClearProgressBar();
            }


            EditorUtility.DisplayDialog("Image Export Result", "Exported: " + cnt + "/" + m_info.Length, "OK");
        }

        /// <summary>
        /// Copy the path of selected image inside builtin asset bundle to clipboard
        /// </summary>
        /// <param name="path"></param>
        private void DoCopyPathToClipboard(string path) {
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log(path + " copied to clipboard.");
        }

        /// <summary>
        /// Excute export image
        /// </summary>
        /// <param name="imgInfo"></param>
        private void DoExportImage(ImageInfo imgInfo) {
            string path = Path.Combine(Application.dataPath, "JVLBuiltInAsset/Image");
            if (!Directory.Exists(path)) {
                AssetDatabase.Refresh();
                Directory.CreateDirectory(path);
            }

            string s = imgInfo.BundlePath;
            string fileName = Path.GetFileName(s);
            if (fileName != null) {
                string savePath = Path.Combine(path, fileName);
                Debug.Log(savePath);
                Texture2D texture = imgInfo.Texture;
                if (texture != null) {
                    m_disablePreviewArea = true;
                    byte[] temp = texture.GetRawTextureData();
                    Texture2D saveFile = new Texture2D(texture.width, texture.height, texture.format, false);
                    saveFile.LoadRawTextureData(temp);
                    saveFile.Apply();
                    if (savePath.EndsWith(".asset")) {
                        savePath = savePath.Replace(".asset", ".png");
                    }

                    temp = saveFile.EncodeToPNG();
                    File.WriteAllBytes(savePath, temp);
                    m_disablePreviewArea = false;
                }
            }
        }
    }
}