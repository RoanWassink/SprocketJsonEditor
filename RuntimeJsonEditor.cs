using BepInEx;
using Il2CppInterop.Runtime;
using System.Text.Json.Nodes;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SprocketJsonEditor;

// Shared presentation. Providers own validation, persistence and runtime refresh.
internal static class RuntimeJsonEditor
{
    private static GameObject? panel;
    internal static bool IsOpen => panel != null && panel.activeInHierarchy;
    private static RectTransform? content;
    private static TMP_FontAsset? font;
    private static TMP_Text? status;
    private static EditorSession? session;

    private static int expanded = -1;
    private static readonly Dictionary<string, string> errors = new();
    private static readonly HashSet<string> openGroups = new();

    private static RectTransform Rect(string name, Transform parent, float height = 0)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>(); rect.SetParent(parent, false); rect.localScale = Vector3.one;
        if(height > 0) { var size = go.AddComponent<LayoutElement>(); size.preferredHeight = height; size.minHeight = height; }
        return rect;
    }
    private static TMP_Text Text(RectTransform parent, string value, float size = 18)
    {
        var r = Rect("Label", parent); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(8, 0); r.offsetMax = new Vector2(-8, 0);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size;
        t.color = Color.white; t.richText = false; t.raycastTarget = false; t.alignment = TextAlignmentOptions.MidlineLeft;
        return t;
    }
    private static void Button(Transform parent, string label, Action action)
    {
        var r = Rect(label, parent, 36); var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.20f, .18f, .14f, 1);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        b.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>((Action)(() => Safe(action)))); Text(r, label);
    }
    private static void Safe(Action action)
    {
        try { action(); } catch (Exception ex) { if (status != null) status.text = ex.Message; Plugin.ModLog.LogWarning("[JSON editor] " + ex.Message); }
    }
    internal static void Open(EditorSession provider)
    {
        Safe(() =>
        {
            Close(); session=provider; expanded = -1; errors.Clear(); openGroups.Clear();
            var fonts = Object.FindObjectsOfType<TMP_Text>();
            font = fonts.FirstOrDefault(t => t.font != null)?.font ?? throw new InvalidOperationException("No game UI font is available.");
            panel = new GameObject("Sprocket.JsonEditor"); var canvas = panel.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            panel.AddComponent<GraphicRaycaster>(); var scaler = panel.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var backdrop = Rect("Backdrop", panel.transform); backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .85f);
            var body = Rect("Editor", backdrop); body.anchorMin = new Vector2(.12f, .06f); body.anchorMax = new Vector2(.88f, .94f); body.offsetMin = body.offsetMax = Vector2.zero;
            body.gameObject.AddComponent<Image>().color = new Color(.08f, .08f, .07f, 1);
            var title=Rect("Title",body); Place(title,0,44,12); Text(title,session.Title,24);
            var help=Rect("Help",body); Place(help,48,44,12);
            Text(help,"Edits affect all parts using this entry. Save validates, backs up and refreshes.",17);
            var notice=Rect("Status",body); Place(notice,96,36,12);
            status=Text(notice,"Expand an entry to edit it.",16);
            var scrollRoot=Rect("Scroll",body); scrollRoot.anchorMin=Vector2.zero;scrollRoot.anchorMax=Vector2.one;
            scrollRoot.offsetMin=new Vector2(12,64);scrollRoot.offsetMax=new Vector2(-12,-140);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.inertia=false;
            var view = Rect("Viewport", scrollRoot); view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.offsetMin = view.offsetMax = Vector2.zero;
            view.gameObject.AddComponent<Image>().color = new Color(.1f,.1f,.1f,1); view.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", view); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.offsetMin = content.offsetMax = Vector2.zero;
            var rows = content.gameObject.AddComponent<VerticalLayoutGroup>(); rows.spacing = 4; rows.childControlHeight = true; rows.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view; scroll.content = content;
            var footer=Rect("Actions",body);footer.anchorMin=footer.anchorMax=Vector2.zero;footer.anchorMax=new Vector2(1,0);footer.pivot=new Vector2(.5f,0);footer.sizeDelta=new Vector2(-24,44);footer.anchoredPosition=new Vector2(0,12);
            var actions=footer.gameObject.AddComponent<HorizontalLayoutGroup>();actions.spacing=12;actions.childControlHeight=true;actions.childControlWidth=true;actions.childForceExpandWidth=true;
            Button(footer, "+ Add " + session.ItemName, () => Templates());
            Button(footer, "Save and refresh", () => { if(errors.Count > 0) throw new FormatException("Correct invalid entries before saving."); session!.Save(); Close(); });
            Button(footer, "Cancel", Close); Draw();
        });
    }
    private static void Place(RectTransform rect,float top,float height,float margin)
    {
        rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
        rect.offsetMin=new Vector2(margin,-top-height);rect.offsetMax=new Vector2(-margin,-top);
    }
    private static void ClearRows()
    {
        if(content == null) return;
        for(int i = content.childCount-1;i>=0;i--){var go=content.GetChild(i).gameObject;go.SetActive(false);Object.Destroy(go);}
    }
    private static void Templates()
    {
        if(errors.Count > 0) throw new FormatException("Correct invalid entries first.");
        ClearRows();
        var templates = session!.Templates;
        foreach(var node in templates){var template=node!.AsObject(); Button(content!, "+ " + template["label"]!.GetValue<string>(), () => { expanded=session!.Duplicate(template);Draw(); });}
        Button(content!, "Back", Draw);
    }
    private static void Draw()
    {
        ClearRows();
        for(int i=0;i<session!.Items.Count;i++)
        {
            int index=i; var profile=session.Items[i]!.AsObject();
            Button(content!, (expanded==i?"[-] ":"[+] ")+profile["label"]!.GetValue<string>(), () => {if(errors.Count>0)throw new FormatException("Correct invalid entries before changing shells.");expanded=expanded==index?-1:index;Draw();});
            if(expanded!=i)continue;
            Button(content!, "Duplicate " + session.ItemName, () => {if(errors.Count>0)throw new FormatException("Correct invalid entries first."); expanded=session.Duplicate(profile);Draw();});
            var fields=session.Fields(i).ToArray();
            var groups=session.Groups(i).Concat(fields.Select(key=>session.FieldGroup(i,key)))
                .Where(group=>!string.IsNullOrEmpty(group)).Distinct().ToArray();
            foreach(var key in fields.Where(key=>string.IsNullOrEmpty(session.FieldGroup(index,key))))
                AddField(index,key,session.Value(index,key));
            foreach(var group in groups)
            {
                var groupKey=index+":"+group;
                Button(content!, (openGroups.Contains(groupKey)?"[-] ":"[+] ")+group, ()=>
                {
                    if(errors.Count>0)throw new FormatException("Correct invalid entries before changing modules.");
                    if(!openGroups.Add(groupKey))openGroups.Remove(groupKey);
                    Draw();
                });
                if(!openGroups.Contains(groupKey))continue;
                var description=session.GroupDescription(index,group);
                if(!string.IsNullOrEmpty(description))Text(Rect("Module description",content!,48),description,15);
                var groupFields=fields.Where(key=>session.FieldGroup(index,key)==group).ToArray();
                if(groupFields.Length==0&&string.IsNullOrEmpty(description))
                    Text(Rect("Empty module",content!,32),"No editable modifiers for this configuration.",15);
                foreach(var key in groupFields)AddField(index,key,session.Value(index,key));
            }
        }
    }
    private static void AddField(int index,string key,JsonNode value)
    {
        if(session!.Choices(key)!=null){Choice(index,key,value.GetValue<string>());return;}
        var r=Rect(key,content!,56); Text(r, session!.Caption(index,key),16).rectTransform.anchorMin=new Vector2(0,.45f);
        var box=Rect("Input",r);box.anchorMin=Vector2.zero;box.anchorMax=new Vector2(1,.48f);box.offsetMin=new Vector2(8,0);box.offsetMax=new Vector2(-8,0);
        box.gameObject.SetActive(false);
        var image=box.gameObject.AddComponent<Image>();image.color=new Color(.22f,.22f,.22f,1);
        var text=Text(box,"");text.TryCast<TextMeshProUGUI>()!.maskable=true;var input=box.gameObject.AddComponent<TMP_InputField>();input.textViewport=box;input.textComponent=text.TryCast<TextMeshProUGUI>();input.targetGraphic=image;input.customCaretColor=true;input.caretColor=Color.white;input.caretWidth=2;input.caretBlinkRate=.8f;input.selectionColor=new Color(.3f,.5f,.85f,.6f);
        input.text=value is JsonValue j && j.TryGetValue<string>(out var s)?s:value.ToJsonString();
        input.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>((Action<string>)(raw => {
            try{session!.Set(index,key,raw);errors.Remove(index+":"+key);image.color=new Color(.22f,.22f,.22f,1);status!.text="Unsaved changes. Save to refresh.";}
            catch(Exception ex){errors[index+":"+key]=ex.Message;image.color=new Color(.5f,.1f,.1f,1);status!.text=session!.Caption(index,key)+": "+ex.Message;}
        })));
        box.gameObject.SetActive(true);
        var range=session.NumberRange(index,key);
        if(range!=null&&EditorSliderMath.IsValid(range)&&value is JsonValue numeric&&numeric.TryGetValue<double>(out var initial)&&double.IsFinite(initial))
            AddSlider(r,index,key,input,range,initial);
    }
    private static void AddSlider(RectTransform row,int index,string key,TMP_InputField input,EditorNumberRange range,double initial)
    {
        input.textViewport.anchorMax=new Vector2(.38f,.48f);
        var track=Rect("Slider",row);track.anchorMin=new Vector2(.42f,.12f);track.anchorMax=new Vector2(.98f,.36f);track.offsetMin=track.offsetMax=Vector2.zero;
        var background=track.gameObject.AddComponent<Image>();background.color=new Color(.18f,.18f,.18f,1);
        var handle=Rect("Handle",track);handle.sizeDelta=new Vector2(18,28);
        var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(.75f,.61f,.35f,1);
        var slider=track.gameObject.AddComponent<Slider>();slider.targetGraphic=handleImage;slider.handleRect=handle;
        slider.direction=Slider.Direction.LeftToRight;slider.minValue=(float)range.Minimum;slider.maxValue=(float)range.Maximum;
        slider.SetValueWithoutNotify((float)initial);
        slider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>((Action<float>)(raw=>Safe(()=>
        {
            var next=EditorSliderMath.Snap(raw,range).ToString("G12",CultureInfo.InvariantCulture);
            try{session!.Set(index,key,next);errors.Remove(index+":"+key);input.SetTextWithoutNotify(next);var image=input.targetGraphic?.TryCast<Image>();if(image!=null)image.color=new Color(.22f,.22f,.22f,1);status!.text="Unsaved changes. Save to refresh.";}
            catch(Exception ex){errors[index+":"+key]=ex.Message;status!.text=session!.Caption(index,key)+": "+ex.Message;}
        }))));
        input.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>((Action<string>)(raw=>
        {
            if(double.TryParse(raw.Replace(',', '.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var parsed)&&double.IsFinite(parsed))
                slider.SetValueWithoutNotify((float)parsed);
        })));
    }
    private static void Choice(int index,string key,string current)
    {
        var choices=session!.Choices(key)!;
        var row=Rect(key,content!,56);Text(row,session!.Caption(index,key),16).rectTransform.anchorMin=new Vector2(0,.45f);
        var box=Rect("Choice",row);box.anchorMin=Vector2.zero;box.anchorMax=new Vector2(1,.48f);box.offsetMin=new Vector2(8,0);box.offsetMax=new Vector2(-8,0);
        box.gameObject.SetActive(false);
        var image=box.gameObject.AddComponent<Image>();image.color=new Color(.22f,.22f,.22f,1);
        var caption=Text(box,"");
        var dropdown=box.gameObject.AddComponent<TMP_Dropdown>();dropdown.targetGraphic=image;dropdown.captionText=caption;
        var template=Rect("Template",box);template.anchorMin=new Vector2(0,0);template.anchorMax=new Vector2(1,0);template.pivot=new Vector2(.5f,1);template.sizeDelta=new Vector2(0,220);template.anchoredPosition=new Vector2(0,-2);
        template.gameObject.AddComponent<Image>().color=new Color(.13f,.13f,.13f,1);
        var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Rect("Viewport",template);viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;viewport.gameObject.AddComponent<RectMask2D>();
        var options=Rect("Options",viewport);options.anchorMin=new Vector2(0,1);options.anchorMax=Vector2.one;options.pivot=new Vector2(.5f,1);options.sizeDelta=new Vector2(0,32);
        var item=Rect("Item",options);item.anchorMin=Vector2.zero;item.anchorMax=new Vector2(1,0);item.pivot=new Vector2(.5f,0);item.sizeDelta=new Vector2(0,32);
        var itemImage=item.gameObject.AddComponent<Image>();itemImage.color=new Color(.25f,.23f,.18f,1);var toggle=item.gameObject.AddComponent<Toggle>();toggle.targetGraphic=itemImage;
        var check=Rect("Selected",item);check.anchorMin=Vector2.zero;check.anchorMax=Vector2.one;check.offsetMin=check.offsetMax=Vector2.zero;var mark=check.gameObject.AddComponent<Image>();mark.color=new Color(.45f,.35f,.15f,1);toggle.graphic=mark;
        dropdown.itemText=Text(item,"");scroll.viewport=viewport;scroll.content=options;
        template.gameObject.SetActive(false);dropdown.template=template;
        var labels=new Il2CppSystem.Collections.Generic.List<string>();foreach(var choice in choices)labels.Add(session!.ChoiceLabel(key,choice));dropdown.AddOptions(labels);dropdown.value=Math.Max(0,Array.IndexOf(choices,current));dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<int>>((Action<int>)(option=>Safe(()=>{
            if(errors.Count>0)throw new FormatException("Correct invalid entries first.");
            session!.SetChoice(index,key,choices[option]);
            Draw();status!.text="Unsaved changes. Save to refresh.";
        }))));
        box.gameObject.SetActive(true);
    }
    internal static void Close(){if(panel!=null){panel.SetActive(false);Object.Destroy(panel);}panel=null;content=null;status=null;session=null;RuntimeJsonEditorInput.Restore();}
}








