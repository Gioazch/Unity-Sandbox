using System.Collections.Generic;
using UnityEngine;

namespace Onager.FXMesh
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode]
    public class FXMesh : MonoBehaviour
    {
        public FXMeshSettings Settings => settings;
        [SerializeField] private FXMeshSettings settings;

        private MeshFilter MFilter => _meshFilter ? _meshFilter : _meshFilter = GetComponent<MeshFilter>(); private MeshFilter _meshFilter;
        private MeshRenderer MRenderer => _meshRenderer ? _meshRenderer : _meshRenderer = GetComponent<MeshRenderer>(); private MeshRenderer _meshRenderer;

        private void Awake()
        {
            ApplyMesh();
        }

        public void ApplyMesh()
        {
            if (settings == null) return;
            MFilter.sharedMesh = settings.GetMesh();
        }

#if UNITY_EDITOR
        public void SetSettings(FXMeshSettings settings)
        {
            this.settings = settings;
            ApplyMesh();
        }

        private void Update()
        {
            if (settings == null) return;
            MFilter.sharedMesh = settings.GetMesh();
        }
#endif
    }
}