/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Editor
{
    using UnityEditor;
#if UNITY_2023_1_OR_NEWER
    using UnityEditor.Build; 
#endif

    /// <summary>
    /// Editor script which will define the Movement Pack compiler symbol so other components are aware of the asset import status.
    /// </summary>
    [InitializeOnLoad]
    public class DefineCompilerSymbol
    {
#if !BEHAVIOR_DESIGNER_MOVEMENT_PACK
        private static string s_Symbol = "BEHAVIOR_DESIGNER_MOVEMENT_PACK";
#endif

        /// <summary>
        /// If the specified classes exist then the compiler symbol should be defined, otherwise the symbol should be removed.
        /// </summary>
        static DefineCompilerSymbol()
        {
#if !BEHAVIOR_DESIGNER_MOVEMENT_PACK
            AddSymbol(s_Symbol);
#endif
        }

        /// <summary>
        /// Adds the specified symbol to the compiler definitions.
        /// </summary>
        /// <param name="symbol">The symbol to add.</param>
        private static void AddSymbol(string symbol)
        {
#if UNITY_2023_1_OR_NEWER
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            if (symbols.Contains(symbol)) {
                return;
            }
            symbols += (";" + symbol);
#if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
#endif
        }
    }
}