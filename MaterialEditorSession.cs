using BepInEx;
using SprocketJsonEditor;
using System.Text.Json.Nodes;
using Sprocket.Vehicles.PlateStructures;
using Sprocket.TechTrees;
namespace SprocketMaterialSelector;
internal sealed class MaterialEditorSession:EditorSession
{
    private readonly MaterialEditorDraft draft;
    private readonly Action refresh;
    internal MaterialEditorSession(Action refresh)
    {
        this.refresh=refresh;
        using var stream=typeof(MaterialEditorSession).Assembly.GetManifestResourceStream("MaterialSelector.ResponseTemplates")!;
        using var reader=new StreamReader(stream);
        draft=new(TechTreeLoader.TechDirectory,Path.Combine(Paths.ConfigPath,"sprocket.armour.responses.json"),Path.Combine(Paths.ConfigPath,"sprocket.materialselector.editor.json"),MaterialDatabase.Materials.Select(m=>(m.Id,m.Label,m.SourceFile)),reader.ReadToEnd());
    }
    public override string Title=>"Material editor";
    public override string ItemName=>"material";
    public override JsonArray Items=>draft.Items;
    public override JsonArray Templates=>draft.Templates;
    public override IEnumerable<string> Fields(int index)
    {
        foreach(var field in new[]{"behavior","label","date","rhaFactor","density","spallFactor","costMultiplier"})yield return field;
        if(Items[index]!["response"] is {} r)
        {
            yield return "effectsEnabled";
            foreach(var key in Scalars(r["calibration"]!,"response.calibration"))yield return key;
            foreach(var key in Scalars(r["geometry"]!,"response.geometry"))yield return key;
            if(Items[index]!["behavior"]!.GetValue<string>() is "lightEra" or "heavyEra")yield return "response.era.cellPitchM";
        }
    }
    private static IEnumerable<string> Scalars(JsonNode node,string path)
    {
        if(node is JsonObject o){foreach(var field in o)if(field.Value!=null)foreach(var s in Scalars(field.Value,path+"."+field.Key))yield return s;}
        else if(node is JsonArray a){for(int i=0;i<a.Count;i++)foreach(var s in Scalars(a[i]!,path+"."+i))yield return s;}
        else yield return path;
    }
    public override JsonNode Value(int index,string field)=>field=="effectsEnabled"?JsonValue.Create(draft.EffectsEnabled?"true":"false")!:draft.Value(index,field);
    public override string Caption(int index,string field)
    {
        var last=field.Split('.').Last();
        var suffix=last switch {"rhaFactor"=>" (× RHA; >0)","density"=>" (kg/m³; >0; special materials 100–25000)","spallFactor"=>" (≥0; special materials 0–1)","costMultiplier"=>" (≥0; balanced minimum applies)","date"=>" (yyyy.MM.dd)","heatRetention"=>$" (remaining HEAT fraction; {draft.MinimumHeatRetention:0.###}–1)","intactRodRetention" or "disturbedRodRetention"=>$" (remaining rod fraction; {draft.MinimumRodRetention:0.###}–1)","disturbanceContribution"=>" (0–0.35)","minNormalThicknessMm"=>" (mm; 0.1–1000)","maxNormalThicknessMm"=>" (mm; minimum thickness–1000)","minMeasuredGapMm"=>" (mm; 0–1000)","maxMeasuredGapMm"=>" (mm; minimum gap–1000)","angleDegrees"=>" (°; 0–89.9; ascending)","distanceMm"=>" (mm; 0–1000; ascending)","weight"=>" (0–1)","cellPitchM"=>" (m; 0.01–10)",_=>""};
        if(field=="effectsEnabled")return "Special effects enabled (shared; requires Shell Selector)";
        var label=System.Text.RegularExpressions.Regex.Replace(last,"([a-z])([A-Z])","$1 $2");
        if(field.Contains("Curve")){var parts=field.Split('.');label=parts[^3]+" point "+(int.Parse(parts[^2])+1)+": "+label;}
        return char.ToUpperInvariant(label[0])+label.Substring(1)+suffix;
    }
    public override string[]? Choices(string field)=>field switch {"behavior"=>new[]{"passive","glassTextolite","nera","lightEra","heavyEra","passiveComposite"},"effectsEnabled"=>new[]{"true","false"},"response.geometry.mode"=>new[]{"declaredCassette","resolvedLayers"},_=>null};
    public override string ChoiceLabel(string field,string value)=>field switch {"behavior"=>MaterialEditorDraft.BehaviorLabel(value),"effectsEnabled"=>value=="true"?"Enabled":"Disabled",_=>value=="declaredCassette"?"Cassette thickness":"Resolved layers and measured gaps"};
    public override void Set(int index,string field,string value)=>draft.Set(index,field,value);
    public override void SetChoice(int index,string field,string value){if(field=="behavior")draft.Behavior(index,value);else draft.Set(index,field,value);}
    public override int Duplicate(JsonObject template)=>draft.Duplicate(template);
    public override void Save(){draft.Save();try{refresh();}catch(Exception ex){Plugin.ModLog.LogError("[Material editor] Saved files; native refresh failed: "+ex);throw new IOException("Materials saved, but refresh failed: "+ex.GetBaseException().Message.Split('\n')[0],ex);}}
}


