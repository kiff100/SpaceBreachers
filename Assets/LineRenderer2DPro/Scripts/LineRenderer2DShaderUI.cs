// Copyright 2024 Alejandro Villalba Avila

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace SiliconHeart.Rendering
{
    internal class LineRenderer2DShaderUI : ShaderGUI
    {
        private static class Texts
        {
            public static GUIContent MaximumAmountOfPoints = new GUIContent("Maximum Amount of Points", "The maximum amount of points the renderer can draw. The number of points can vary but it cannot be greater than this value. In order to get the best performance, the lowest value should be used. It is possible to choose a capacity of 2, 32, 128 or unlimited, which means that there is no maximum (this is the slowest option). It is not possible to change this parameter at runtime.");
            public static GUIContent MaskInteraction = new GUIContent("Mask Interaction", "LineRenderer2D's interaction with a Sprite Mask.");
        }
            
        private static GUIContent[] m_amountEnumTexts = new GUIContent[] { new GUIContent("2"), new GUIContent("32"), new GUIContent("128"), new GUIContent("Unlimited") };

        private static readonly string[] MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS = new string[]{ "MAXIMUM_AMOUNT_OF_POINTS__2",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__32",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__128",
                                                                                           "MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED" };
        private static GUIContent[] m_maskInteractionEnumTexts = new GUIContent[] { new GUIContent(nameof(SpriteMaskInteraction.None)), new GUIContent(nameof(SpriteMaskInteraction.VisibleInsideMask)), new GUIContent(nameof(SpriteMaskInteraction.VisibleOutsideMask)) };
        private static int[] m_maskInteractionEnumValues = new int[] { 0, 1, 2 };
        private static readonly Tuple<float, float, float>[] MASK_INTERACTION_VALUES = new Tuple<float, float, float>[]{ new Tuple<float, float, float>(1.0f, (float)UnityEngine.Rendering.CompareFunction.Always, (float)UnityEngine.Rendering.StencilOp.Replace), // None
                                                                                                                         new Tuple<float, float, float>(1.0f, (float)UnityEngine.Rendering.CompareFunction.Equal, (float)UnityEngine.Rendering.StencilOp.Keep), // Visible inside
                                                                                                                         new Tuple<float, float, float>(1.0f, (float)UnityEngine.Rendering.CompareFunction.NotEqual, (float)UnityEngine.Rendering.StencilOp.Keep)}; // Visible outside
        
        private static int[] MAXIMUM_AMOUNT_OF_POINTS_VALUES = new int[] { 2, 32, 128, -1 };

        private int m_currentMaximumAmount = int.MinValue;
        private int m_currentMaskInteraction = int.MinValue;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = (Material)materialEditor.target;

            if (m_currentMaximumAmount == int.MinValue)
            {
                m_currentMaximumAmount = GetSelectedAmountFromKeywords(material);
            }

            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                {
                    // Maximum amount of points
                    m_currentMaximumAmount = EditorGUILayout.IntPopup(Texts.MaximumAmountOfPoints, m_currentMaximumAmount, m_amountEnumTexts, MAXIMUM_AMOUNT_OF_POINTS_VALUES);
                }
                EditorGUI.EndDisabledGroup();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EnableKeywordFromSelectedAmount(material);
            }

            if (m_currentMaskInteraction == int.MinValue)
            {
                m_currentMaskInteraction = GetSelectedMaskInteractionFromMaterial(material);
            }
            
            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                {
                    // Mask interaction (none, visible inside, visible outside)
                    m_currentMaskInteraction = EditorGUILayout.IntPopup(Texts.MaskInteraction, m_currentMaskInteraction, m_maskInteractionEnumTexts, m_maskInteractionEnumValues);
                }
                EditorGUI.EndDisabledGroup();
            }
            if (EditorGUI.EndChangeCheck())
            {
                SetStencilParamsFromMaskInteraction(material, m_currentMaskInteraction);
            }
            
            base.OnGUI(materialEditor, properties);
        }

        private void SetStencilParamsFromMaskInteraction(Material material, int maskInteraction)
        {
            material.SetFloat("_StencilRef", MASK_INTERACTION_VALUES[maskInteraction].Item1);
            material.SetFloat("_StencilComp", MASK_INTERACTION_VALUES[maskInteraction].Item2);
            material.SetFloat("_StencilPass", MASK_INTERACTION_VALUES[maskInteraction].Item3);
        }

        private int GetSelectedMaskInteractionFromMaterial(Material material)
        {
            int stencilComp = (int)material.GetFloat("_StencilComp");

            if (stencilComp == (int)UnityEngine.Rendering.CompareFunction.Always)
            {
                return 0; // None
            }
            else if (stencilComp == (int)UnityEngine.Rendering.CompareFunction.Equal)
            {
                return 1; // Visible inside
            }
            else if (stencilComp == (int)UnityEngine.Rendering.CompareFunction.NotEqual)
            {
                return 2; // Visible outside
            }

            return -1;
        }
        
        private int GetSelectedAmountFromKeywords(Material lineMaterial)
        {
            for (int i = 0; i < MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS.Length; ++i)
            {
                if (lineMaterial.IsKeywordEnabled(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[i]))
                {
                    return MAXIMUM_AMOUNT_OF_POINTS_VALUES[i];
                }
            }

            // If no keyword is enabled, it enables the first by default
            lineMaterial.EnableKeyword(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[0]);

            return MAXIMUM_AMOUNT_OF_POINTS_VALUES[0];
        }

        private void EnableKeywordFromSelectedAmount(Material lineMaterial)
        {
            for (int i = 0; i < MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS.Length; ++i)
            {
                lineMaterial.DisableKeyword(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[i]);
            }

            for (int i = 0; i < MAXIMUM_AMOUNT_OF_POINTS_VALUES.Length; ++i)
            {
                if (MAXIMUM_AMOUNT_OF_POINTS_VALUES[i] == m_currentMaximumAmount)
                {
                    lineMaterial.EnableKeyword(MAXIMUM_AMOUNT_OF_POINTS_KEYWORDS[i]);
                    break;
                }
            }
        }
    }
}

#endif