using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public class ActorControllerEditorSymbol : Editor
{
    /// <summary>
    /// Symbol that will be added to the editor
    /// </summary>
    private static string _EditorSymbol = "OOTII_AC";

    /// <summary>
    /// Add a new symbol as soon as Unity gets done compiling.
    /// </summary>
    static ActorControllerEditorSymbol()
    {
        string lSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        if (!lSymbols.Split(';').Contains(_EditorSymbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, lSymbols + ";" + _EditorSymbol);
        }
    }
}
