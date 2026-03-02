using System;
using UnityEditor;
using UnityEngine;

namespace Salvage.ClothingCuller.Editor.Configuration
{
    public static class OcclusionDataViewerConfiguration
    {
        public static Lazy<Material> OccludeeMaterial { get; }
        public static Lazy<Material> OccluderMaterial { get; }
        public static Lazy<Material> OccludedMaterial { get; }

        #region Constructor

        static OcclusionDataViewerConfiguration()
        {
            OccludeeMaterial = new Lazy<Material>(() => UnityRenderPipelineHelper.CreateOpaqueMaterial(nameof(OccludeeMaterial), new Color32(0, 100, 0, 1)));
            OccluderMaterial = new Lazy<Material>(() => UnityRenderPipelineHelper.CreateTransparentMaterial(nameof(OccluderMaterial), new Color(1f, 1f, 1f, PersistentValues.OccluderAlpha)));
            OccludedMaterial = new Lazy<Material>(() => UnityRenderPipelineHelper.CreateOpaqueMaterial(nameof(OccludedMaterial), new Color32(139, 0, 0, 1)));
        }

        #endregion

        #region Private Methods



        #endregion

        #region Public Methods



        #endregion

        #region Inner Classes

        public enum BrushType
        {
            Triangle,
            Circle,
            Sphere
        }

        public static class PersistentValues
        {
            private const string editorPrefsPrefix = "CC_";

            #region Properties

            public static float OccluderAlpha
            {
                get
                {
                    return EditorPrefs.GetFloat($"{editorPrefsPrefix}{nameof(OccluderAlpha)}", 1f);
                }
                set
                {
                    EditorPrefs.SetFloat($"{editorPrefsPrefix}{nameof(OccluderAlpha)}", value);
                }
            }

            public static BrushType SelectedBrushType
            {
                get
                {
                    return (BrushType)EditorPrefs.GetInt($"{editorPrefsPrefix}{nameof(SelectedBrushType)}", 0);
                }
                set
                {
                    EditorPrefs.SetInt($"{editorPrefsPrefix}{nameof(SelectedBrushType)}", (int)value);
                }
            }

            public static float PaintRadius
            {
                get
                {
                    return EditorPrefs.GetFloat($"{editorPrefsPrefix}{nameof(PaintRadius)}", 0f);
                }
                set
                {
                    EditorPrefs.SetFloat($"{editorPrefsPrefix}{nameof(PaintRadius)}", value);
                }
            }

            public static bool ShowOccludeeWireFrame
            {
                get
                {
                    return EditorPrefs.GetBool($"{editorPrefsPrefix}{nameof(ShowOccludeeWireFrame)}", false);
                }
                set
                {
                    EditorPrefs.SetBool($"{editorPrefsPrefix}{nameof(ShowOccludeeWireFrame)}", value);
                }
            }

            public static bool ShowOccluderWireFrame
            {
                get
                {
                    return EditorPrefs.GetBool($"{editorPrefsPrefix}{nameof(ShowOccluderWireFrame)}", false);
                }
                set
                {
                    EditorPrefs.SetBool($"{editorPrefsPrefix}{nameof(ShowOccluderWireFrame)}", value);
                }
            }

            public static bool ViewMeshesAsSkinned
            {
                get
                {
                    return EditorPrefs.GetBool($"{editorPrefsPrefix}{nameof(ViewMeshesAsSkinned)}", true);
                }
                set
                {
                    EditorPrefs.SetBool($"{editorPrefsPrefix}{nameof(ViewMeshesAsSkinned)}", value);
                }
            }

            public static float DefaultPokeThroughRaycastDistance
            {
                get
                {
                    return EditorPrefs.GetFloat($"{editorPrefsPrefix}{nameof(DefaultPokeThroughRaycastDistance)}", 0.02f);
                }
                set
                {
                    EditorPrefs.SetFloat($"{editorPrefsPrefix}{nameof(DefaultPokeThroughRaycastDistance)}", value);
                }
            }

            public static int DefaultAmountOfRaycastsPerVertex
            {
                get
                {
                    return EditorPrefs.GetInt($"{editorPrefsPrefix}{nameof(DefaultAmountOfRaycastsPerVertex)}", 1000);
                }
                set
                {
                    EditorPrefs.SetInt($"{editorPrefsPrefix}{nameof(DefaultAmountOfRaycastsPerVertex)}", value);
                }
            }

            #endregion
        }

        #endregion

    }
}
