// Copyright 2024 Alejandro Villalba Avila

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SiliconHeart.Rendering
{
    /// <summary>
    /// Determines how a 2D pixel-perfect line strip is to be rendered: the position of the points, the colors, the thickness, etc. It sends the data to the shader of the GPU which
    /// calculates the shape of the lines and their final colors.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(SortingGroup))]
    [DisallowMultipleComponent]
    public class LineRenderer2D : MonoBehaviour
    {
        /// <summary>
        /// A way to find the camera to be used to render the line. Camera resolvers are optional and can be shared among several lines.
        /// </summary>
        public abstract class CameraResolver
        {
            public delegate void CameraChangedDelegate();

            /// <summary>
            /// Raised when the current camera should be updated. Lines subscribe to it.
            /// </summary>
            public event CameraChangedDelegate CameraChanged;

            /// <summary>
            /// Tells the subscribed lines to update their camera.
            /// </summary>
            protected void NotifyCameraChanged()
            {
                CameraChanged?.Invoke();
            }

            /// <summary>
            /// Searches for a camera used to render lines.
            /// </summary>
            /// <returns>The camera to be used by line renderers.</returns>
            public abstract Camera GetCamera();
        }

        [Tooltip("The position of every point in the line. Its world position depends on whether the Local Space options is enabled.")]
        [SerializeField]
        protected List<Vector2> m_Points = new List<Vector2>();

        [Tooltip("The color of the line.")]
        [SerializeField]
        protected Color m_MainColor = Color.red;

        [Tooltip("The color used in the area of the quad that is not filled with the line.")]
        [SerializeField]
        protected Color m_BackgroundColor = Color.clear;

        [Tooltip("The width of every 'pixel' of the line, in screen pixels.")]
        [SerializeField]
        protected float m_Thickness = 4;

        [Tooltip("The amount of pixels to offset the dots towards the last endpoint.")]
        [SerializeField]
        protected int m_DotOffset = 0;

        [Tooltip("The amount of pixels that will be drawn contiguously in a dot.")]
        [Min(1)]
        [SerializeField]
        protected int m_DotLength = 1;

        [Tooltip("The amount of pixels that separate a dot from other.")]
        [Min(0.0f)]
        [SerializeField]
        protected int m_DotSpaceLength = 0;

        [Tooltip("The start color of the color gradient (at the first point of the line strip). The gradient is multiplied by existing colors.")]
        [SerializeField]
        protected Color m_GradientStartColor = Color.white;

        [Tooltip("The end color of the color gradient (at the last point of the line strip). The gradient is multiplied by existing colors.")]
        [SerializeField]
        protected Color m_GradientEndColor = Color.white;

        [Tooltip("The amount of pixels along which the gradient will be drawn. The end color will be used after the last pixel of the gradient. If it equals zero, only the end color will be visible.")]
        [Min(0)]
        [SerializeField]
        protected int m_GradientLength = 0;

        [Tooltip("The amount of pixels to offset the start of the color gradient. The start color will be used before the firtst pixels of the gradient.")]
        [SerializeField]
        protected int m_GradientOffset = 0;

        [Tooltip("The amount of contiguous pixels to be drawn, at maximum, starting from the closest pixel to the first point of the line strip.")]
        [SerializeField]
        protected int m_MaximumLength = 99999;

        [Tooltip("The amount of pixels to offset the first drawn pixel, starting at the first point of the line strip. The amount of pixels to draw after this depends on the maximum length.")]
        [SerializeField]
        protected int m_StartPoint = 0;

        [SerializeField]
        protected bool m_StartPointAffectsAllOffsets = false;

        [Tooltip("The amount of pixels to offset the color pattern towards the last point of the line strip.")]
        [SerializeField]
        protected int m_PixelColorPatternOffset = 0;

        [Tooltip("The consecutive colors that form the pattern to repeat for each pixel of the line strip. Do not use this color list if a texture is already assigned.")]
        [SerializeField]
        protected List<Color> m_PixelColorPattern = new List<Color>();

        [Tooltip("The consecutive colors that form the pattern to repeat for each pixel of the line strip. Pixels are read left to right from the first row of the texture.")]
        [SerializeField]
        protected Texture2D m_PointColorPatternTexture;

        [Tooltip("When enabled, the position of the points is sent to the GPU once per frame without having to apply the changes.")]
        [SerializeField]
        protected bool m_AutoApplyPositionChanges = true;

        [Tooltip("When enabled, the world position of the parent is added to the position of the points which is assumed to be local.")]
        [SerializeField]
        protected bool m_PositionsAreLocalSpace = false;

        [Tooltip("The camera used for calculating the actual thickness on screen. If may be empty if it is expected to be set later, at runtime. When the editor is not playing, it will use the camera of the scene view")]
        [SerializeField]
        protected Camera m_Camera = null;

        [Tooltip("Adjustment multiplied by the thickness to make sure the width of the quad is enough for all slopes of the lines.")]
        [SerializeField]
        protected float m_QuadWidthFactor = 2.3f;

        [Tooltip("A texture to project on the line. It repeats infinitely.")]
        [SerializeField]
        protected Texture2D m_OverlayTexture = null;

        [Tooltip("An offset to apply to the overlay texture. 1 means the texture is displaced by its own size.")]
        [SerializeField]
        protected Vector2 m_OverlayOffset = Vector2.zero;

        [Tooltip("The overlay texture is drawn as tiles. Each tile is as big as the original texture, divided by this factor.")]
        [SerializeField]
        protected Vector2 m_OverlayTileRepetitions = Vector2.one;

        [Tooltip("The value of the camera's orthographic size for which the line will be drawn with the specified thickness on the screen. Use it if the zoom of the camera is expected to change while the line is being rendered. A value of zero will make the line keep its thickness in screen whatever the orthogonal size of the camera is.")]
        [Min(0.0f)]
        [SerializeField]
        protected float m_ReferenceOrthographicSize = 0.0f;

        [Tooltip("When it is not empty, a camera with this tag will be automatically assigned to the line.")]
        [SerializeField]
        protected string m_CameraTag = string.Empty;

        [Tooltip("When enabled, the amount of points that form the line can grow until it reaches the maximum (points can be removed too). Otherwise, no more points (apart from the ones added in editor) will be allowed although removals will still be possible. For performance reasons, it is recommended to disable it when no more points are expected to be added.")]
        [SerializeField]
        protected bool m_CanAddPoints = true;

        [Tooltip("When enabled, the length of the gradient will be calculated automatically so it always matches the length of the line strip. As a result, the gradient will extend along the line strip regardless of its length.")]
        [SerializeField]
        protected bool m_AdaptiveGradientLength = false;

        /// <summary>
        /// Gets or sets whether the position of the points is sent to the GPU once per frame without having to apply the changes.
        /// </summary>
        public bool AutoApplyPositionChanges
        {
            get
            {
                return m_AutoApplyPositionChanges;
            }

            set
            {
                m_AutoApplyPositionChanges = value;
            }
        }

        /// <summary>
        /// Gets the maximum amount of points the renderer can draw. The number of points can vary but it cannot be greater than this value.
        /// </summary>
        public int MaximumAmountOfPoints
        {
            get
            {

#if UNITY_EDITOR

                return Application.isPlaying ? m_maximumAmountOfPoints : GetSelectedAmountFromKeywords();
#else
                return m_maximumAmountOfPoints;
#endif
            }

            set
            {
                m_maximumAmountOfPoints = value;
            }
        }

        /// <summary>
        /// Gets the current thickness of the line on the screen, in actual pixels. It may change as the camera zooms in and out.
        /// </summary>
        public float ActualThickness
        {
            get
            {
                return m_actualThickness * GlobalThicknessMultiplier;
            }
        }

        /// <summary>
        /// Gets whether the amount of points that form the line can grow until it reaches the maximum (points can be removed too). Otherwise, no more points (apart from the ones added in editor) will be allowed although removals will still be possible.
        /// </summary>
        public bool CanAddPoints
        {
            get
            {
                return m_CanAddPoints;
            }
        }

        /// <summary>
        /// Gets or sets the camera the renderer uses for calculating the actual thickness of the line on screen.
        /// When the editor is not playing, it will use the camera of the scene view.
        /// </summary>
        public Camera CurrentCamera
        {
            get
            {
                return m_Camera
#if UNITY_EDITOR
                                == null ? GetEditorCamera() : m_Camera
#endif
                       ;
            }

            set
            {
                m_Camera = value;
                AdaptToCameraOrthogonalSize();
                ApplyGeometryChanges();
            }
        }

        protected float PixelsPerUnit
        {
            get
            {
#if UNITY_EDITOR

                if (!Application.isPlaying && m_editorCamera != null)
                {
                    return m_editorCamera.pixelHeight / (m_editorCamera.orthographicSize * 2.0f);
                }

#endif

                return m_pixelsPerUnit;
            }
        }

        /// <summary>
        /// Gets or sets the color of the line.
        /// </summary>
        public Color MainColor
        {
            get
            {
                return m_MainColor;
            }

            set
            {
                m_MainColor = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the color used in the area of the quad that is not filled with the line. It should be fully transparent, otherwise it may incur in performance degradation.
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                return m_BackgroundColor;
            }

            set
            {
                m_BackgroundColor = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the width of every point in the line, in pixels.
        /// </summary>
        public float Thickness
        {
            get
            {
                return m_Thickness;
            }

            set
            {
                m_Thickness = value;
                AdaptToCameraOrthogonalSize();
                ApplyGeometryChanges();
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels to offset the dots towards the last endpoint.
        /// </summary>
        public int DotOffset
        {
            get
            {
                return m_DotOffset;
            }

            set
            {
                m_DotOffset = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels that will be drawn contiguously in a dot.
        /// </summary>
        public int DotLength
        {
            get
            {
                return m_DotLength;
            }

            set
            {
                m_DotLength = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels that separate a dot from other.
        /// </summary>
        public int DotSpaceLength
        {
            get
            {
                return m_DotSpaceLength;
            }

            set
            {
                m_DotSpaceLength = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the start color of the color gradient(at the first point of the line strip). The gradient is multiplied by existing colors.
        /// </summary>
        public Color GradientStartColor
        {
            get
            {
                return m_GradientStartColor;
            }

            set
            {
                m_GradientStartColor = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the end color of the color gradient(at the last point of the line strip). The gradient is multiplied by existing colors.
        /// </summary>
        public Color GradientEndColor
        {
            get
            {
                return m_GradientEndColor;
            }

            set
            {
                m_GradientEndColor = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels along which the gradient will be drawn. The end color will be used after the last pixel of the gradient. If it equals zero, only the end color will be visible.
        /// </summary>
        public int GradientLength
        {
            get
            {
                return m_GradientLength;
            }

            set
            {
                m_GradientLength = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels to offset the start of the color gradient. The start color will be used before the firtst pixels of the gradient.
        /// </summary>
        public int GradientOffset
        {
            get
            {
                return m_GradientOffset;
            }

            set
            {
                m_GradientOffset = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of contiguous pixels to be drawn, at maximum, starting from the closest pixel to the first point of the line strip.
        /// </summary>
        public int MaximumLength
        {
            get
            {
                return m_MaximumLength;
            }

            set
            {
                m_MaximumLength = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels to offset the first drawn pixel, starting at the first point of the line strip. The amount of pixels to draw after this depends on the maximum length.
        /// </summary>
        public int StartPoint
        {
            get
            {
                return m_StartPoint;
            }

            set
            {
                m_StartPoint = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the amount of pixels to offset the color pattern towards the last point of the line strip.
        /// </summary>
        public int PixelColorPatternOffset
        {
            get
            {
                return m_PixelColorPatternOffset;
            }

            set
            {
                m_PixelColorPatternOffset = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the texture to project on the line. It repeats infinitely.
        /// </summary>
        public Texture2D OverlayTexture
        {
            get
            {
                return m_OverlayTexture;
            }

            set
            {
                m_OverlayTexture = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets an offset to apply to the overlay texture. 1 means the texture is displaced by its own size.
        /// </summary>
        public Vector2 OverlayOffset
        {
            get
            {
                return m_OverlayOffset;
            }

            set
            {
                m_OverlayOffset = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets or sets the repetition factor of the overlay texture. The overlay texture is drawn as tiles. Each tile is as big as the original texture, divided by this factor.
        /// </summary>
        public Vector2 OverlayTileRepetitions
        {
            get
            {
                return m_OverlayTileRepetitions;
            }

            set
            {
                m_OverlayTileRepetitions = value;
                ApplyPropertyChanges();
            }
        }

        /// <summary>
        /// Gets a reference to the points of the line strip. It is not allowed to modify the length of the list, instead call <see cref="SetPointCount"/>, <see cref="AddPoint"/> or <see cref="RemovePoint"/>. 
        /// </summary>
        public List<Vector2> Points
        {
            get
            {
                return m_Points;
            }
        }

        /// <summary>
        /// Gets the amount of pixels used for drawing the line, taking into account the thickness and the camera's orthogonal size (so they are not actual screen pixels).
        /// This value is automatically updated when any point is moved, only if the adaptive gradient length is enabled.
        /// </summary>
        public int PixelLength
        {
            get
            {
                return m_pixelLength;
            }
        }

        /// <summary>
        /// Gets or sets a camera resolver implementation that will be automatically assigned to every new line renderer on Start.
        /// If not set, default methods will be used to find the camera.
        /// If a line renderer has its own resolver, that instance will be used instead as if the global resolver did not exist.
        /// </summary>
        public static CameraResolver GlobalCameraResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether the material uses a fixed amount of points or not.
        /// </summary>
        protected bool IsAmountOfPointsLimited
        {
            get
            {
                return MaximumAmountOfPoints != MAXIMUM_AMOUNT_OF_POINTS_VALUES[MAXIMUM_AMOUNT_OF_POINTS_VALUES.Length - 1];
            }
        }

        private int m_maximumAmountOfPoints = 2;
        private int m_packedPointsCount;
        private float m_pixelsPerUnit;
        private int m_pixelLength;

        private Texture2D m_packedPointsTexture;
        private Color[] m_packedPoints;
        private Vector4[] m_packedPointsAsVectors;
        private bool m_isPositionsDirty = false;
        private bool m_isColorPatternDirty = false;
        private bool m_isLayoutDirty = false;
        private bool m_isGeometryDirty = false;
        private bool m_isPropertyDirty = false;
        private MaterialPropertyBlock m_materialPropertyBlock;

        private MeshFilter m_meshFilter;
        private MeshRenderer m_meshRenderer;
        private Mesh m_mesh;

        private Vector3[] m_quadVertices;
        private int[] m_quadIndices;

        private float m_actualThickness = 0.0f;
        private float m_previousOrtographicSize = 0.0f;
        private CameraResolver m_cameraResolver;

        private delegate void GlobalThicknessMultiplierChangedDelegate();
        private static event GlobalThicknessMultiplierChangedDelegate sm_globalThicknessMultiplierChanged;

        private static float sm_globalThicknessMultiplier = 1.0f;

        /// <summary>
        /// Gets or sets the scale of the thickness for all the existing 2D line renderers.
        /// </summary>
        /// <remarks>
        /// It can be used to make the thickness of the lines on the screen stay the same when the resolution changes. For example, if the value is 1 when resolution is Full HD, it should be 2 when using 4K.
        /// </remarks>
        public static float GlobalThicknessMultiplier
        {
            get
            {
                return sm_globalThicknessMultiplier;
            }

            set
            {
                sm_globalThicknessMultiplier = value;
                sm_globalThicknessMultiplierChanged?.Invoke();
            }
        }

#if UNITY_EDITOR

        protected Camera m_editorCamera;

#endif

        private static class ShaderParams
        {
            public static readonly int LineColor = Shader.PropertyToID("_LineColor");
            public static readonly int Thickness = Shader.PropertyToID("_Thickness");
            public static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
            public static readonly int Origin = Shader.PropertyToID("_Origin");
            public static readonly int PackedPoints = Shader.PropertyToID("_PackedPoints");
            public static readonly int DotOffset = Shader.PropertyToID("_DotOffset");
            public static readonly int DotLength = Shader.PropertyToID("_DotLength");
            public static readonly int DotSpaceLength = Shader.PropertyToID("_DotSpaceLength");
            public static readonly int GradientStartColor = Shader.PropertyToID("_GradientStartColor");
            public static readonly int GradientEndColor = Shader.PropertyToID("_GradientEndColor");
            public static readonly int GradientLength = Shader.PropertyToID("_GradientLength");
            public static readonly int GradientOffset = Shader.PropertyToID("_GradientOffset");
            public static readonly int MaximumLength = Shader.PropertyToID("_MaximumLength");
            public static readonly int StartPoint = Shader.PropertyToID("_StartPoint");
            public static readonly int StartPointAffectsAllOffsets = Shader.PropertyToID("_StartPointAffectsAllOffsets");
            public static readonly int ColorOffset = Shader.PropertyToID("_ColorOffset");
            public static readonly int PointColorPatternCount = Shader.PropertyToID("_PointColorPatternCount");
            public static readonly int PointColorPattern = Shader.PropertyToID("_PointColorPattern");
            public static readonly int OverlayTexture = Shader.PropertyToID("_OverlayTexture");
            public static readonly int OverlayTextureST = Shader.PropertyToID("_OverlayTexture_ST");
            public static readonly int OverlayTextureSize = Shader.PropertyToID("_OverlayTextureSize");
        }

        private static readonly string[] MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS = new string[]{ "MAXIMUM_AMOUNT_OF_POINTS__2",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__32",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__128",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED" };
        private static readonly int[] MAXIMUM_AMOUNT_OF_POINTS_VALUES = new int[] { 2, 32, 128, -1 };

        /// <summary>
        /// Adds a new point at the end of the line strip.
        /// </summary>
        public void AddPoint()
        {
            SetPointCount(m_Points.Count + 1);
        }

        /// <summary>
        /// Removes the last point of the line strip. The line will always contain at least 2 points.
        /// </summary>
        public void RemovePoint()
        {
            SetPointCount(m_Points.Count - 1);
        }

        /// <summary>
        /// Establishes the amount of points that form the line. Points will be added or removed without changing the position of the existing ones.
        /// </summary>
        /// <param name="pointCount">The amount of points that form the line (the line will always contain at least 2 points).</param>
        public void SetPointCount(int pointCount)
        {
            if(!m_CanAddPoints && IsAmountOfPointsLimited && Application.isPlaying && pointCount > m_Points.Count)
            {
                Debug.LogError("The line is configured not to allow adding new points. Line name: " + name);
                return;
            }

            if(pointCount != m_Points.Count)
            {
                OnPointCountChanged(Mathf.Max(pointCount, 2));

                ApplyLayoutChanges();
                ApplyGeometryChanges();
                ApplyPointPositionChanges();
            }
        }

        /// <summary>
        /// Provides a method to find the camera used for rendering the line. If no resolver is provided, default methods will be used to find the camera.
        /// When a resolver is set for a specific line renderer, it will ignore the global resolver.
        /// </summary>
        /// <param name="resolver">The implementation of a camera resolver.</param>
        public void SetCameraResolver(CameraResolver resolver)
        {
            if(m_cameraResolver != null)
            {
                m_cameraResolver.CameraChanged -= OnCameraResolverCameraChanged;
            }

            if(resolver != null)
            {
                resolver.CameraChanged += OnCameraResolverCameraChanged;
                CurrentCamera = resolver.GetCamera();
            }
            
            m_cameraResolver = resolver;
        }

        /// <summary>
        /// Called when the amount of points changes so they can be added / removed.
        /// </summary>
        /// <param name="newPointCount">The new amount of points that form the line. The minimum is 2.</param>
        protected virtual void OnPointCountChanged(int newPointCount)
        {
            int pointCount = m_Points.Count;

            if (newPointCount > pointCount)
            {
                Vector2 newPoint = pointCount > 0 ? m_Points[pointCount - 1]
                                                  : Vector2.zero;

                for (int i = pointCount; i < newPointCount; ++i)
                {
                    if (IsAmountOfPointsLimited && i + 1 > MaximumAmountOfPoints)
                    {
                        Debug.LogError("It is not possible to add more points than the maximum (" + MaximumAmountOfPoints + "). Line name: " + name);
                        break;
                    }

                    m_Points.Add(newPoint);
                }
            }
            else
            {
                for (int i = pointCount - 1; i >= newPointCount; --i)
                {
                    m_Points.RemoveAt(i);
                }
            }
        }

        protected virtual void Awake()
        {
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshRenderer = GetComponent<MeshRenderer>();

            m_mesh = new Mesh();
            m_mesh.MarkDynamic();
            m_meshFilter.mesh = m_mesh;

            MaximumAmountOfPoints = GetSelectedAmountFromKeywords();
        }

        protected virtual void Start()
        {
            if(!Application.isPlaying)
            {
                return;
            }

            MaximumAmountOfPoints = GetSelectedAmountFromKeywords();

            if (m_materialPropertyBlock == null)
            {
                m_materialPropertyBlock = new MaterialPropertyBlock();

                if (m_meshRenderer != null)
                {
                    m_meshRenderer.GetPropertyBlock(m_materialPropertyBlock);

                    if(IsAmountOfPointsLimited)
                    {
                        GeneratePackedPointsAsVectors();
                    }
                }
            }

            if(GlobalCameraResolver != null && m_cameraResolver == null)
            {
                SetCameraResolver(GlobalCameraResolver);
            }

            // Looks for a camera with a tag, if any
            if (m_Camera == null && !string.IsNullOrEmpty(m_CameraTag))
            {
                Camera foundCamera = GameObject.FindWithTag(m_CameraTag)?.GetComponent<Camera>();

                if (foundCamera != null)
                {
                    CurrentCamera = foundCamera;
                }
                else
                {
                    Debug.LogError("It was not possible to find a Camera with tag '" + m_CameraTag + "' to assign to line '" + name + "'.");
                }
            }

            if (CurrentCamera != null)
            {
                AdaptToCameraOrthogonalSize(isInitialization: true);
            }

            ApplyColorPatternChanges();
            ApplyLayoutChanges();
            ApplyGeometryChanges();
            ApplyPointPositionChanges();
            ApplyPropertyChanges();

            RefreshMaterial();
        }

        protected virtual void OnEnable()
        {
            sm_globalThicknessMultiplierChanged += OnPixelPerUnitMultiplierChanged;

#if UNITY_EDITOR

            // OnEnabled must be executed only in editor
            if (Application.isPlaying)
            {
                return;
            }

            if(m_cameraResolver != null)
            {
                CurrentCamera = m_cameraResolver.GetCamera();
            }

            MaximumAmountOfPoints = GetSelectedAmountFromKeywords();

            if (m_materialPropertyBlock == null)
            {
                m_materialPropertyBlock = new MaterialPropertyBlock();

                if (m_meshRenderer != null)
                {
                    m_meshRenderer.GetPropertyBlock(m_materialPropertyBlock);

                    if (IsAmountOfPointsLimited)
                    {
                        GeneratePackedPointsAsVectors();
                    }
                }
            }

            if (m_Points.Count < 2)
            {
                Debug.LogWarning("It is not allowed to set the point count of the line to less than 2. Line name: " + name);
                SetPointCount(2);
            }
            else
            {
                GenerateGeometry();
            }

            if(CurrentCamera != null)
            {
                AdaptToCameraOrthogonalSize(isInitialization: true);
            }
            
            ApplyColorPatternChanges();
            ApplyLayoutChanges();
            ApplyGeometryChanges();
            ApplyPointPositionChanges();
            ApplyPropertyChanges();
   
            RefreshMaterial();

            SceneView.RepaintAll();

#endif
        }

        private void OnDisable()
        {
            sm_globalThicknessMultiplierChanged -= OnPixelPerUnitMultiplierChanged;
        }

        private void OnPixelPerUnitMultiplierChanged()
        {
            ApplyPropertyChanges();
        }

        protected virtual void LateUpdate()
        {
            if (CurrentCamera == null)
            {
                Debug.LogError("The line renderer does not have a camera, the line will not be drawn.");
                return;
            }

            if(IsAmountOfPointsLimited && m_Points.Count > MaximumAmountOfPoints)
            {
                Debug.LogError("The amount of points must not exceed the maximum when using fixed size lines. Line name: " + name, this);
            }

            if(CurrentCamera.orthographicSize != m_previousOrtographicSize)
            {
                m_previousOrtographicSize = CurrentCamera.orthographicSize;

                AdaptToCameraOrthogonalSize();
            }
            
            if (m_AutoApplyPositionChanges || transform.hasChanged)
            {
                transform.hasChanged = false;
                ApplyPointPositionChanges();
            }

#if UNITY_EDITOR

            // Sometimes the pixels per unit is not calculated properly when the editor loads the scene
            if(m_pixelsPerUnit == 0.0f)
            {
                AdaptToCameraOrthogonalSize(isInitialization: true);
            }

            // This prevents the line from using an obsolete geometry when undoing operations
            if (m_quadVertices.Length != (m_Points.Count - 1) * 4)
            {
                ApplyGeometryChanges();
            }

#endif

            RefreshMaterial();
        }

        protected virtual void OnDestroy()
        {
            if (m_cameraResolver != null)
            {
                m_cameraResolver.CameraChanged -= OnCameraResolverCameraChanged;
            }
        }

        private void AdaptToCameraOrthogonalSize(bool isInitialization = false)
        {
            if (m_ReferenceOrthographicSize != 0.0f)
            {
                float currentCameraOrthographicSize = CurrentCamera.orthographicSize;
                m_actualThickness = m_Thickness * m_ReferenceOrthographicSize / currentCameraOrthographicSize;
                m_pixelsPerUnit = CurrentCamera.pixelHeight / (currentCameraOrthographicSize * 2.0f);

                ApplyGeometryChanges();
                ApplyPropertyChanges();
            }
            else if(isInitialization)
            {
                m_actualThickness = m_Thickness;
                m_pixelsPerUnit = CurrentCamera.pixelHeight / (CurrentCamera.orthographicSize * 2.0f);
            }
        }

        private int GetSelectedAmountFromKeywords()
        {
            if(m_meshRenderer != null)
            {
                Material lineMaterial = m_meshRenderer.sharedMaterial;

                for (int i = 0; i < MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS.Length; ++i)
                {
                    if (lineMaterial.IsKeywordEnabled(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[i]))
                    {
                        return MAXIMUM_AMOUNT_OF_POINTS_VALUES[i];
                    }
                }

                // If no keyword is enabled, it enables the first by default
                lineMaterial.EnableKeyword(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[0]);
            }
            
            return MAXIMUM_AMOUNT_OF_POINTS_VALUES[0];
        }

        /// <summary>
        /// Marks the layout of the line (the number of points) as dirty, so the new values will be sent to the GPU right before the line is rendered again.
        /// </summary>
        protected void ApplyLayoutChanges()
        {
            m_isLayoutDirty = true;
        }

        /// <summary>
        /// Marks the positions of the points as dirty, so the new values will be sent to the GPU right before the line is rendered again.
        /// </summary>
        public void ApplyPointPositionChanges()
        {
            m_isPositionsDirty = true;
        }

        /// <summary>
        /// Marks the geometry of the line as dirty, so the shape of the renderers will be updated before the line is rendered again.
        /// </summary>
        protected void ApplyGeometryChanges()
        {
            m_isGeometryDirty = true;
        }

        /// <summary>
        /// Marks the properties of the line as dirty, so the values will applied to the material before the line is rendered again.
        /// </summary>
        protected void ApplyPropertyChanges()
        {
            m_isPropertyDirty = true;
        }

        /// <summary>
        /// Marks the color pattern of the line as dirty, so the values will applied to the material before the line is rendered again.
        /// </summary>
        protected void ApplyColorPatternChanges()
        {
            m_isColorPatternDirty = true;
        }

        private void RefreshColorPattern()
        {
            if (m_PointColorPatternTexture == null || m_PointColorPatternTexture.width != m_PixelColorPattern.Count)
            {
                if (m_PointColorPatternTexture != null)
                {
#if UNITY_EDITOR
                    if (!EditorUtility.IsPersistent(m_PointColorPatternTexture))
                    {
#endif
                        DestroyImmediate(m_PointColorPatternTexture);
#if UNITY_EDITOR
                    }
#endif
                }

                if (m_PixelColorPattern.Count > 0)
                {
                    m_PointColorPatternTexture = new Texture2D(m_PixelColorPattern.Count, 1, TextureFormat.RGBAFloat, false, true);
                }
            }

            for (int i = 0; i < m_PixelColorPattern.Count; ++i)
            {
                m_PointColorPatternTexture.SetPixel(i, 0, m_PixelColorPattern[i]);
                m_PointColorPatternTexture.Apply();
            }

            // Point color pattern data
            m_materialPropertyBlock.SetTexture(ShaderParams.PointColorPattern, m_PointColorPatternTexture == null ? Texture2D.whiteTexture
                                                                                                                  : m_PointColorPatternTexture);
            m_materialPropertyBlock.SetFloat(ShaderParams.PointColorPatternCount, m_PointColorPatternTexture == null ? 0
                                                                                                                     : m_PointColorPatternTexture.width);
        }

        private void SendLayoutToGPU()
        {
            m_packedPointsCount = (m_Points.Count + 1) / 2;

            if(!IsAmountOfPointsLimited)
            {
                if (m_packedPoints == null || m_packedPoints.Length < m_packedPointsCount)
                {
                    m_packedPoints = new Color[m_packedPointsCount];
                }

                if (m_packedPointsTexture == null || m_packedPointsTexture.width != m_packedPointsCount)
                {
                    if (m_packedPointsTexture != null)
                    {
                        DestroyImmediate(m_packedPointsTexture);
                    }

                    m_packedPointsTexture = new Texture2D(m_packedPointsCount, 1, TextureFormat.RGBAFloat, false, true);
                }
            }

            // Point positions data
            if(IsAmountOfPointsLimited)
            {
                if(m_packedPointsAsVectors != null && m_packedPointsAsVectors.Length > 0)
                {
                    m_materialPropertyBlock.SetVectorArray(ShaderParams.PackedPoints, m_packedPointsAsVectors);
                }
            }
            else
            {
                m_materialPropertyBlock.SetTexture(ShaderParams.PackedPoints, m_packedPointsTexture);
            }
        }

        private void GeneratePackedPointsAsVectors()
        {
            if(Application.isPlaying)
            {
                m_packedPointsAsVectors = m_CanAddPoints ? new Vector4[(MaximumAmountOfPoints + 1) / 2]
                                                         : new Vector4[(m_Points.Count + 1) / 2];
            }
            else
            {
                m_packedPointsAsVectors = new Vector4[(MaximumAmountOfPoints + 1) / 2]; // In editor, it stores the maximum
            }
            
            m_materialPropertyBlock.SetVectorArray(ShaderParams.PackedPoints, m_packedPointsAsVectors);
        }

        private void SendPointPositionsToGPU()
        {
            if (m_Points.Count == 0 || PixelsPerUnit == 0.0f)
            {
                return;
            }

            Vector2 parentPosition = Vector2.zero;

            if (m_PositionsAreLocalSpace)
            {
                parentPosition = new Vector2(transform.position.x, transform.position.y);
            }

            float pixelSize = ActualThickness / PixelsPerUnit;
            float halfPixelSize = pixelSize * 0.5f;

            // Adjusts the point to the pixel grid, this stabilizes pixels so they do not vibrate when the parent moves
            if (pixelSize > 0.0f)
            {
                parentPosition.x = Mathf.Round(parentPosition.x / pixelSize) * pixelSize;
                parentPosition.y = Mathf.Round(parentPosition.y / pixelSize) * pixelSize;
            }

            // Packs the points in colors that contain 2 points each
            if(IsAmountOfPointsLimited)
            {
                if(m_packedPointsAsVectors == null || m_packedPointsAsVectors.Length < m_packedPointsCount)
                {
                    GeneratePackedPointsAsVectors();
                }

                int maxPackedPointsCount = Mathf.Min(m_packedPointsAsVectors.Length, m_packedPointsCount);

                for (int j = 0; j < maxPackedPointsCount; ++j)
                {
                    m_packedPointsAsVectors[j].x = m_Points[j * 2].x + parentPosition.x;
                    m_packedPointsAsVectors[j].y = m_Points[j * 2].y + parentPosition.y;

                    // Adjusts the point to the pixel grid, this stabilizes pixels so they do not vibrate when camera moves
                    if (pixelSize > 0.0f)
                    {
                        m_packedPointsAsVectors[j].x = Mathf.Round(m_packedPointsAsVectors[j].x / pixelSize) * pixelSize + halfPixelSize; // Adding half pixel reduces the amount of imprecisions
                        m_packedPointsAsVectors[j].y = Mathf.Round(m_packedPointsAsVectors[j].y / pixelSize) * pixelSize + halfPixelSize;
                    }

                    if (j * 2 + 1 >= m_Points.Count)
                    {
                        break;
                    }

                    m_packedPointsAsVectors[j].z = m_Points[j * 2 + 1].x + parentPosition.x;
                    m_packedPointsAsVectors[j].w = m_Points[j * 2 + 1].y + parentPosition.y;

                    // Adjusts the point to the pixel grid, this stabilizes pixels so they do not vibrate when camera moves
                    if (pixelSize > 0.0f)
                    {
                        m_packedPointsAsVectors[j].z = Mathf.Round(m_packedPointsAsVectors[j].z / pixelSize) * pixelSize + halfPixelSize;
                        m_packedPointsAsVectors[j].w = Mathf.Round(m_packedPointsAsVectors[j].w / pixelSize) * pixelSize + halfPixelSize;
                    }
                }
            }
            else
            {
                for (int j = 0; j < m_packedPointsCount; ++j)
                {
                    m_packedPoints[j].r = m_Points[j * 2].x + parentPosition.x;
                    m_packedPoints[j].g = m_Points[j * 2].y + parentPosition.y;

                    // Adjusts the point to the pixel grid, this stabilizes pixels so they do not vibrate when camera moves
                    if (pixelSize > 0.0f)
                    {
                        m_packedPoints[j].r = Mathf.Round(m_packedPoints[j].r / pixelSize) * pixelSize + halfPixelSize;
                        m_packedPoints[j].g = Mathf.Round(m_packedPoints[j].g / pixelSize) * pixelSize + halfPixelSize;
                    }

                    if (j * 2 + 1 >= m_Points.Count)
                    {
                        break;
                    }

                    m_packedPoints[j].b = m_Points[j * 2 + 1].x + parentPosition.x;
                    m_packedPoints[j].a = m_Points[j * 2 + 1].y + parentPosition.y;

                    // Adjusts the point to the pixel grid, this stabilizes pixels so they do not vibrate when camera moves
                    if (pixelSize > 0.0f)
                    {
                        m_packedPoints[j].a = Mathf.Round(m_packedPoints[j].a / pixelSize) * pixelSize + halfPixelSize;
                        m_packedPoints[j].b = Mathf.Round(m_packedPoints[j].b / pixelSize) * pixelSize + halfPixelSize;
                    }
                }
            }
            
            if(IsAmountOfPointsLimited)
            {
                m_materialPropertyBlock.SetVectorArray(ShaderParams.PackedPoints, m_packedPointsAsVectors);
            }
            else
            {
                if (m_packedPointsTexture == null || m_packedPointsTexture.width != m_packedPointsCount)
                {
                    if (m_packedPointsTexture != null)
                    {
                        DestroyImmediate(m_packedPointsTexture);
                    }

                    m_packedPointsTexture = new Texture2D(m_packedPointsCount, 1, TextureFormat.RGBAFloat, false, true);
                    SendLayoutToGPU();
                }

                m_packedPointsTexture.SetPixels(0, 0, m_packedPointsCount, 1, m_packedPoints);
                m_packedPointsTexture.Apply();
            }
        }

        private void RefreshQuads()
        {
            // This avoids the pixel blocks of the line to be cut-off
            float MINIMUM_THICKNESS = PixelsPerUnit == 0 ? ActualThickness
                                                         : ActualThickness / (float)PixelsPerUnit;

            bool isAmountOfPointsLimited = IsAmountOfPointsLimited;

            MINIMUM_THICKNESS *= m_QuadWidthFactor; // Adjustment to make sure the width of the mesh is enough for all slopes
            int pointCount = m_Points.Count;

            if(isAmountOfPointsLimited && pointCount > MaximumAmountOfPoints)
            {
                pointCount = MaximumAmountOfPoints;
            }

            for (int i = 0; i < pointCount - 1; ++i)
            {
                Vector2 nextPoint;
                Vector2 currentPoint;

                if(i % 2 == 0)
                {
                    int nextPointIndex = i / 2;
                    int currentPointIndex = nextPointIndex;
                    nextPoint = isAmountOfPointsLimited ? new Vector2(m_packedPointsAsVectors[nextPointIndex].z, m_packedPointsAsVectors[nextPointIndex].w)
                                                        : new Vector2(m_packedPoints[nextPointIndex].b, m_packedPoints[nextPointIndex].a);
                    currentPoint = isAmountOfPointsLimited ? new Vector2(m_packedPointsAsVectors[currentPointIndex].x, m_packedPointsAsVectors[currentPointIndex].y)
                                                           : new Vector2(m_packedPoints[currentPointIndex].r, m_packedPoints[currentPointIndex].g);
                }
                else
                {
                    int nextPointIndex = i / 2 + 1;
                    int currentPointIndex = i / 2;
                    nextPoint = isAmountOfPointsLimited ? new Vector2(m_packedPointsAsVectors[nextPointIndex].x, m_packedPointsAsVectors[nextPointIndex].y)
                                                        : new Vector2(m_packedPoints[nextPointIndex].r, m_packedPoints[nextPointIndex].g);
                    currentPoint = isAmountOfPointsLimited ? new Vector2(m_packedPointsAsVectors[currentPointIndex].z, m_packedPointsAsVectors[currentPointIndex].w)
                                                           : new Vector2(m_packedPoints[currentPointIndex].b, m_packedPoints[currentPointIndex].a);
                }
                
                Vector2 lineDirection = (nextPoint - currentPoint).normalized;
                Vector2 lineDirectionPerpendicular = Vector2.Perpendicular(lineDirection);

                m_quadVertices[0 + i * 4] = currentPoint - lineDirection * MINIMUM_THICKNESS - lineDirectionPerpendicular * MINIMUM_THICKNESS;
                m_quadVertices[1 + i * 4] = currentPoint - lineDirection * MINIMUM_THICKNESS + lineDirectionPerpendicular * MINIMUM_THICKNESS;
                m_quadVertices[2 + i * 4] = nextPoint + lineDirection * MINIMUM_THICKNESS + lineDirectionPerpendicular * MINIMUM_THICKNESS;
                m_quadVertices[3 + i * 4] = nextPoint + lineDirection * MINIMUM_THICKNESS - lineDirectionPerpendicular * MINIMUM_THICKNESS;

                m_quadVertices[0 + i * 4].z = i; // This was necessary as a workaround for the bug in Unity and Metal that made SV_PrimitiveID fail, Z stores the line index
                m_quadVertices[1 + i * 4].z = i;
                m_quadVertices[2 + i * 4].z = i;
                m_quadVertices[3 + i * 4].z = i;

                m_quadIndices[0 + i * 6] = 0 + i * 4;
                m_quadIndices[1 + i * 6] = 1 + i * 4;
                m_quadIndices[2 + i * 6] = 2 + i * 4;
                m_quadIndices[3 + i * 6] = 0 + i * 4;
                m_quadIndices[4 + i * 6] = 2 + i * 4;
                m_quadIndices[5 + i * 6] = 3 + i * 4;
            }

            Vector4 worldBounds = Calculate2DWorldBoundingBox(m_quadVertices); // World positions
            worldBounds.x -= MINIMUM_THICKNESS;
            worldBounds.y += MINIMUM_THICKNESS;
            worldBounds.z += 2 * MINIMUM_THICKNESS;
            worldBounds.w += 2 * MINIMUM_THICKNESS;

            Vector3 worldPosition = (Vector2)transform.position;

            for (int i = 0; i < m_quadVertices.Length; ++i)
            {
                m_quadVertices[i] -= worldPosition;
            }

            m_mesh.Clear();
            m_mesh.vertices = m_quadVertices;
            m_mesh.triangles = m_quadIndices;
            m_mesh.bounds = new Bounds(new Vector3(worldBounds.x + worldBounds.z * 0.5f, worldBounds.y - worldBounds.w * 0.5f) - worldPosition, new Vector3(worldBounds.z, worldBounds.w));

            m_mesh.UploadMeshData(false);

#if UNITY_EDITOR

            m_meshFilter.mesh = m_mesh; // This was added to prevent the editor preview from failing when Undoing
#endif
        }

        private static Vector4 Calculate2DWorldBoundingBox(Vector3[] points)
        {
            Vector4 bounds = new Vector4(float.MaxValue, -float.MaxValue, -float.MaxValue, float.MaxValue);

            for (int i = 0; i < points.Length; ++i)
            {
                bounds.x = bounds.x > points[i].x ? points[i].x 
                                                  : bounds.x;
                bounds.y = bounds.y < points[i].y ? points[i].y
                                                  : bounds.y;
                bounds.z = bounds.z < points[i].x ? points[i].x
                                                  : bounds.z;
                bounds.w = bounds.w > points[i].y ? points[i].y
                                                  : bounds.w;
            }

            bounds.z -= bounds.x;
            bounds.w = bounds.y - bounds.w;

            return bounds;
        }

        private void OnWillRenderObject()
        {
            // It cannot continue without a camera
            if (CurrentCamera == null)
            {
                return;
            }

            bool updateMaterial = m_isPropertyDirty || m_isColorPatternDirty || !Application.isPlaying;

            if (m_isPropertyDirty || !Application.isPlaying)
            {
                m_isPropertyDirty = false;

                SendPropertiesToGPU(m_materialPropertyBlock);
            }

            if (m_isColorPatternDirty || !Application.isPlaying)
            {
                m_isColorPatternDirty = false;
                RefreshColorPattern();
            }

            if(updateMaterial)
            {
                m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);

#if UNITY_EDITOR

                SceneView.RepaintAll();

#endif
            }
        }

        /// <summary>
        /// Sets the material property block before rendering if it changed (<see cref="SiliconHeart.Rendering.LineRenderer2D.ApplyPropertyChanges" />).
        /// Extend this method to send custom properties.
        /// </summary>
        /// <param name="materialPropertyBlock">The material property block instance to fill.</param>
        protected virtual void SendPropertiesToGPU(MaterialPropertyBlock materialPropertyBlock)
        {
            materialPropertyBlock.SetColor(ShaderParams.LineColor, m_MainColor);
            materialPropertyBlock.SetFloat(ShaderParams.Thickness, ActualThickness);
            materialPropertyBlock.SetColor(ShaderParams.BackgroundColor, m_BackgroundColor);
            materialPropertyBlock.SetFloat(ShaderParams.MaximumLength, m_MaximumLength);
            materialPropertyBlock.SetFloat(ShaderParams.StartPoint, m_StartPoint);
            materialPropertyBlock.SetFloat(ShaderParams.StartPointAffectsAllOffsets, m_StartPointAffectsAllOffsets ? 1.0f : 0.0f);

            materialPropertyBlock.SetFloat(ShaderParams.DotOffset, -m_DotOffset);
            materialPropertyBlock.SetFloat(ShaderParams.DotLength, m_DotLength);

            materialPropertyBlock.SetFloat(ShaderParams.DotSpaceLength, m_DotSpaceLength);

            if (m_AdaptiveGradientLength)
            {
                m_GradientLength = m_pixelLength;
            }

            if (m_GradientLength != 0)
            {
                materialPropertyBlock.SetColor(ShaderParams.GradientStartColor, m_GradientStartColor);
                materialPropertyBlock.SetFloat(ShaderParams.GradientOffset, -m_GradientOffset);
            }

            materialPropertyBlock.SetColor(ShaderParams.GradientEndColor, m_GradientEndColor);
            materialPropertyBlock.SetFloat(ShaderParams.GradientLength, m_GradientLength);

            if (m_PointColorPatternTexture != null)
            {
                materialPropertyBlock.SetFloat(ShaderParams.ColorOffset, -m_PixelColorPatternOffset);
            }

            if (m_OverlayTexture != null)
            {
                materialPropertyBlock.SetTexture(ShaderParams.OverlayTexture, m_OverlayTexture);
                materialPropertyBlock.SetVector(ShaderParams.OverlayTextureST, new Vector4(m_OverlayTileRepetitions.x, m_OverlayTileRepetitions.y, m_OverlayOffset.x, m_OverlayOffset.y));
                materialPropertyBlock.SetVector(ShaderParams.OverlayTextureSize, new Vector4(m_OverlayTexture.width, m_OverlayTexture.height));
            }
            else
            {
                materialPropertyBlock.SetTexture(ShaderParams.OverlayTexture, Texture2D.whiteTexture);
            }
        }

        private void RefreshMaterial()
        {
            if(m_meshRenderer == null)
            {
                return;
            }

            // It cannot continue without a camera
            if(CurrentCamera == null)
            {
                return;
            }

            bool updateMaterial = m_isLayoutDirty || m_isPositionsDirty || m_isGeometryDirty;

            if (m_isLayoutDirty || !Application.isPlaying)
            {
                m_isLayoutDirty = false;
                SendLayoutToGPU();
            }

            if (m_isGeometryDirty || !Application.isPlaying)
            {
                m_isGeometryDirty = false;
                GenerateGeometry();
            }
            
            if (m_isPositionsDirty || !Application.isPlaying)
            {
                m_isPositionsDirty = false;
                SendPointPositionsToGPU();

                if(m_AdaptiveGradientLength
#if UNITY_EDITOR
                    || !Application.isPlaying
#endif
                    )
                {
                    m_pixelLength = CalculatePixelLength(Points, PixelsPerUnit, ActualThickness);
                }

                if (m_AdaptiveGradientLength)
                {
                    // Gradient length updated in the material
                    m_GradientLength = m_pixelLength;
                    m_materialPropertyBlock.SetFloat(ShaderParams.GradientLength, m_GradientLength);
                }
            }

            if (updateMaterial)
            {
                RefreshQuads();
                m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);

#if UNITY_EDITOR

                SceneView.RepaintAll();

#endif
            }
        }

        private void GenerateGeometry()
        {
            int quadCount = m_Points.Count - 1;

            if(IsAmountOfPointsLimited && quadCount > MaximumAmountOfPoints)
            {
                quadCount = MaximumAmountOfPoints;
            }

            if (m_quadVertices == null || quadCount != m_quadVertices.Length / 4)
            {
                m_quadVertices = new Vector3[4 * quadCount];
                m_quadIndices = new int[6 * quadCount];
            }
        }

        /// <summary>
        /// Calculates the current pixel length of the line strip.
        /// </summary>
        /// <param name="linePoints">The points that define the endpoints of the segments.</param>
        /// <param name="pixelsPerUnit">The amount of pixels of the screen that fit into one world space unit. It depends on the orthogonal size of the camera.</param>
        /// <param name="thickness">The width, in actual screen pixels, of each "pixel" of the line.</param>
        /// <returns>The amount of line pixels required to drawn the entire line strip.</returns>
        protected static int CalculatePixelLength(List<Vector2> linePoints, float pixelsPerUnit, float thickness)
        {
            int pixelCount = 0;

            for (int i = 0; i < linePoints.Count - 1; ++i)
            {
                Vector2 segment = linePoints[i] - linePoints[i + 1];
                float segmentMaxAxisLength = Mathf.Max(Mathf.Abs(segment.x), Mathf.Abs(segment.y));
                float pixelsInSegment = segmentMaxAxisLength * pixelsPerUnit / thickness;
                pixelCount += Mathf.CeilToInt(pixelsInSegment);
            }

            return pixelCount;
        }

        private void OnCameraResolverCameraChanged()
        {
            if(gameObject.activeInHierarchy)
            {
                CurrentCamera = m_cameraResolver.GetCamera();
            }
        }

#if UNITY_EDITOR

        private Camera GetEditorCamera()
        {
            // Fills the camera to be used in editor
            if (m_editorCamera == null)
            {
                Camera[] sceneCameras = SceneView.GetAllSceneCameras();

                if (sceneCameras.Length > 0)
                {
                    m_editorCamera = sceneCameras[0];
                }
            }

            return m_editorCamera;
        }

        protected virtual void OnDrawGizmos()
        {
            Vector2 parentPosition = m_PositionsAreLocalSpace ? (Vector2)transform.position
                                                              : Vector2.zero;

            for (int i = 0; i < m_Points.Count; ++i)
            {
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.2f);
                Gizmos.DrawRay(m_Points[i] + parentPosition, Vector2.up);
                Gizmos.DrawRay(m_Points[i] + parentPosition, Vector2.right);

                if (i < m_Points.Count - 1)
                {
                    Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.2f);
                    Gizmos.DrawLine(m_Points[i] + parentPosition, m_Points[i + 1] + parentPosition);
                }
            }

            if(IsAmountOfPointsLimited)
            {
                if (m_packedPointsAsVectors == null)
                    return;

                int maxPackedPointsCount = Mathf.Min(m_packedPointsAsVectors.Length, m_packedPointsCount);

                for (int i = 0; i < maxPackedPointsCount; ++i)
                {
                    Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.2f);
                    Vector2 point = new Vector2(m_packedPointsAsVectors[i].x, m_packedPointsAsVectors[i].y);
                    Vector2 pointB = new Vector2(m_packedPointsAsVectors[i].z, m_packedPointsAsVectors[i].w);
                    Gizmos.DrawRay(point, Vector2.up);
                    Gizmos.DrawRay(point, Vector2.right);

                    if(i * 2 + 1 >= Points.Count)
                    {
                        break;
                    }

                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                    Gizmos.DrawLine(point, pointB);

                    Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.2f);
                    Gizmos.DrawRay(pointB, Vector2.up);
                    Gizmos.DrawRay(pointB, Vector2.right);
                }

                for (int i = 0; i < maxPackedPointsCount - 1; ++i)
                {
                    Vector2 point = new Vector2(m_packedPointsAsVectors[i].z, m_packedPointsAsVectors[i].w);
                    Vector2 pointB = new Vector2(m_packedPointsAsVectors[i + 1].x, m_packedPointsAsVectors[i + 1].y);

                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                    Gizmos.DrawLine(point, pointB);
                }
            }
            else
            {
                if(m_packedPoints == null)
                    return;

                for (int i = 0; i < m_packedPoints.Length; ++i)
                {
                    Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.2f);
                    Vector2 point = new Vector2(m_packedPoints[i].r, m_packedPoints[i].g);
                    Vector2 pointB = new Vector2(m_packedPoints[i].b, m_packedPoints[i].a);
                    Gizmos.DrawRay(point, Vector2.up);
                    Gizmos.DrawRay(point, Vector2.right);
                    
                    if (i < m_packedPoints.Length - 1)
                    {
                        Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                        Gizmos.DrawLine(point, pointB);

                        Gizmos.DrawRay(pointB, Vector2.up);
                        Gizmos.DrawRay(pointB, Vector2.right);
                    }
                }

                for (int i = 0; i < m_packedPoints.Length - 1; ++i)
                {
                    Vector2 point = new Vector2(m_packedPoints[i].b, m_packedPoints[i].a);
                    Vector2 pointB = new Vector2(m_packedPoints[i + 1].r, m_packedPoints[i + 1].g);

                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                    Gizmos.DrawLine(point, pointB);
                }
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 offset = transform.position;
            Color wireColor = new Color(1.0f, 1.0f, 0.0f, 0.1f);
            
            for (int i = 0; i < m_quadVertices.Length / 4; ++i)
            {
                // Note: Z stores the line index
                Debug.DrawLine((Vector2)(m_quadVertices[0 + i * 4] + offset), (Vector2)(m_quadVertices[1 + i * 4] + offset), wireColor);
                Debug.DrawLine((Vector2)(m_quadVertices[1 + i * 4] + offset), (Vector2)(m_quadVertices[2 + i * 4] + offset), wireColor);
                Debug.DrawLine((Vector2)(m_quadVertices[2 + i * 4] + offset), (Vector2)(m_quadVertices[3 + i * 4] + offset), wireColor);
                Debug.DrawLine((Vector2)(m_quadVertices[3 + i * 4] + offset), (Vector2)(m_quadVertices[0 + i * 4] + offset), wireColor);
            }
        }

        [CustomEditor(typeof(LineRenderer2D)), CanEditMultipleObjects]
        protected class LineRenderer2DEditor : UnityEditor.Editor
        {
            private static GUIContent[] m_amountEnumTexts = new GUIContent[] { new GUIContent("2"), new GUIContent("32"), new GUIContent("128"), new GUIContent("Unlimited") };

            private static class Texts
            {
                public static GUIContent PixelsPerUnit = new GUIContent("Pixels per unit: ", "The detected pixels per Unity spacial unit.");
                public static GUIContent ActualThickness = new GUIContent("Actual thickness: ", "The width of the line in actual screen pixels.");
                public static GUIContent GlobalThicknessMultiplier = new GUIContent("Global thickness multiplier: ", "A factor that scales the thickness of all existing lines. This property is not serialized.");
                public static GUIContent Global = new GUIContent("Global");
                public static GUIContent PointsCount = new GUIContent("Points count: ", "The actual amount of points in the line.");
                public static GUIContent PackedPointsCount = new GUIContent("Packed points count: ", "The amount of packages that contain 2 points each.");
                public static GUIContent TriangleCount = new GUIContent("Triangle count: ", "The amount of triangles being rendered.");
                public static GUIContent AffectsAllOffsets = new GUIContent("Affects all offsets: ", "When enabled, the start point will be added to all other offset parameters.");
                public static GUIContent PointCountWarning = new GUIContent("The amount of points must not exceed the maximum when using a fixed size line.", ".");
                public static GUIContent Rendering = new GUIContent("Rendering");
                public static GUIContent LineMorphology = new GUIContent("Line morphology");
                public static GUIContent LinePointColors = new GUIContent("Line point colors");
                public static GUIContent DottedLine = new GUIContent("Dotted line");
                public static GUIContent ColorGradient = new GUIContent("Color gradient");
                public static GUIContent LineBounds = new GUIContent("Line bounds");
                public static GUIContent OverlayTexture = new GUIContent("Overlay texture");
                public static GUIContent Info = new GUIContent("Info");
                public static GUIContent PixelLength = new GUIContent("Pixel length: ", "The amount of pixels required to draw the line strip (approximately). Updated only if the adaptive gradient length is enabled.");
            }

            protected SerializedProperty m_points;
            protected SerializedProperty m_lineThickness;
            protected SerializedProperty m_lineColor;
            protected SerializedProperty m_backgroundColor;
            protected SerializedProperty m_positionsAreLocalSpace;
            protected SerializedProperty m_autoApplyPositionChanges;
            protected SerializedProperty m_camera;
            protected SerializedProperty m_dotOffset;
            protected SerializedProperty m_dotLength;
            protected SerializedProperty m_dotSpaceLength;
            protected SerializedProperty m_gradientStartColor;
            protected SerializedProperty m_gradientEndColor;
            protected SerializedProperty m_gradientLength;
            protected SerializedProperty m_gradientOffset;
            protected SerializedProperty m_maximumLength;
            protected SerializedProperty m_startPoint;
            protected SerializedProperty m_startPointAffectsAllOffsets;
            protected SerializedProperty m_colorOffset;
            protected SerializedProperty m_pointColorPattern;
            protected SerializedProperty m_pointColorPatternTexture;
            protected SerializedProperty m_quadWidthFactor;
            protected SerializedProperty m_overlayTexture;
            protected SerializedProperty m_overlayOffset;
            protected SerializedProperty m_overlayTileRepetitions;
            protected SerializedProperty m_referenceOrthographicSize;
            protected SerializedProperty m_cameraTag;
            protected SerializedProperty m_canAddPoints;
            protected SerializedProperty m_adaptiveGradientLength;

            protected virtual void OnEnable()
            {
                m_points = serializedObject.FindProperty(nameof(LineRenderer2D.m_Points));
                m_lineThickness = serializedObject.FindProperty(nameof(LineRenderer2D.m_Thickness));
                m_lineColor = serializedObject.FindProperty(nameof(LineRenderer2D.m_MainColor));
                m_backgroundColor = serializedObject.FindProperty(nameof(LineRenderer2D.m_BackgroundColor));
                m_positionsAreLocalSpace = serializedObject.FindProperty(nameof(LineRenderer2D.m_PositionsAreLocalSpace));
                m_autoApplyPositionChanges = serializedObject.FindProperty(nameof(LineRenderer2D.m_AutoApplyPositionChanges));
                m_camera = serializedObject.FindProperty(nameof(LineRenderer2D.m_Camera));
                m_dotOffset = serializedObject.FindProperty(nameof(LineRenderer2D.m_DotOffset));
                m_dotLength = serializedObject.FindProperty(nameof(LineRenderer2D.m_DotLength));
                m_dotSpaceLength = serializedObject.FindProperty(nameof(LineRenderer2D.m_DotSpaceLength));
                m_gradientStartColor = serializedObject.FindProperty(nameof(LineRenderer2D.m_GradientStartColor));
                m_gradientEndColor = serializedObject.FindProperty(nameof(LineRenderer2D.m_GradientEndColor));
                m_gradientLength = serializedObject.FindProperty(nameof(LineRenderer2D.m_GradientLength));
                m_gradientOffset = serializedObject.FindProperty(nameof(LineRenderer2D.m_GradientOffset));
                m_maximumLength = serializedObject.FindProperty(nameof(LineRenderer2D.m_MaximumLength));
                m_startPoint = serializedObject.FindProperty(nameof(LineRenderer2D.m_StartPoint));
                m_startPointAffectsAllOffsets = serializedObject.FindProperty(nameof(LineRenderer2D.m_StartPointAffectsAllOffsets));
                m_colorOffset = serializedObject.FindProperty(nameof(LineRenderer2D.m_PixelColorPatternOffset));
                m_pointColorPattern = serializedObject.FindProperty(nameof(LineRenderer2D.m_PixelColorPattern));
                m_pointColorPatternTexture = serializedObject.FindProperty(nameof(LineRenderer2D.m_PointColorPatternTexture));
                m_quadWidthFactor = serializedObject.FindProperty(nameof(LineRenderer2D.m_QuadWidthFactor));
                m_overlayTexture = serializedObject.FindProperty(nameof(LineRenderer2D.m_OverlayTexture));
                m_overlayOffset = serializedObject.FindProperty(nameof(LineRenderer2D.m_OverlayOffset));
                m_overlayTileRepetitions = serializedObject.FindProperty(nameof(LineRenderer2D.m_OverlayTileRepetitions));
                m_referenceOrthographicSize = serializedObject.FindProperty(nameof(LineRenderer2D.m_ReferenceOrthographicSize));
                m_cameraTag = serializedObject.FindProperty(nameof(LineRenderer2D.m_CameraTag));
                m_canAddPoints = serializedObject.FindProperty(nameof(LineRenderer2D.m_CanAddPoints));
                m_adaptiveGradientLength = serializedObject.FindProperty(nameof(LineRenderer2D.m_AdaptiveGradientLength));
            }

            public override void OnInspectorGUI()
            {
                LineRenderer2D lineRenderer = target as LineRenderer2D;

                EditorGUILayout.BeginVertical();
                {
                    bool hasChanges = false;

                    EditorGUILayout.LabelField(Texts.Rendering, EditorStyles.boldLabel);
                    DrawInspectorLineSeparator();

                    EditorGUI.BeginChangeCheck();
                    {
                        // Camera tag
                        EditorGUILayout.PropertyField(m_cameraTag);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Camera
                        EditorGUILayout.PropertyField(m_camera);
                        // Reference orthogonal size
                        EditorGUILayout.PropertyField(m_referenceOrthographicSize);
                        // Quad width factor
                        EditorGUILayout.PropertyField(m_quadWidthFactor);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyGeometryChanges();
                        serializedObject.ApplyModifiedProperties();
                        hasChanges = true;
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Texts.Global, EditorStyles.boldLabel);
                    DrawInspectorLineSeparator();

                    // Global thickness multiplier
                    LineRenderer2D.GlobalThicknessMultiplier = EditorGUILayout.FloatField(Texts.GlobalThicknessMultiplier, LineRenderer2D.GlobalThicknessMultiplier);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Texts.LineMorphology, EditorStyles.boldLabel);
                    DrawInspectorLineSeparator();

                    // Can add points
                    EditorGUI.BeginChangeCheck();
                    {
                        if((target as LineRenderer2D).IsAmountOfPointsLimited)
                        {
                            EditorGUI.BeginDisabledGroup(Application.isPlaying);
                            {
                                EditorGUILayout.PropertyField(m_canAddPoints);
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Points
                        EditorGUILayout.PropertyField(m_points);

                        if((target as LineRenderer2D).IsAmountOfPointsLimited && lineRenderer.m_Points.Count > lineRenderer.MaximumAmountOfPoints)
                        { 
                            EditorGUILayout.HelpBox(Texts.PointCountWarning.text, MessageType.Error, true);
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyLayoutChanges();
                        lineRenderer.ApplyGeometryChanges();
                        lineRenderer.ApplyPointPositionChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Positions are local space
                        EditorGUILayout.PropertyField(m_positionsAreLocalSpace);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyPointPositionChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Auto apply position changes
                        EditorGUILayout.PropertyField(m_autoApplyPositionChanges);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyPointPositionChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Line thickness
                        EditorGUILayout.PropertyField(m_lineThickness);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyGeometryChanges();
                        lineRenderer.ApplyPropertyChanges();
                        hasChanges = true;
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Texts.LinePointColors, EditorStyles.boldLabel);
                    DrawInspectorLineSeparator();

                    EditorGUI.BeginChangeCheck();
                    {
                        // Line color
                        EditorGUILayout.PropertyField(m_lineColor);
                        // Background color
                        EditorGUILayout.PropertyField(m_backgroundColor);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyPropertyChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Point color pattern
                        EditorGUILayout.PropertyField(m_pointColorPattern);
                        // Point color pattern texture
                        EditorGUILayout.PropertyField(m_pointColorPatternTexture);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyColorPatternChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        // Color offset
                        EditorGUILayout.PropertyField(m_colorOffset);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyPropertyChanges();
                        hasChanges = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    {
                        DrawProperties();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        lineRenderer.ApplyPropertyChanges();
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        serializedObject.ApplyModifiedProperties();

                        lineRenderer.AdaptToCameraOrthogonalSize(isInitialization: true);
                        lineRenderer.OnWillRenderObject();
                        lineRenderer.RefreshMaterial();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            protected virtual void DrawProperties()
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Texts.DottedLine, EditorStyles.boldLabel);
                DrawInspectorLineSeparator();
                // Dot offset
                EditorGUILayout.PropertyField(m_dotOffset);
                // Dot length
                EditorGUILayout.PropertyField(m_dotLength);
                // Dot space length
                EditorGUILayout.PropertyField(m_dotSpaceLength);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Texts.ColorGradient, EditorStyles.boldLabel);
                DrawInspectorLineSeparator();
                // Gradient start color
                EditorGUILayout.PropertyField(m_gradientStartColor);
                // Gradient start end
                EditorGUILayout.PropertyField(m_gradientEndColor);
                // Adaptative gradient length
                EditorGUILayout.PropertyField(m_adaptiveGradientLength);

                EditorGUI.BeginDisabledGroup(m_adaptiveGradientLength.boolValue);
                {
                    // Gradient length
                    EditorGUILayout.PropertyField(m_gradientLength);
                }
                EditorGUI.EndDisabledGroup();
                
                // Gradient offset
                EditorGUILayout.PropertyField(m_gradientOffset);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Texts.LineBounds, EditorStyles.boldLabel);
                DrawInspectorLineSeparator();
                // Start point
                EditorGUILayout.PropertyField(m_startPoint);
                // Start point affects all offsets
                EditorGUILayout.PropertyField(m_startPointAffectsAllOffsets, Texts.AffectsAllOffsets);
                // Maximum length
                EditorGUILayout.PropertyField(m_maximumLength);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Texts.OverlayTexture, EditorStyles.boldLabel);
                DrawInspectorLineSeparator();
                // Overlay texture
                EditorGUILayout.PropertyField(m_overlayTexture);
                // Overlay texture offset
                EditorGUILayout.PropertyField(m_overlayOffset);
                // Overlay texture tile repetitions
                EditorGUILayout.PropertyField(m_overlayTileRepetitions);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Texts.Info, EditorStyles.boldLabel);
                DrawInspectorLineSeparator();
                // Pixels per unit
                LineRenderer2D targetLine = target as LineRenderer2D;
                EditorGUILayout.LabelField(new GUIContent(Texts.PixelsPerUnit.text + targetLine.m_pixelsPerUnit, Texts.PixelsPerUnit.tooltip));
                // Actual thickness
                EditorGUILayout.LabelField(new GUIContent(Texts.ActualThickness.text + targetLine.ActualThickness, Texts.ActualThickness.tooltip));
                // Points count
                EditorGUILayout.LabelField(new GUIContent(Texts.PointsCount.text + targetLine.m_Points.Count, Texts.PointsCount.tooltip));
                // Packed points count
                EditorGUILayout.LabelField(new GUIContent(Texts.PackedPointsCount.text + targetLine.m_packedPointsCount, Texts.PackedPointsCount.tooltip));
                // Triangle count
                if(targetLine.m_quadVertices != null)
                { 
                    EditorGUILayout.LabelField(new GUIContent(Texts.TriangleCount.text + (targetLine.m_quadVertices.Length / 2), Texts.TriangleCount.tooltip));
                }
                EditorGUILayout.LabelField(new GUIContent(Texts.PixelLength.text + targetLine.m_pixelLength, Texts.PixelLength.tooltip));
            }

            protected void DrawInspectorLineSeparator()
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                rect.height = 1;
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            }

            protected void OnSceneGUI()
            {
                LineRenderer2D lineRenderer = target as LineRenderer2D;
                float handleSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.1f;
                Vector2 point;
                
                for (int i = 0; i < m_points.arraySize; ++i)
                {
                    Vector2 parentPosition = m_positionsAreLocalSpace.boolValue ? (Vector2)lineRenderer.transform.position
                                                                                : Vector2.zero;
                    EditorGUI.BeginChangeCheck();
                    {
                        point = (Vector2)Handles.PositionHandle(m_points.GetArrayElementAtIndex(i).vector2Value + parentPosition, Quaternion.identity);
                        Handles.Label(point + Vector2.down * handleSize, i.ToString());
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Line point moved");

                        lineRenderer.m_Points[i] = point - parentPosition;
                        (target as LineRenderer2D).ApplyPointPositionChanges();
                    }
                }
            }
        }

#endif

    }
}