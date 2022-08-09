using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Onager.Utilities
{
    public class Layout
    {
        public class FoldoutScope : GUI.Scope
        {
            public bool visible;
            private Color color;

            private void DrawFoldout(GUIContent content, SerializedProperty state)
            {
                bool stateless = state == null;

                EditorGUILayout.Space(1);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (state != null)
                            state.isExpanded = EditorGUILayout.Foldout(state.isExpanded, content, true, FoldoutStyle);
                        else
                        {
                            EditorGUILayout.Foldout(true, content, true, FoldoutStyle);
                        }
                    }

                    EditorGUILayout.Space(2);
                }

                var r = GetLastRectWide();
                EditorGUI.DrawRect(r, Color.white.Alpha(.05f));

                if (!stateless)
                    DrawLastRectOverlay();

                var borderColorRect = new Rect(r);
                borderColorRect.width = 3;
                var borderColor = color;
                if (!stateless && !state.isExpanded) borderColor = Color.grey;
                EditorGUI.DrawRect(borderColorRect, borderColor);

                visible = true;
                if (!stateless) visible = state.isExpanded;

                if (visible)
                {
                    EditorGUILayout.BeginVertical();
                    // EditorGUI.indentLevel++;
                    EditorGUILayout.Space();
                }
            }

            public FoldoutScope(GUIContent content, SerializedProperty state)
            {
                color = Colors.OnagerColor;
                DrawFoldout(content, state);
            }

            public FoldoutScope(GUIContent content, SerializedProperty state, Color color)
            {
                this.color = color;
                DrawFoldout(content, state);
            }

            protected override void CloseScope()
            {
                if (visible)
                {
                    // EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                    var borderColorRect = GetLastRectWide();
                    borderColorRect.width = 3;
                    EditorGUI.DrawRect(borderColorRect, color.Alpha(.15f));
                }
            }
        }

        // Styles

        public static GUIStyle FoldoutStyle => new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = Color.white },
            padding = new RectOffset(5, 0, 0, 0),
            stretchWidth = true
        };

        public static GUIStyle ToolbarStyle => new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        public static GUIStyle OnagerTitleStyle => new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = Color.white },
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 0, 0, 0)
        };

        public static GUIStyle OnagerSubtitleStyle => new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = Color.white },
            fontSize = 11,
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(0, 5, 0, 0),
            fontStyle = FontStyle.Italic
        };

        public static GUIStyle BigPadding => new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(3, 3, 3, 3)
        };

        public static GUIStyle HelpBoxContent => new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };

        // Methods

        public static void DrawTintedTexture(Texture texture, Color color, GUIStyle style,
            params GUILayoutOption[] options)
        {
            GUI.color = color;
            GUILayout.Box(texture, style, options);
            GUI.color = Color.white;
        }

        public static void DrawSeparator(bool addLine = false)
        {
            EditorGUILayout.Separator();

            if (addLine)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += rect.height / 2;
                rect.height = 1;
                EditorGUI.DrawRect(rect, new Color(1, 1, 1, .05f));
            }

            EditorGUILayout.Separator();
        }

        public static void DrawHeader(GUIContent content, Color color)
        {
            EditorGUI.indentLevel--;
            using (new EditorGUILayout.VerticalScope())
            {
                Space(3);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = color;
                    EditorGUILayout.LabelField(content, OnagerTitleStyle);

                    GUILayout.FlexibleSpace();

                    GUI.color = Color.grey;
                    EditorGUILayout.LabelField("by Onager", OnagerSubtitleStyle, GUILayout.Width(40));

                    GUI.color = Color.white;
                }


                Space(5);
            }

            EditorGUI.indentLevel++;

            var border = GetLastRectWide();
            border.width = 3;
            border.y = 1;
            border.height += 3;
            EditorGUI.DrawRect(border, color);

            var r = GetLastRectWide();
            r.y = 1;
            r.height += 3;
            // EditorGUI.DrawRect(r, Color.white.Alpha(.05f));
        }

        public static int DrawToolbar(int index, params string[] tabs)
        {
            var rect = EditorGUILayout.GetControlRect(false, 25, ToolbarStyle);
            rect.x = 0;
            rect.width = Screen.width;

            var borders = new Rect(rect);
            borders.height = 1;
            borders.y--;
            EditorGUI.DrawRect(borders, Color.black.Alpha(.25f));
            borders.y += rect.height + 1;
            EditorGUI.DrawRect(borders, Color.black.Alpha(.25f));

            EditorGUI.DrawRect(rect, Color.white.Alpha(.05f));

            int count = tabs.Length;
            int buttonSize = Screen.width / count;

            var buttonRect = new Rect(rect);
            buttonRect.width = buttonSize;

            for (int i = 0; i < count; i++)
            {
                if (index == i)
                {
                    EditorGUI.DrawRect(buttonRect, Color.black.Alpha(.25f));
                }

                if (GUI.Button(buttonRect, tabs[i], ToolbarStyle))
                {
                    index = i;
                }

                var borderRect = new Rect(buttonRect);
                borderRect.x += borderRect.width;
                borderRect.width = 1;
                // EditorGUI.DrawRect(borderRect, Color.black.Alpha(.2f));

                if (index != i && buttonRect.ContainsMouse())
                {
                    EditorGUI.DrawRect(buttonRect, Color.white.Alpha(.1f));
                }

                buttonRect.x += buttonSize;
            }

            return index;
        }

        public static Rect DrawLastRectOverlay()
        {
            var lastRect = GetLastRectWide();
            if (lastRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(lastRect, Color.white.Alpha(.05f));
            }

            return lastRect;
        }

        public static Rect GetLastRect()
        {
            return GUILayoutUtility.GetLastRect();
        }

        public static Rect GetLastRectWide()
        {
            var r = GUILayoutUtility.GetLastRect();
            r.x = 0;
            r.width = Screen.width;

            return r;
        }

        public static Rect ReserveSingleLine(float height = 0)
        {
            return EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + height);
        }

        public static void Space(float amount = 10)
        {
            EditorGUILayout.Space(amount);
        }

        public static void DrawBlankSpace(float space = 20)
        {
            Space(space);
            var r = GetLastRectWide();
            EditorGUI.DrawRect(r, Color.black.Alpha(.1f));
        }

        public static Texture GetEditorIcon(string name)
        {
            return EditorGUIUtility.IconContent(name).image;
        }

        public static void HelpBox(string content)
        {
            EditorGUILayout.BeginHorizontal(BigPadding);
            EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.infoicon.inactive.sml@2x"), GUILayout.Width(40), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(content, HelpBoxContent, GUILayout.MinHeight(40));
            EditorGUILayout.EndHorizontal();
        }
    }

    public static class LayoutExtensions
    {
        public static bool ContainsMouse(this Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        public static GUIContent EditText(this GUIContent content, string newText)
        {
            content.text = newText;
            return content;
        }
    }
}