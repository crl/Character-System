using UnityEditor;

[InitializeOnLoad]
public class SwimmingMPEditorSymbol : Editor
{
    /// <summary>
    /// Symbol that will be added to the editor
    /// </summary>
    private static string _EditorSymbol = "USE_SWIMMING_MP";

    /// <summary>
    /// Add a new symbol as soon as Unity gets done compiling.
    /// </summary>
    static SwimmingMPEditorSymbol()
    {
        string lSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        if (!lSymbols.Contains(_EditorSymbol))
        {
            lSymbols += (";" + _EditorSymbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, lSymbols);
        }
    }
}
