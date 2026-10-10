using System.Globalization;
using HarmonyLib;
using Sprocket.Vehicles;
using UnityEngine;

namespace SprocketMaterialSelector;

// Measurement/visual candidate only. No ERA consumption or impact mutation.
[HarmonyPatch]
internal static class ReliktPrototype
{
    internal const string CassetteId = "reliktCassette";
    internal const string MountId = "reliktMount";
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static int identityLogs;
    private static int geometryLogs;
    private static bool Own(VehicleObjectModel model) => model != null &&
        EraPresetRegistry.Visual(model.ComponentID,out _,out _);

    [HarmonyPrefix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.Build))]
    private static void Building(VehicleObjectModel __instance)
    { if (Own(__instance)) __instance.staticRenderer = false; }

    [HarmonyPostfix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.Build))]
    private static void Built(VehicleObjectModel __instance)
    {
        if (!Own(__instance)) return;
        try
        {
            // Native definition.mass is the sole mass owner. SetCost replaces its
            // resource entry; it does not add a second visual/mechanism mass.
            EraPresetRegistry.Visual(__instance.ComponentID,out var preset,out bool cassette);
            float mass=cassette ? preset.CassetteMass : preset.MountMass;
            float rha=cassette ? .55f : 1f, density=cassette ? 4000f : 7850f;
            float spall=cassette ? .25f : __instance.damageColliderParameters.SpallFactor;
            float price=cassette ? 10.5f : 2f;
            if(cassette)
            {
                if(!EraPresetRegistry.Cassette(__instance.ComponentID,out var binding))
                    throw new InvalidOperationException("Exact cassette binding is unavailable; passive native definition retained.");
                var material=MaterialDatabase.Materials.FirstOrDefault(m=>m.Id==binding.MaterialId);
                if(material==null)throw new InvalidOperationException("Native Relikt material is missing; protection adapter must remain unavailable.");
                rha=material.RhaFactor;density=material.Density;spall=material.SpallFactor;price=material.Balance.EffectiveCostMultiplier;
            }
            // Constructor order differs from field presentation: RHA, spall, density, priority.
            __instance.damageColliderParameters=new Sprocket.Vehicles.Colliders.DamageModelParameters(rha,spall,density,__instance.damageColliderParameters.Priority);
            var balance=MaterialBalance.Calculate(rha,density,price);
            if(!balance.IsValid || !MaterialBalance.TryMaterialCost(mass,balance.EffectiveCostMultiplier,Sprocket.Constants.CostPerKgOfArmour,out float cost))
                throw new InvalidOperationException("Relikt material cost is invalid.");
            float assembly=Math.Max(__instance.GetCost(MassType.Armour,CostType.Assembly),cost*.2f);
            __instance.SetCost(cost,MassType.Armour,CostType.Material);
            __instance.SetCost(assembly,MassType.Armour,CostType.Assembly);
        }
        catch(Exception ex){Plugin.ModLog.LogWarning("Relikt native balance: "+ex.Message);}
    }

    [HarmonyPostfix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.OnModelLoaded))]
    private static void Loaded(VehicleObjectModel __instance)
    {
        if (!Own(__instance)) return;
        try { Apply(__instance); }
        catch (Exception ex) { Plugin.ModLog.LogWarning("Relikt prototype model: " + ex.Message); }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.BuildDamageModelColliders))]
    private static void BuildingDamage(VehicleObjectModel __instance)
    {
        if (!Own(__instance)) return;
        try
        {
            Apply(__instance);
            if(EraPresetRegistry.Visual(__instance.ComponentID,out _,out bool cassette)&&cassette&&geometryLogs++<24)
            {
                var actual=__instance.Model?.CollisionMesh;
                var expected=Geometry(__instance.ComponentID,true);
                Plugin.ModLog.LogInfo($"[Relikt geometry] vuid={__instance.VUID.Value} collisionMatch={actual==expected} vertices={actual?.vertexCount} bounds={actual?.bounds.size} expectedBounds={expected.bounds.size} modelScale={__instance.GetModelScale()} modelPosition={__instance.ModelPosition} modelRotation={__instance.ModelRotation}; native geometry only, no declared thickness override");
            }
            if (identityLogs++ < 24)
                Plugin.ModLog.LogInfo($"[Relikt identity] component={__instance.ComponentID} vuid={__instance.VUID.Value} generated={__instance.generateDamageColliders}; native own VUID is the damage identity");
        }
        catch (Exception ex) { Plugin.ModLog.LogWarning("Relikt prototype damage identity: " + ex.Message); }
    }

    private static Mesh Geometry(string id, bool collision=false)
    {
        if(!EraPresetRegistry.Visual(id,out var preset,out bool cassette))throw new InvalidDataException("Unknown placed ERA visual component.");
        string materialKey=EraPresetRegistry.Cassette(id,out var binding) ? binding.MaterialId+":"+binding.ResponseId : "passive";
        string cacheKey=id+":"+materialKey+(collision ? ":collision" : ":visual");
        if (Meshes.TryGetValue(cacheKey, out var cached)) return cached;
        var positions = new List<Vector3>(); var expanded = new List<Vector3>();
        string activeGroup = "";
        string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "relikt-assets", collision&&cassette ? preset.Collision : preset.Visual);
        foreach (string line in File.ReadLines(path))
        {
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 0) continue;
            if (fields[0] == "g") activeGroup = fields[1];
            if (fields[0] == "v") positions.Add(new Vector3(float.Parse(fields[1], CultureInfo.InvariantCulture),float.Parse(fields[2], CultureInfo.InvariantCulture),float.Parse(fields[3], CultureInfo.InvariantCulture)));
            if (fields[0] != "f" || activeGroup != (cassette ? "cassette" : "mount")) continue;
            if (fields.Length != 4) throw new InvalidDataException("Relikt triangle required");
            for (int i=1;i<4;i++) { int index=int.Parse(fields[i],CultureInfo.InvariantCulture)-1; if(index<0||index>=positions.Count)throw new InvalidDataException("Relikt vertex index"); expanded.Add(positions[index]); }
        }
        if (expanded.Count == 0 || expanded.Count > 2000 || expanded.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))) throw new InvalidDataException("Relikt geometry invalid");
        var mesh=new Mesh { name="Original Relikt prototype "+id,hideFlags=HideFlags.HideAndDontSave };
        mesh.vertices=expanded.ToArray();mesh.triangles=Enumerable.Range(0,expanded.Count).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        // VehicleLit expects a valid tangent frame, including with flat normal maps.
        // Expanded triangles already have hard face normals; use a matching planar
        // UV frame without changing geometry, collision indices or native placement.
        var normals=mesh.normals;
        var uv=new Vector2[expanded.Count];var tangents=new Vector4[expanded.Count];
        for(int i=0;i<expanded.Count;i+=3)
        {
            var normal=normals[i];
            var axis=Math.Abs(normal.y)<.9f ? Vector3.up : Vector3.forward;
            var tangent=Vector3.Cross(axis,normal).normalized;
            var bitangent=Vector3.Cross(normal,tangent);
            for(int j=i;j<i+3;j++)
            {
                uv[j]=new Vector2(Vector3.Dot(expanded[j],tangent),Vector3.Dot(expanded[j],bitangent));
                tangents[j]=new Vector4(tangent.x,tangent.y,tangent.z,1f);
            }
        }
        mesh.uv=uv;mesh.tangents=tangents;
        Meshes.Add(cacheKey,mesh);return mesh;
    }

    private static void Apply(VehicleObjectModel model)
    {
        var native=model.Model;if(native==null)return;
        var mesh=Geometry(model.ComponentID);model.MarkDynamic();
        if(native.InteractionMesh!=mesh)
        {native.SetMesh(mesh);native.SetShadowProxy(mesh);native.SetInteractionMesh(mesh);}
        EraPresetRegistry.Visual(model.ComponentID,out _,out bool cassette);
        var collision=cassette ? Geometry(model.ComponentID,true) : mesh;
        if(native.CollisionMesh!=collision)native.SetCollisionMesh(collision);
        // Native per-vehicle exterior paint; renderer updates retain the linked wrapper.
        EraSurface.Apply(model,cassette);
        ReliktSpentVisuals.Observe(model);
    }
}
