using Sprocket.Vehicles;
using Sprocket.Vehicles.AssetManagement;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SprocketMaterialSelector;

// Native material register/painter owns paint changes and material lifetime.
// No fixed-colour shader, shared vehicle mutation, or extra rendering hooks.
internal static class EraSurface
{
    private sealed record Surface(IntPtr Vehicle,IntPtr Transform,VehicleMaterial Material);
    private static readonly Dictionary<IntPtr,Surface> Surfaces=new();
    private static VehicleMaterialConfiguration? factory;
    private static int appliedLogs;
    internal static void Apply(VehicleObjectModel model,bool cassette)
    {
        var native=model.Model;var vehicle=model.Vehicle;var transform=model.VehicleTransform;
        if(native==null||vehicle==null||transform==null)return;
        if(!Surfaces.TryGetValue(model.Pointer,out var current)||current.Vehicle!=vehicle.Pointer||current.Transform!=transform.Pointer)
        {
            if(Surfaces.Count>=4096)throw new InvalidOperationException("ERA paint cache limit reached; native fallback retained.");
            if(factory==null)
            {
                factory=ScriptableObject.CreateInstance<VehicleMaterialConfiguration>();
                factory.hideFlags=HideFlags.HideAndDontSave;
                factory.flags=VehicleMaterialFlags.None;factory.slot=VehicleMaterialSlot.Exterior;
                // Native source clones force CustomShader, which bypasses normal paint.
                factory.materialSource=null;
                factory.baseColourMap=NeutralMap("ERA base",Color.white,false);
                factory.armMap=NeutralMap("ERA surface",new Color(1f,.5f,0f,1f),true);
                factory.normalMap=NeutralMap("ERA normal",new Color(.5f,.5f,1f,1f),true);
                // VehicleLit: paint weight = DPM.b * (1-saturate(3*(DPM.g-condition))).
                // Blue enables paint; red/green zero adds no dirt or wear mask.
                factory.dpmMap=NeutralMap("ERA paint mask",new Color(0f,0f,1f,1f),true);
            }
            var material=vehicle.Materials.NewMaterial(factory.Cast<IVehicleMaterialFactory>(),transform);
            if(material==null)throw new InvalidOperationException("Native ERA paint material unavailable.");
            material.Flags=VehicleMaterialFlags.None;material.PaintSlot=VehicleMaterialSlot.Exterior;
            current=new(vehicle.Pointer,transform.Pointer,material);Surfaces[model.Pointer]=current;
            if(appliedLogs++<6)Plugin.ModLog.LogInfo($"[ERA vehicle paint] component={model.ComponentID} vuid={model.VUID.Value} shader={material.Material?.shader?.name} slot={material.PaintSlot} flags={material.Flags}; native linked exterior material");
        }
        var materials=native.Materials;
        if(materials==null||materials.Length!=1||materials[0]?.Pointer!=current.Material.Pointer)
        {
            native.SetMaterials(new Il2CppReferenceArray<VehicleMaterial>(new[]{current.Material}));
            vehicle.Materials.MarkMaterialModified(current.Material);
            vehicle.MaterialProcessor.Process(current.Material,transform);
        }
    }
    private static Texture2D NeutralMap(string name,Color colour,bool linear)
    {
        var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,linear)
        {name=name,hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};
        texture.SetPixel(0,0,colour);
        texture.Apply(false,true);
        return texture;
    }
    internal static void Forget(VehicleObjectModel model)=>Surfaces.Remove(model.Pointer);
}
