using HarmonyLib;
using Sprocket;
using Sprocket.PlayerControl;
using Sprocket.Vehicles;
using SprocketShellSelector;

namespace SprocketMaterialSelector;

[HarmonyPatch]
internal static class ReliktSpentVisuals
{
    // Rendering references only. The authoritative expenditure ledger is Shell's.
    private static readonly Dictionary<(IntPtr Root,int Vuid),VehicleObjectModel> Models=new();
    private static bool subscribed;
    private static int activationLogs;
    internal static void Start()
    {if(subscribed)return;PlacedEra.Activated+=Activated;subscribed=true;}

    private static bool Own(VehicleObjectModel model)=>model!=null&&EraPresetRegistry.Cassette(model.ComponentID,out _)
        &&EraPresetRegistry.Visual(model.ComponentID,out _,out bool cassette)&&cassette;
    internal static void Observe(VehicleObjectModel model)
    {
        if(!Own(model)||model.VehicleRoot is not {} root)return;
        var key=(root.Pointer,model.VUID.Value);
        if(Models.Count>=4096&&!Models.ContainsKey(key))return;
        Models[key]=model;
        Reconcile(model);
    }
    private static void Reconcile(VehicleObjectModel model)
    {
        if(model.Model==null||model.VehicleRoot is not {} root)return;
        // Model visibility only, after the impact has completed. Passive damage
        // geometry and native mass deliberately remain as a first-version proxy.
        bool spent=PlacedEra.IsSpent(root.Pointer,model.VUID.Value);
        model.Model.SetVisible(!spent&&model.visible);
    }
    private static void Activated(PlacedEraActivation activation)
    {
        bool tracked=Models.TryGetValue((activation.VehicleRootPointer,activation.ComponentVuid),out var model);
        bool exact=tracked&&Own(model!)&&model!.VehicleRoot?.Pointer==activation.VehicleRootPointer&&model.Vehicle?.Pointer==activation.VehiclePointer;
        if(activationLogs++<24)Plugin.ModLog.LogInfo($"[Relikt activation received] vuid={activation.ComponentVuid} spawn={activation.Spawn} tracked={tracked} exact={exact} spent={PlacedEra.IsSpent(activation.VehicleRootPointer,activation.ComponentVuid)}");
        if(!exact||model==null)return;
        Reconcile(model);
        if(activationLogs<=24)Plugin.ModLog.LogInfo($"[Relikt hide applied] vuid={activation.ComponentVuid} modelPresent={model.Model!=null} nativePartVisible={model.visible}; cassette renderer hidden from authoritative spent query");
    }
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.OnModelLoaded))]
    private static void Loaded(VehicleObjectModel __instance)
    {try{Observe(__instance);}catch(Exception ex){Plugin.ModLog.LogWarning("Relikt spent model: "+ex.Message);}}
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.SetVisible))]
    private static void Visibility(VehicleObjectModel __instance)
    {if(!Own(__instance))return;try{Observe(__instance);}catch(Exception ex){Plugin.ModLog.LogWarning("Relikt visibility: "+ex.Message);}}
    [HarmonyPrefix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.Release))]
    private static void Released(VehicleObjectModel __instance)
    {
        EraSurface.Forget(__instance);
        if(!Own(__instance)||__instance.VehicleRoot is not {} root)return;
        var key=(root.Pointer,__instance.VUID.Value);
        if(Models.TryGetValue(key,out var current)&&current.Pointer==__instance.Pointer)Models.Remove(key);
    }
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleEditorScenarioGameState),nameof(VehicleEditorScenarioGameState.OnPlayerStateChanged))]
    private static void EnteredEditor(VehicleEditorScenarioGameState __instance,PlayerStateChangeEventArgs __1)
    {
        try
        {
            // This is the committed player-state event used by the native state
            // callback, not creation of the asynchronous Enter task or a request.
            var editor=__instance.editorPlayerState;
            if(editor==null||__1?.NewState==null||__1.PreviousState==null||__1.NewState.Pointer!=editor.Pointer||__1.PreviousState.Pointer==editor.Pointer)return;
            PlacedEra.ResetForEdit();
            foreach(var model in Models.Values.ToArray())Reconcile(model);
        }
        catch(Exception ex){Plugin.ModLog.LogWarning("Relikt Edit reset: "+ex.Message);}
    }
}

