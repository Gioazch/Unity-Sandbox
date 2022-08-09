using UnityEngine;
using UnityEditor;
using System.IO;
using static Onager.FXMesh.FXMeshSettings;

namespace Onager.FXMesh
{
    using Onager.Utilities;

    [CustomEditor(typeof(FXMeshSettings)), CanEditMultipleObjects]
    public class FXMeshSettingsEditor : Editor
    {
        private static string[] tabs = { "Shape", "Color", "UVs", "About" };

        private static GUIContent generalFoldoutContent = new GUIContent(" Shape");
        private static GUIContent modifiersFoldoutContent = new GUIContent(" Modifiers");
        private static GUIContent heightFoldoutContent = new GUIContent(" Height");
        private static GUIContent twistFoldoutContent = new GUIContent(" Twist");
        private static GUIContent colorFoldoutContent = new GUIContent(" Colors");
        private static GUIContent uvFoldoutContent = new GUIContent(" UV Sets");
        private static GUIContent titleContent = new GUIContent(" FX Mesh Settings");
        private static GUIContent previewContent = new GUIContent(" Preview");
        private static GUIContent exportContent = new GUIContent(" Export");
        private static GUIContent exportHelpContent =
            new GUIContent(
                "We recommend using the FXMesh Component. Exporting the mesh prevents live/realtime editing.");
        private static GUIContent exportCreateContent =
            new GUIContent(
                "Create a new GameObject with the FXMesh component.");

        private static GUIContent exportFBXWarningUVContent =
            new GUIContent(
                "FBX format only allows (X) and (Y) components in UV channels.");

        private static GUIContent exportAlreadyExistsContent = new GUIContent("Asset at path already exists");

        private static string sessionKey = "FXMesh";
        private string HashName => "FXMesh_Preview_" + target.GetHashCode();
        private string PreferredFolder => sessionKey + "_PreferredFolder";

        private static OnagerMeshPreview preview;

        public void OnEnable()
        {
            generalFoldoutContent.image = Layout.GetEditorIcon("PreMatCylinder");
            heightFoldoutContent.image = Layout.GetEditorIcon("ScaleTool");
            twistFoldoutContent.image = Layout.GetEditorIcon("RotateTool");
            colorFoldoutContent.image = Layout.GetEditorIcon("d_Grid.PaintTool");
            uvFoldoutContent.image = Layout.GetEditorIcon("d_GridLayoutGroup Icon");
            titleContent.image = Layout.GetEditorIcon("PreMatCube");
            previewContent.image = Layout.GetEditorIcon("ViewToolOrbit");
            exportContent.image = Layout.GetEditorIcon("d_Project");
            exportAlreadyExistsContent.image = Layout.GetEditorIcon("console.warnicon.sml");
            modifiersFoldoutContent.image = Layout.GetEditorIcon("PreMatSphere");

            preview = null;
        }

        public override void OnInspectorGUI()
        {
            FXMeshSettings settings = target as FXMeshSettings;
            FXMesh mesh = null;

            if (preview == null)
                preview = new OnagerMeshPreview(serializedObject, settings, HashName);

            serializedObject.Update();

            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
                mesh = selectedObject.GetComponent<FXMesh>();

            Layout.DrawHeader(titleContent, Colors.OnagerColor);

            using (var s = new Layout.FoldoutScope(generalFoldoutContent, FindProperty(nameof(settings.loops))))
            {
                if (s.visible)
                {
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.loops)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.rings)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.ringProfile)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.startRadius)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.endRadius)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.maxAngle)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.computeNormals)));
                    EditorGUILayout.Space(20);
                }
            }

            using (var s = new Layout.FoldoutScope(modifiersFoldoutContent, FindProperty(nameof(settings.height))))
            {
                if (s.visible)
                {
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.height)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.heightProfile)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.twist)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.twistProfile)));
                    EditorGUILayout.Space(20);
                }
            }

            using (var s = new Layout.FoldoutScope(colorFoldoutContent, FindProperty(nameof(settings.Color))))
            {
                if (s.visible)
                {
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.Color)));
                    EditorGUILayout.Space(20);
                }
            }

            using (var s = new Layout.FoldoutScope(uvFoldoutContent, FindProperty(nameof(settings.UV0))))
            {
                if (s.visible)
                {
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.UV0)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.UV1)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.UV2)));
                    EditorGUILayout.PropertyField(FindProperty(nameof(settings.UV3)));
                    EditorGUILayout.Space(20);
                }
            }

            using (var s = new Layout.FoldoutScope(exportContent, FindProperty("exportToggle")))
            {
                if (s.visible)
                {
                    Layout.Space();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Create a new FXMesh GameObject");
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Create", GUILayout.Width(80)))
                        {
                            var gameObject = new GameObject("New FXMesh");
                            var mr = gameObject.AddComponent<MeshRenderer>();
                            gameObject.AddComponent<MeshFilter>();
                            mr.sharedMaterial = new Material(Shader.Find("Hidden/Onager/DataDisplay"));

                            var FXMesh = gameObject.AddComponent<FXMesh>();
                            FXMesh.SetSettings(settings);
                        }
                    }

                    Layout.Space();
                    Layout.DrawSeparator(true);
                    Layout.HelpBox(exportHelpContent.text);
                    Layout.Space();

                    var exportName = FindProperty("exportName");
                    exportName.stringValue = EditorGUILayout.TextField("Filename", exportName.stringValue);

                    var absolutePath = SessionState.GetString(PreferredFolder, "") + "/" + exportName.stringValue + ".asset";
                    var path = FileUtil.GetProjectRelativePath(absolutePath);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.TextField("Folder", path);

                        if (GUILayout.Button("...", GUILayout.Width(40)))
                        {
                            SessionState.SetString(PreferredFolder, EditorUtility.OpenFolderPanel("Set Export Folder", "", ""));
                        }
                    }

                    Layout.Space();

                    bool fileExists = File.Exists(absolutePath);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (fileExists)
                        {
                            EditorGUILayout.LabelField(exportAlreadyExistsContent, EditorStyles.boldLabel);
                        }

                        GUILayout.FlexibleSpace();

                        GUI.enabled = !fileExists;
                        if (GUILayout.Button("Export", GUILayout.Width(80)))
                        {
                            // New mesh instance
                            var exportedMesh = Instantiate(settings.GetMesh(true));
                            exportedMesh.Optimize();

                            AssetDatabase.CreateAsset(exportedMesh, path);
                            AssetDatabase.SaveAssets();
                            EditorGUIUtility.PingObject(exportedMesh);
                        }

                        GUI.enabled = true;
                        if (GUILayout.Button("Update", GUILayout.Width(80)))
                        {
                            var exportedMesh = settings.GetMesh();
                            exportedMesh.Optimize();

                            Mesh existingAsset = AssetDatabase.LoadMainAssetAtPath(path) as Mesh;
                            if (existingAsset != null)
                            {
                                existingAsset.Clear();
                                EditorUtility.CopySerialized(exportedMesh, existingAsset);
                                AssetDatabase.SaveAssets();
                                EditorGUIUtility.PingObject(existingAsset);
                            }
                            else
                            {
                                AssetDatabase.CreateAsset(exportedMesh, path);
                                AssetDatabase.SaveAssets();
                                EditorGUIUtility.PingObject(exportedMesh);
                            }
                        }
                    }

                    Layout.Space();
                }
            }

            using (new Layout.FoldoutScope(previewContent, null, Colors.OnagerColor.Brightness(.6f)))
            {
                string meshLabel = "Preview mesh";
                GUIStyle style = EditorStyles.label;
                if (mesh != null && mesh.Settings == settings)
                {
                    meshLabel = mesh.gameObject.name;
                    GUI.color = Color.green;
                    style = EditorStyles.boldLabel;
                }

                var r = GUILayoutUtility.GetLastRect();
                r.y -= 20;
                r.x = r.width - style.CalcSize(new GUIContent(meshLabel)).x + 10;
                r.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(r, meshLabel, style);
                GUI.color = Color.white;

                preview.DrawPreviewMesh(serializedObject, serializedObject.hasModifiedProperties);
            }

            Layout.Space();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private SerializedProperty FindProperty(string name)
        {
            return serializedObject.FindProperty(name);
        }
    }

    [CustomPropertyDrawer(typeof(ChannelInfo))]
    public class ChannelDrawer : PropertyDrawer
    {
        private static int padding = 4;
        private static float height => EditorGUIUtility.singleLineHeight;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            position.height = height;

            var labelRect = new Rect(position.x, position.y, position.width, height);
            var modeRect = new Rect(position.x + 170, position.y, position.width - 170, height);
            var buttonRect = new Rect(modeRect.xMax + 5, position.y, (position.xMax - modeRect.xMax - 5), height);

            var background = new Rect(labelRect.x, labelRect.y + labelRect.height + 6, labelRect.width, 1);
            EditorGUI.DrawRect(background, Color.white.Alpha(.05f));

            var mode = property.FindPropertyRelative("Mode");
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
            EditorGUI.PropertyField(modeRect, mode, GUIContent.none);

            var selectedObject = Selection.activeGameObject;

            labelRect.y = labelRect.yMax + 4;
            labelRect.height = 1;

            position.y += 10;

            // Gradient
            if (mode.intValue == 1)
            {
                EditorGUI.indentLevel++;

                CombinedLine("", property, "Gradient", "Gradient_Horizontal", position);
            }
            // Curves
            else if (mode.intValue == 2)
            {
                EditorGUI.indentLevel++;

                var curveRect = new Rect(position);
                CombinedLine("X", property, "R", "R_Horizontal", curveRect);
                curveRect.y += height + padding;
                CombinedLine("Y", property, "G", "G_Horizontal", curveRect);
                curveRect.y += height + padding;
                CombinedLine("Z", property, "B", "B_Horizontal", curveRect);
                curveRect.y += height + padding;
                CombinedLine("W", property, "A", "A_Horizontal", curveRect);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static void CombinedLine(string label, SerializedProperty property, string data, string horizontal,
            Rect sourceRect)
        {
            var lineRect = new Rect(sourceRect);
            lineRect.y += height + padding;

            EditorGUI.LabelField(lineRect, label);

            lineRect.x += 16;
            lineRect.width -= 44;

            EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(data), GUIContent.none);

            lineRect.x = lineRect.xMax + 5;
            lineRect.width = 25;

            var prop = property.FindPropertyRelative(horizontal);
            prop.boolValue = GUI.Toggle(lineRect, prop.boolValue, !prop.boolValue ? "↑" : "→", "Button");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int mode = property.FindPropertyRelative("Mode").intValue;
            float height = EditorGUIUtility.singleLineHeight;

            if (mode == 1)
            {
                height += (EditorGUIUtility.singleLineHeight) + padding + (padding / 2);
            }
            else if (mode == 2)
            {
                height += (EditorGUIUtility.singleLineHeight * 4) + (padding * 5) + (padding / 2);
            }

            if (mode != 0) height += 5;

            return height + 8;
        }
    }

    public enum DataNames
    {
        Color = 0,
        Normal = 1,
        Tangent = 2,
        UV0 = 3,
        UV1 = 4,
        UV2 = 5,
        UV3 = 6,
        UV4 = 7,
        UV5 = 8,
        UV6 = 9,
        UV7 = 10,
        // ID = 11,
        // WorldPos = 12,
        LocalPos = 13,
    }

    public enum ChannelNames
    {
        RGB = 0,
        R = 1,
        G = 2,
        B = 3,
        A = 4,
    }
}