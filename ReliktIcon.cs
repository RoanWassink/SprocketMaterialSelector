using HarmonyLib;
using Sprocket.PartImporting;
using UnityEngine;

namespace SprocketMaterialSelector;

[HarmonyPatch]
internal static class ReliktIcon
{
    internal const string PartGuid="ef15b8e5-d4c3-4d38-8b03-f07bb395ab63";
    private static readonly Dictionary<string,Sprite> Icons=new();
    private static readonly HashSet<string> Failed=new();
    [HarmonyPostfix,HarmonyPatch(typeof(PartDefinitionCardFactory),nameof(PartDefinitionCardFactory.CreateCard))]
    private static void Created(PartDefinition __0,PartDisplayCard __result)
    {
        if(__0==null||__result==null)return;
        var preset=EraPresetRegistry.Presets.FirstOrDefault(p=>p.PartGuid==__0.guid);
        if(preset==null||Failed.Contains(preset.PartGuid))return;
        try
        {
            if(!Icons.TryGetValue(preset.PartGuid,out var icon))
            {
                string path=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"relikt-assets",preset.Icon);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false))throw new InvalidDataException("Relikt icon decode failed");
                icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
                icon.hideFlags=HideFlags.HideAndDontSave;
                Icons.Add(preset.PartGuid,icon);
            }
            __result.Icon=icon;
        }
        catch(Exception ex){Failed.Add(preset.PartGuid);Plugin.ModLog.LogWarning("Optional ERA icon: "+ex.Message);}
    }
}
