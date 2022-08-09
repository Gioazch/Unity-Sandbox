using System;
using System.Collections.Generic;
using UnityEngine;

namespace Onager.FXMesh
{
    [CreateAssetMenu(menuName = "Onager/FXMesh/New Preset")]
    public class FXMeshSettings : ScriptableObject
    {
        #region Structures
        public enum ChannelMode { None, Gradient, Curves }

        [System.Serializable]
        public struct ChannelInfo
        {
            public ChannelMode Mode;
            public bool Enabled => Mode != ChannelMode.None;

            public Gradient Gradient;
            public bool Gradient_Horizontal;

            public AnimationCurve R;
            public bool R_Horizontal;
            public AnimationCurve G;
            public bool G_Horizontal;
            public AnimationCurve B;
            public bool B_Horizontal;
            public AnimationCurve A;
            public bool A_Horizontal;
        }

        private struct PolarCoords
        {
            public int ring, loop;

            public PolarCoords(int ring, int theta)
            {
                this.ring = ring;
                this.loop = theta;
            }

            public override string ToString()
            {
                return $"{ring};{loop}";
            }
        }
        #endregion

        #region Settings
        [Header("Radius")]
        [Min(0)] public float startRadius = 0;
        [Min(0.001f)] public float endRadius = 10;
        [Range(0.001f, 360)] public float maxAngle = 360;

        [Header("Topology")]
        [Range(3, 128)] public int loops = 8;
        [Range(2, 128)] public int rings = 3;
        public AnimationCurve ringProfile = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Height")]
        public float height = 0;
        public AnimationCurve heightProfile = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Normals")]
        public bool computeNormals;

        [Header("Twist")]
        public float twist = 0;
        public AnimationCurve twistProfile = AnimationCurve.Linear(0, 0, 0, 0);

        public ChannelInfo Color;
        public ChannelInfo UV0;
        public ChannelInfo UV1;
        public ChannelInfo UV2;
        public ChannelInfo UV3;
        #endregion

        #region EditorOnly
#if UNITY_EDITOR
        public bool toggleR, toggleG, toggleB, toggleA;
        public bool toggleRGB = true;
        public bool exportToggle;
        public string exportName;
#endif
        #endregion

        public static Dictionary<FXMeshSettings, Mesh> GeneratedMeshes = new Dictionary<FXMeshSettings, Mesh>();
        private static Dictionary<PolarCoords, int> vertexLUT = new Dictionary<PolarCoords, int>();
        private static List<Vector3> vertices = new List<Vector3>();
        private static List<Color> colors = new List<Color>();
        private static List<Vector4> uv0 = new List<Vector4>();
        private static List<Vector4> uv1 = new List<Vector4>();
        private static List<Vector4> uv2 = new List<Vector4>();
        private static List<Vector4> uv3 = new List<Vector4>();
        private static List<int> triangles = new List<int>();
        private bool dirty = false;

        public Mesh GetMesh(bool forceRebuild = false)
        {
            Mesh mesh;

            if (GeneratedMeshes.ContainsKey(this))
            {
                mesh = GeneratedMeshes[this];

                if ((forceRebuild || dirty)) { }
                else if (mesh != null) return mesh;
            }
            else
            {
                mesh = new Mesh();
                GeneratedMeshes.Add(this, mesh);
            }

            if (mesh == null) mesh = new Mesh();

            mesh.name = name;
            mesh.Clear();
            vertexLUT.Clear();
            vertices.Clear();
            triangles.Clear();
            colors.Clear();
            uv0.Clear();
            uv1.Clear();
            uv2.Clear();
            uv3.Clear();

            for (int r = 0; r < rings; r++)
            {
                for (int c = 0; c < loops; c++)
                {
                    AddQuad(r, c);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            if (Color.Enabled) mesh.SetColors(colors);
            if (UV0.Enabled) mesh.SetUVs(0, uv0);
            if (UV1.Enabled) mesh.SetUVs(1, uv1);
            if (UV2.Enabled) mesh.SetUVs(2, uv2);
            if (UV3.Enabled) mesh.SetUVs(3, uv3);

            if (computeNormals) mesh.RecalculateNormals();

            mesh.UploadMeshData(false);
            dirty = false;
            return mesh;
        }

        private void InsertUniqueVertex(PolarCoords coords)
        {
            if (vertexLUT.ContainsKey(coords)) return;

            float theta = GetAngle(coords) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta));

            Vector3 pos = (dir * startRadius) + (dir * GetLength(coords.ring));
            Vector3 height = (Vector3.up * GetHeight((pos.magnitude - startRadius) / endRadius));

            vertices.Add(pos + height);
            vertexLUT.Add(coords, vertices.Count - 1);

            GetColor(Color, coords, colors);
            GetUV(UV0, coords, uv0);
            GetUV(UV1, coords, uv1);
            GetUV(UV2, coords, uv2);
            GetUV(UV3, coords, uv3);
        }

        private float GetLength(float ring)
        {
            float progress = ring / rings;
            float radius = endRadius;
            return (ringProfile.Evaluate(progress)) * (radius / 2);
        }

        private float GetHeight(float progress)
        {
            return (heightProfile.Evaluate(progress)) * (height);
        }

        private float GetAngle(PolarCoords coords)
        {
            float progress = coords.ring / (float)rings;
            float twistAngle = (twistProfile.Evaluate(progress)) * twist;

            float Angle = maxAngle / loops;
            return (((Angle * coords.loop)) % (maxAngle + Angle)) + twistAngle;
        }

        private void GetUV(ChannelInfo info, PolarCoords coords, List<Vector4> target)
        {
            if (info.Mode == ChannelMode.None) return;

            float vertical = coords.ring / (float)rings;
            float horizontal = coords.loop / (float)loops;

            if (info.Mode == ChannelMode.Gradient)
            {
                float progress = info.Gradient_Horizontal ? horizontal : vertical;
                target.Add(info.Gradient.Evaluate(progress));
            }

            if (info.Mode == ChannelMode.Curves)
            {
                Vector4 value = new Vector4();
                value.x = info.R.Evaluate(info.R_Horizontal ? horizontal : vertical);
                value.y = info.G.Evaluate(info.G_Horizontal ? horizontal : vertical);
                value.z = info.B.Evaluate(info.B_Horizontal ? horizontal : vertical);
                value.w = info.A.Evaluate(info.A_Horizontal ? horizontal : vertical);
                target.Add(value);
            }
        }

        private void GetColor(ChannelInfo info, PolarCoords coords, List<Color> target)
        {
            if (info.Mode == ChannelMode.None) return;

            float vertical = coords.ring / (float)rings;
            float horizontal = coords.loop / (float)loops;

            if (info.Mode == ChannelMode.Gradient)
            {
                float progress = info.Gradient_Horizontal ? horizontal : vertical;
                target.Add(info.Gradient.Evaluate(progress));
            }

            if (info.Mode == ChannelMode.Curves)
            {
                Color value = new Color();
                value.r = info.R.Evaluate(info.R_Horizontal ? horizontal : vertical);
                value.g = info.G.Evaluate(info.G_Horizontal ? horizontal : vertical);
                value.b = info.B.Evaluate(info.B_Horizontal ? horizontal : vertical);
                value.a = info.A.Evaluate(info.A_Horizontal ? horizontal : vertical);
                target.Add(value);
            }
        }

        private int GetNextLoop(int currentLoop) => (currentLoop + 1) /* % loops */;

        private void AddQuad(int ring, int loop)
        {
            int nextLoop = GetNextLoop(loop);
            int nextRing = ring + 1;

            PolarCoords p0 = new PolarCoords(nextRing, loop);
            PolarCoords p1 = new PolarCoords(ring, loop);
            PolarCoords p2 = new PolarCoords(ring, nextLoop);
            PolarCoords p3 = new PolarCoords(nextRing, nextLoop);

            InsertUniqueVertex(p0);
            InsertUniqueVertex(p1);
            InsertUniqueVertex(p2);
            InsertUniqueVertex(p3);

            // t1
            triangles.Add(vertexLUT[p0]);
            triangles.Add(vertexLUT[p1]);
            triangles.Add(vertexLUT[p2]);

            //t2
            triangles.Add(vertexLUT[p0]);
            triangles.Add(vertexLUT[p2]);
            triangles.Add(vertexLUT[p3]);
        }

        private void OnValidate()
        {
            dirty = true;
        }
    }
}