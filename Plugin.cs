using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
namespace SprocketJsonEditor;
[BepInPlugin("sprocket.jsoneditor", "Sprocket JSON Editor", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    internal static int MainThread;
    internal static ManualLogSource ModLog=null!;
    public override void Load(){MainThread=Environment.CurrentManagedThreadId;ModLog=Log;AddComponent<JsonEditorInputDriver>();}
}

