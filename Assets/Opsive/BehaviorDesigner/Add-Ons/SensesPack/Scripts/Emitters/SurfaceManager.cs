/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Manages surface types and textures in the game world, providing functionality to identify and query surface properties.
    /// </summary>
    public class SurfaceManager : MonoBehaviour
    {
        private static SurfaceManager s_Instance;
        private static SurfaceManager Instance {
            get {
                if (s_Instance == null) {
                    s_Instance = new GameObject("SurfaceManager").AddComponent<SurfaceManager>();
                    s_MaskID = Shader.PropertyToID("_Mask");
                    s_SecondaryTextureID = Shader.PropertyToID("_MainTex2");
                }
                return s_Instance;
            }
        }
        private static int s_MaskID;
        private static int s_SecondaryTextureID;

        /// <summary>
        /// Represets a default surface listed within the SurfaceManager.
        /// </summary>
        [System.Serializable]
        public struct ObjectSurface
        {
            [Tooltip("The type of surface represented.")]
            [SerializeField] private SurfaceType m_SurfaceType;
            [Tooltip("The textures which go along with the specified SurfaceType.")]
            [SerializeField] private Texture[] m_Textures;

            public SurfaceType SurfaceType { get { return m_SurfaceType; } set { m_SurfaceType = value; } }
            public Texture[] Textures { get { return m_Textures; } set { m_Textures = value; } }
        }

        [Tooltip("An array of SurfaceTypes which are paired to a UV position within a texture.")]
        [SerializeField] protected ObjectSurface[] m_ObjectSurfaces;
        [Tooltip("The name of the main texture property that should be retrieved.")]
        [SerializeField] protected string m_MainTexturePropertyName = "_BaseMap";
        [Tooltip("The fallback SurfaceType if no SurfaceTypes can be found.")]
        [SerializeField] protected SurfaceType m_FallbackSurfaceType;

        private int m_MainTexturePropertyID;
        private Dictionary<Texture, ObjectSurface> m_TextureObjectSurfaceMap = new Dictionary<Texture, ObjectSurface>();
        private Dictionary<Texture, SurfaceType> m_TextureSurfaceTypeMap = new Dictionary<Texture, SurfaceType>();

        private Dictionary<GameObject, SurfaceIdentifier> m_GameObjectSurfaceIdentifiersMap = new Dictionary<GameObject, SurfaceIdentifier>();
        private Dictionary<GameObject, SurfaceType> m_GameObjectSurfacesTypesMap = new Dictionary<GameObject, SurfaceType>();
        private Dictionary<GameObject, bool> m_GameObjectComplexMaterialsMap = new Dictionary<GameObject, bool>();
        private Dictionary<GameObject, Renderer> m_GameObjectRendererMap = new Dictionary<GameObject, Renderer>();
        private Dictionary<GameObject, Texture> m_GameObjectMainTextureMap = new Dictionary<GameObject, Texture>();

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            // PropertyToID cannot be initialized within a MonoBehaviour constructor.
            m_MainTexturePropertyID = Shader.PropertyToID(m_MainTexturePropertyName);

            InitObjectSurfaces();

            s_MaskID = Shader.PropertyToID("_Mask");
            s_SecondaryTextureID = Shader.PropertyToID("_MainTex2");
        }

        /// <summary>
        /// The object has been enabled.
        /// </summary>
        private void OnEnable()
        {
            s_Instance = this;
        }

        /// <summary>
        /// Stores all the textures added as an object surface to the TextureObjectSurfaceMap dictionary (if using the default UV) or
        /// the UVTextureObjectSurfaceMap dictionary (if using any other UV).
        /// </summary>
        protected void InitObjectSurfaces()
        {
            if (m_ObjectSurfaces == null) {
                return;
            }

            for (int i = 0; i < m_ObjectSurfaces.Length; ++i) {
                for (int j = 0; j < m_ObjectSurfaces[i].Textures.Length; ++j) {
                    if (m_ObjectSurfaces[i].Textures[j] == null) {
                        continue;
                    }

                    if (m_TextureSurfaceTypeMap.ContainsKey(m_ObjectSurfaces[i].Textures[j])) {
                        continue;
                    }

                    m_TextureSurfaceTypeMap.Add(m_ObjectSurfaces[i].Textures[j], m_ObjectSurfaces[i].SurfaceType);
                }
            }
        }

        public static SurfaceType GetSurfaceType(GameObject target)
        {
            return Instance.GetSurfaceTypeInternal(target);
        }

        /// <summary>
        /// Returns the SurfaceType based on the RaycastHit.
        /// </summary>
        /// <param name="hit">The RaycastHit which caused the SurfaceEffect to spawn.</param>
        /// <param name="hitCollider">The collider of the object that was hit.</param>
        /// <returns>The SurfaceType based on the RaycastHit. Can be null.</returns>
        private SurfaceType GetSurfaceTypeInternal(GameObject target)
        {
            if (target == null) {
                return null;
            }

            // The SurfaceType on the SurfaceIdentifier can provide a unique SurfaceType for that collider. Therefore it should be tested first.
            var surfaceIdentifier = GetSurfaceIdentifier(target);
            if (surfaceIdentifier != null) {
                if (surfaceIdentifier.SurfaceType != null) {
                    return surfaceIdentifier.SurfaceType;
                }
            }

            SurfaceType surfaceType;
            if (!m_GameObjectSurfacesTypesMap.TryGetValue(target, out surfaceType)) {
                var texture = GetMainTexture(target);
                if (texture != null) {
                    surfaceType = GetSurfaceType(texture);
                }

                m_GameObjectSurfacesTypesMap.Add(target, surfaceType);
            }

            return surfaceType;
        }

        /// <summary>
        /// Returns the SurfaceIdentifier for the specified collider.
        /// </summary>
        /// <param name="hitCollider">The collider to retrieve the SurfaceIdentifier of.</param>
        /// <returns>The SurfaceIdentifier for the specified collider. Can be null.</returns>
        private SurfaceIdentifier GetSurfaceIdentifier(GameObject target)
        {
            SurfaceIdentifier surfaceIdentifier;
            if (!m_GameObjectSurfaceIdentifiersMap.TryGetValue(target, out surfaceIdentifier)) {
                // Try to find a SurfaceIdentifier on the GameObject.
                surfaceIdentifier = target.GetComponent<SurfaceIdentifier>();
                // If there is no SurfaceIdentifier on the GameObject then try to find a SurfaceIdentifier withinin the children or parent.
                if (surfaceIdentifier == null) {
                    surfaceIdentifier = target.GetComponentInChildren<SurfaceIdentifier>();

                    if (surfaceIdentifier == null) {
                        surfaceIdentifier = target.GetComponentInParent<SurfaceIdentifier>();
                    }
                }

                m_GameObjectSurfaceIdentifiersMap.Add(target, surfaceIdentifier);
            }

            return surfaceIdentifier;
        }

        /// <summary>
        /// Returns the texture of the specified collider.
        /// </summary>
        /// <param name="hitCollider">The collider to get the texture of.</param>
        /// <returns>The texture of the specified collider. Can be null.</returns>
        private Texture GetMainTexture(GameObject target)
        {
            if (target == null) {
                return null;
            }

            Texture texture;
            if (!m_GameObjectMainTextureMap.TryGetValue(target, out texture)) {
                // The texture is retrieved from the renderer.
                var hitRenderer = GetRenderer(target);
                if (hitRenderer != null && hitRenderer.sharedMaterial != null && hitRenderer.sharedMaterial.HasProperty(m_MainTexturePropertyID)) {
                    texture = hitRenderer.sharedMaterial.GetTexture(m_MainTexturePropertyID);
                }

                m_GameObjectMainTextureMap.Add(target, texture);
            }

            return texture;
        }

        /// <summary>
        /// Returns the main renderer of the specified collider.
        /// </summary>
        /// <param name="hitCollider">The collider to get the renderer of.</param>
        /// <returns>The main Renderer of the specified collider. Can be null.</returns>
        private Renderer GetRenderer(GameObject target)
        {
            if (target == null) {
                return null;
            }

            Renderer hitRenderer;
            if (!m_GameObjectRendererMap.TryGetValue(target, out hitRenderer)) {
                // Try to get a renderer on the collider's GameObject.
                hitRenderer = target.GetComponent<Renderer>();

                // If no renderer exists, try to find a renderer in the collider's children.
                if (hitRenderer == null || !hitRenderer.enabled || hitRenderer is SkinnedMeshRenderer) {
                    var childRenderers = target.GetComponentsInChildren<Renderer>();
                    for (int i = 0; i < childRenderers.Length; ++i) {
                        if (childRenderers[i] == hitRenderer || !childRenderers[i].enabled || hitRenderer is SkinnedMeshRenderer) {
                            continue;
                        }
                        hitRenderer = childRenderers[i];
                        break;
                    }
                }

                // If no renderer exists, try to find a renderer in the collider's parent.
                if (hitRenderer == null || !hitRenderer.enabled || hitRenderer is SkinnedMeshRenderer) {
                    var parentRenderers = target.GetComponentsInParent<Renderer>();
                    for (int i = 0; i < parentRenderers.Length; ++i) {
                        if (parentRenderers[i] == hitRenderer || !parentRenderers[i].enabled || hitRenderer is SkinnedMeshRenderer) {
                            continue;
                        }
                        hitRenderer = parentRenderers[i];
                        break;
                    }
                }

                // SkinnedMeshRenderers can not have their triangles fetched.
                if (hitRenderer != null && (!hitRenderer.enabled || hitRenderer is SkinnedMeshRenderer)) {
                    hitRenderer = null;
                }

                m_GameObjectRendererMap.Add(target, hitRenderer);
            }

            return hitRenderer;
        }

        /// <summary>
        /// Returns the SurfaceType for the specified texture.
        /// </summary>
        /// <param name="texture">The texture to get the surface type of.</param>
        /// <returns>The SurfaceType for the specified texture. Can be null.</returns>
        private SurfaceType GetSurfaceType(Texture texture)
        {
            if (texture == null) {
                return null;
            }

            SurfaceType surfaceType;
            if (!m_TextureSurfaceTypeMap.TryGetValue(texture, out surfaceType)) {
                ObjectSurface objectSurface;
                if (m_TextureObjectSurfaceMap.TryGetValue(texture, out objectSurface)) {
                    surfaceType = objectSurface.SurfaceType;
                }
                m_TextureSurfaceTypeMap.Add(texture, surfaceType);
            }

            return surfaceType;
        }

        /// <summary>
        /// The object has been disabled.
        /// </summary>
        private void OnDisable()
        {
            s_Instance = null;
        }

        /// <summary>
        /// Reset the static variables for domain reloading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReset()
        {
            s_Instance = null;
        }
    }
}