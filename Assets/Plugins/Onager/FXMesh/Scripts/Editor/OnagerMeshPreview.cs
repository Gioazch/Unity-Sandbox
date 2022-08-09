using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Onager.FXMesh
{
    using Onager.Utilities;

    public class OnagerMeshPreview
    {
        private Editor window;
        private GUIStyle style;

        private Mesh mesh;
        private Material material;
        private GameObject gameObject;

        private float wireframe;
        private GUIContent wireframeContent = new GUIContent(" Wireframe");

        private DataNames selectedChannel;
        private SerializedProperty toggleR;
        private SerializedProperty toggleG;
        private SerializedProperty toggleB;
        private SerializedProperty toggleA;
        private SerializedProperty toggleRGB;
        private SerializedProperty lastToggled;

        private FXMeshSettings settings;

        public OnagerMeshPreview(SerializedObject serializedObject, FXMeshSettings settings, string hashName)
        {
            wireframeContent.image = EditorGUIUtility.IconContent("d_Mesh Icon").image;
            material = Resources.Load("FXMesh_DataDisplay") as Material;
            wireframe = material.GetFloat("WireframeOpacity");
            style = new GUIStyle();
            gameObject = GameObject.Find(hashName);
            this.settings = settings;
            mesh = settings.GetMesh(true);

            if (gameObject == null)
            {
                gameObject = new GameObject(hashName);
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            }
            else
            {
                gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            gameObject.transform.position = Vector3.one * 10000f;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            InitToggles(serializedObject);

            // selectedChannel = DataNames.Color;
            UpdatePreviewMaterialChannel();
            UpdatePreviewMaterialMask();
        }

        private void InitToggles(SerializedObject serializedObject)
        {
            toggleR = serializedObject.FindProperty("toggleR");
            toggleG = serializedObject.FindProperty("toggleG");
            toggleB = serializedObject.FindProperty("toggleB");
            toggleA = serializedObject.FindProperty("toggleA");
            toggleRGB = serializedObject.FindProperty("toggleRGB");

            if (toggleR.boolValue) lastToggled = toggleR;
            if (toggleG.boolValue) lastToggled = toggleG;
            if (toggleB.boolValue) lastToggled = toggleB;
            if (toggleA.boolValue) lastToggled = toggleA;
            if (toggleRGB.boolValue) lastToggled = toggleRGB;
        }

        public void DrawPreviewMesh(SerializedObject serializedObject, bool rebuild)
        {
            if (rebuild)
                mesh = settings.GetMesh(true);

            InitToggles(serializedObject);

            if (mesh != null && gameObject != null)
            {
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                if (window == null || gameObject != window.target)
                    window = Editor.CreateEditor(gameObject);

                EditorGUI.indentLevel++;

                var space = Layout.ReserveSingleLine(8);
                var separator = new Rect(3, space.y + space.height + 3, Screen.width, 1);
                var channelBg = new Rect(3, space.y - 8, Screen.width, space.height + 8);
                var channelPopup = new Rect(55, channelBg.y + 10, 80, EditorGUIUtility.singleLineHeight);
                var channelButton = new Rect(space.width, channelBg.y + 10, 22, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(channelBg.x, channelBg.y + 45, 200, EditorGUIUtility.singleLineHeight);

                // Draw Channel Popup
                {
                    EditorGUI.DrawRect(channelBg, new Color32(49, 49, 49, 255));

                    DrawPreviewChannelMaskButton(channelButton, "A", toggleA);

                    channelButton.x -= 23;
                    DrawPreviewChannelMaskButton(channelButton, "B", toggleB);

                    channelButton.x -= 23;
                    DrawPreviewChannelMaskButton(channelButton, "G", toggleG);

                    channelButton.x -= 23;
                    DrawPreviewChannelMaskButton(channelButton, "R", toggleR);

                    channelButton.x -= 48;
                    channelButton.width += 20;
                    DrawPreviewChannelMaskButton(channelButton, "RGB", toggleRGB);

                    EditorGUI.BeginChangeCheck();
                    selectedChannel = (DataNames)EditorGUI.EnumPopup(channelPopup, selectedChannel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdatePreviewMaterialChannel();
                    }

                    channelPopup.x -= 55;
                    EditorGUI.LabelField(channelPopup, "Channel");
                }

                // Draw Preview
                {
                    var r = GUILayoutUtility.GetRect(0, 200);
                    r = new Rect(3, r.y - 5, Screen.width, r.height + 7);
                    window.OnInteractivePreviewGUI(r, style);
                }

                {
                    string channels = (toggleR.boolValue ? "r" : "") +
                                      (toggleG.boolValue ? "g" : "") +
                                      (toggleB.boolValue ? "b" : "") +
                                      (toggleA.boolValue ? "a" : "");

                    if (toggleRGB.boolValue) channels = "rgb";

                    if (channels != "")
                        EditorGUI.LabelField(labelRect, selectedChannel.ToString().ToLower() + "." + channels);
                }

                // Draw Wireframe Slider
                {
                    EditorGUI.DrawRect(separator, Color.white.Alpha(.05f));

                    // Draw slider background
                    var background = Layout.ReserveSingleLine(8);
                    var sliderRect = new Rect(background);
                    sliderRect.height -= 8;
                    background.x = 3;
                    background.width = Screen.width;
                    EditorGUI.DrawRect(background, new Color32(49, 49, 49, 255));

                    separator = new Rect(3, sliderRect.y - 8, Screen.width, 1);
                    EditorGUI.DrawRect(separator, Color.white.Alpha(.05f));

                    // Draw wireframe slider
                    EditorGUI.indentLevel--;
                    EditorGUI.BeginChangeCheck();
                    wireframe = EditorGUI.Slider(sliderRect, "Wireframe", wireframe, 0, 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        material.SetFloat("WireframeOpacity", wireframe);
                        window.ReloadPreviewInstances();
                    }

                    EditorGUI.indentLevel++;
                }

                EditorGUI.indentLevel--;

                if (serializedObject.hasModifiedProperties && window != null)
                {
                    window.ReloadPreviewInstances();
                    window.Repaint();
                }
            }
        }

        private void DrawPreviewChannelMaskButton(Rect r, string label, SerializedProperty prop)
        {
            EditorGUI.BeginChangeCheck();
            prop.boolValue = GUI.Toggle(r, prop.boolValue, label, "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (prop.boolValue)
                {
                    if (lastToggled != null && lastToggled != prop)
                    {
                        lastToggled.boolValue = false;
                    }

                    lastToggled = prop;
                }

                UpdatePreviewMaterialMask();
            }
        }

        private void UpdatePreviewMaterialChannel()
        {
            int data = (int)selectedChannel;
            gameObject.GetComponent<Renderer>().sharedMaterial.SetFloat("DisplayedData", data);
            window?.ReloadPreviewInstances();
        }

        private void UpdatePreviewMaterialMask()
        {
            if (toggleRGB.boolValue)
                material.EnableKeyword("RGB");
            else
                material.DisableKeyword("RGB");

            Vector4 mask = new Vector4(
                toggleR.boolValue ? 1f : 0f,
                toggleG.boolValue ? 1f : 0f,
                toggleB.boolValue ? 1f : 0f,
                toggleA.boolValue ? 1f : 0f);

            material.SetVector("ChannelMask", mask);
            window?.ReloadPreviewInstances();
        }
    }
}
