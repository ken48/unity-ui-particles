using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityUiParticles.Internal;

namespace UnityUiParticles
{
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class ParticleSystemMeshGenerator : MaskableGraphic
    {
        static readonly Matrix4x4 ScaleZ = Matrix4x4.Scale(new Vector3(x: 1f, y: 1f, z: 0.00001f));

        [SerializeField] Material _material;

        [SerializeField] Material _trailsMaterial;

        ParticleSystem _particleSystem;
        ParticleSystemRenderer _particleSystemRenderer;
        ParticleSystem.MainModule _mainModule;
        ParticleSystem.TrailModule _trailsModule;
        MeshHelper _meshHelper;
        Material[] _maskMaterials;
        Canvas _parentCanvas;
        bool _isRegistered;

        protected override void Awake()
        {
            base.Awake();

            _particleSystem = GetComponent<ParticleSystem>();
            _particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
            _mainModule = _particleSystem.main;
            _trailsModule = _particleSystem.trails;
        }

        protected override void OnDidApplyAnimationProperties() { }

        protected override void OnDisable()
        {
            base.OnDisable();

            Canvas.willRenderCanvases -= Refresh;

            _meshHelper.Destroy();
            _meshHelper = null;

            foreach (Material maskMaterial in _maskMaterials)
            {
                StencilMaterial.Remove(maskMaterial);
            }

            _maskMaterials = null;

            if (_isRegistered)
            {
                BakingCamera.UnregisterConsumer();
                _isRegistered = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _meshHelper = MeshHelper.Create();
            _maskMaterials = new Material[2];

            _parentCanvas = null;
            Canvas.willRenderCanvases += Refresh;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            raycastTarget = false;

            GetComponent<ParticleSystemRenderer>()
               .enabled = false;
        }
#endif

        //
        // Essential overrides
        //

        protected override void UpdateGeometry() { }

        protected override void UpdateMaterial()
        {
            canvasRenderer.materialCount = _trailsModule.enabled
                ? 2
                : 1;

            canvasRenderer.SetMaterial(GetModifiedMaterial(_material, index: 0), index: 0);

            if (_trailsModule.enabled)
            {
                canvasRenderer.SetMaterial(GetModifiedMaterial(_trailsMaterial, index: 1), index: 1);
            }
        }

        // Overloaded version for multiple materials
        protected virtual Material GetModifiedMaterial(Material baseMaterial, int index)
        {
            Material baseMat = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                m_ShouldRecalculateStencil = false;

                if (maskable)
                {
                    Transform sortOverrideCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                    m_StencilValue = MaskUtilities.GetStencilDepth(transform, sortOverrideCanvas) + index;
                }
                else
                {
                    m_StencilValue = 0;
                }
            }

            var component = GetComponent<Mask>();

            if (m_StencilValue > 0
             && (component == null || !component.IsActive()))
            {
                int stencilId = (1 << m_StencilValue) - 1;

                Material maskMaterial = StencilMaterial.Add(
                    baseMat,
                    stencilId,
                    StencilOp.Keep,
                    CompareFunction.Equal,
                    ColorWriteMask.All,
                    stencilId,
                    writeMask: 0
                );

                StencilMaterial.Remove(_maskMaterials[index]);
                _maskMaterials[index] = maskMaterial;
                baseMat = _maskMaterials[index];
            }

            return baseMat;
        }

        protected Canvas GetParentCanvas()
        {
            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>();
            }

            return _parentCanvas;
        }

        void Refresh()
        {
            Canvas parentCanvas = GetParentCanvas();

            if (parentCanvas == null)
            {
                return;
            }

            if (!_isRegistered)
            {
                BakingCamera.RegisterConsumer();
                _isRegistered = true;
            }

            _meshHelper.Clear();

            if (_particleSystem.particleCount > 0)
            {
                Camera meshBakingCamera = BakingCamera.GetCamera(parentCanvas);
#if UNITY_2023_2_OR_NEWER
                const ParticleSystemBakeMeshOptions bakeOptions = ParticleSystemBakeMeshOptions.Default;
                _particleSystemRenderer.BakeMesh(_meshHelper.GetTemporaryMesh(), meshBakingCamera, bakeOptions);
#else
                _particleSystemRenderer.BakeMesh(_meshHelper.GetTemporaryMesh(), meshBakingCamera, useTransform: false);
#endif

                bool isTrailsEnabled = _trailsModule.enabled;

                if (isTrailsEnabled)
                {
#if UNITY_2023_2_OR_NEWER
                    _particleSystemRenderer.BakeTrailsMesh(
                        _meshHelper.GetTemporaryMesh(),
                        meshBakingCamera,
                        bakeOptions
                    );
#else
                    _particleSystemRenderer.BakeTrailsMesh(_meshHelper.GetTemporaryMesh(), meshBakingCamera, useTransform: false);
#endif
                }

                Matrix4x4 matrix = _mainModule.simulationSpace == ParticleSystemSimulationSpace.World
                    ? transform.worldToLocalMatrix
                    : Matrix4x4.identity;

                _meshHelper.CombineTemporaryMeshes(ScaleZ * matrix);
            }

            canvasRenderer.SetMesh(_meshHelper.mainMesh);
        }
    }
}
