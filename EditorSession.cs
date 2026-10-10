using System.Text.Json.Nodes;
namespace SprocketJsonEditor;

/// <summary>Mods own storage, validation and refresh; the framework owns UI and input lifetime.</summary>
public abstract class EditorSession
{
    public abstract string Title { get; }
    public virtual string ItemName => "item";
    public abstract JsonArray Items { get; }
    public abstract JsonArray Templates { get; }
    public abstract IEnumerable<string> Fields(int index);
    public abstract JsonNode Value(int index,string field);
    public abstract string Caption(int index,string field);
    public virtual IEnumerable<string> Groups(int index)=>Array.Empty<string>();
    public virtual string FieldGroup(int index,string field)=>"";
    public virtual string? GroupDescription(int index,string group)=>null;
    public virtual EditorNumberRange? NumberRange(int index,string field)=>null;
    public virtual string[]? Choices(string field)=>null;
    public virtual string ChoiceLabel(string field,string value)=>value;
    public abstract void Set(int index,string field,string value);
    public virtual void SetChoice(int index,string field,string value)=>Set(index,field,value);
    public abstract int Duplicate(JsonObject template);
    public abstract void Save();
}


public static class JsonEditor
{
    public static bool IsOpen=>RuntimeJsonEditor.IsOpen;
    public static void Open(EditorSession session){if(Environment.CurrentManagedThreadId!=Plugin.MainThread)throw new InvalidOperationException("Open the JSON editor on the game main thread.");if(IsOpen)throw new InvalidOperationException("Close the active editor before opening another.");RuntimeJsonEditor.Open(session??throw new ArgumentNullException(nameof(session)));}
    public static void Close()=>RuntimeJsonEditor.Close();
}

