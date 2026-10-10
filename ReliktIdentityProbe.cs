using System.Reflection;
using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.Vehicles.Colliders;

namespace SprocketMaterialSelector;

// Read-only exact argument comparison. Scope is exclusively the new cassette.
[HarmonyPatch]
internal static class ReliktIdentityProbe
{
    private static int logs;
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(VehicleObjectModel),
        nameof(VehicleObjectModel.BuildModelDamageColliders),
        new[] { typeof(VUID), typeof(IVehicleComponentDamageModelBuilder), typeof(DamageModelParameters).MakeByRefType() });

    [HarmonyPrefix]
    private static void Building(VehicleObjectModel __instance, VUID __0)
    {
        if (__instance == null || __instance.ComponentID != ReliktPrototype.CassetteId || logs >= 24) return;
        logs++;
        bool exact = __instance.VUID.Value == __0.Value;
        Plugin.ModLog.LogInfo($"[Relikt exact identity] componentVUID={__instance.VUID.Value} nativeBuilderVUID={__0.Value} exact={exact}");
    }
}
