using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityUiParticles
{
    [CustomEditor(typeof(ParticleSystemMeshGenerator), editorForChildClasses: false)]
    [CanEditMultipleObjects]
    public class ParticleSystemMeshGeneratorEditor : Editor
    {
        SerializedProperty _material;
        SerializedProperty _trailsMaterial;
        SerializedProperty _maskable;
        ParticleSystemMeshGenerator _particleSystemMeshGenerator;

        void OnEnable()
        {
            _material = serializedObject.FindProperty("_material");
            _trailsMaterial = serializedObject.FindProperty("_trailsMaterial");
            _maskable = serializedObject.FindProperty("m_Maskable");
            _particleSystemMeshGenerator = (ParticleSystemMeshGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_material);
            EditorGUILayout.PropertyField(_trailsMaterial);
            EditorGUILayout.PropertyField(_maskable);
            serializedObject.ApplyModifiedProperties();

            var errorsBuilder = new StringBuilder();

            Check(errorsBuilder, _particleSystemMeshGenerator);

            string errorText = errorsBuilder.Length > 0
                ? string.Join("\n", errorsBuilder)
                : null;

            if (!string.IsNullOrEmpty(errorText))
            {
                EditorGUILayout.HelpBox(errorText, MessageType.Error);
            }
        }

        public static void Check(StringBuilder errorsBuilder, ParticleSystemMeshGenerator psmg)
        {
            var ps = psmg.GetComponent<ParticleSystem>();
            var psRenderer = psmg.GetComponent<ParticleSystemRenderer>();
            ParticleSystem.MainModule mainModule = ps.main;
            ParticleSystem.TextureSheetAnimationModule texSheetAnimationModule = ps.textureSheetAnimation;

            if (psRenderer.enabled)
            {
                errorsBuilder.AppendLine("ParticleSystemRenderer has to be disabled for UI Particles");
            }

            if (psRenderer.renderMode == ParticleSystemRenderMode.None)
            {
                errorsBuilder.AppendLine("ParticleSystemRenderer renderMode is None. Default: Billboard");
            }

            // Using Trails module leads to using 2 materials with 2 different textures determined inside each material.
            // Sprites mode in Texture sheet animation module requires CanvasRenderer.SetTexture that overrides texture for all materials.
            // Also the requirement of the 'Sprites' mode that all the sprites were inside the same texture, makes it redundant.
            // Just use the 'Grid' mode instead.
            if (texSheetAnimationModule.enabled
             && texSheetAnimationModule.mode == ParticleSystemAnimationMode.Sprites)
            {
                errorsBuilder.AppendLine("Texture sheet animation 'Sprites' mode is unsupported for UI Particles");
            }

            switch (mainModule.simulationSpace)
            {
                case ParticleSystemSimulationSpace.World:
                    if (mainModule.scalingMode != ParticleSystemScalingMode.Hierarchy)
                    {
                        errorsBuilder.AppendLine(
                            "Scaling mode for 'World' simulation space has to be 'Hierarchy' in UI Particles"
                        );
                    }

                    break;

                case ParticleSystemSimulationSpace.Local:
                    if (mainModule.scalingMode != ParticleSystemScalingMode.Local)
                    {
                        errorsBuilder.AppendLine(
                            "Scaling mode for 'Local' simulation space has to be 'Local' in UI Particles"
                        );
                    }

                    break;
            }
        }
    }
}
