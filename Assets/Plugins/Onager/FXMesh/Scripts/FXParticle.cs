using System.Collections.Generic;
using UnityEngine;

namespace Onager.FXMesh
{
    [RequireComponent(typeof(ParticleSystem)), ExecuteInEditMode]
    public class FXParticle : MonoBehaviour
    {
        public FXMeshSettings Settings => settings;
        [SerializeField] private FXMeshSettings settings;

        private ParticleSystemRenderer psr => _psr ? _psr : _psr = GetComponent<ParticleSystemRenderer>(); private ParticleSystemRenderer _psr;

        private void Awake()
        {
            ApplyMesh();
        }

        public void ApplyMesh()
        {
            if (settings == null) return;
            psr.mesh = Settings.GetMesh();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (settings == null) return;
            psr.mesh = Settings.GetMesh();
        }
#endif
    }
}