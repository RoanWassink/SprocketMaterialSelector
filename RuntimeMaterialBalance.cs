using System;
using System.Collections.Generic;
using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.Vehicles.PlateStructures;

namespace SprocketMaterialSelector;

[HarmonyPatch]
internal static class RuntimeMaterialBalance
{
    private static readonly HashSet<string> warnings = new(StringComparer.Ordinal);

    private static void Warn(string id, string error)
    {
        if (warnings.Add(id + error))
            Plugin.ModLog.LogWarning($"[Material Balance] Rejected {id}: {error} Using RHA.");
    }

    private static MaterialBalanceResult Read(PlateStructure component)
    {
        var result = MaterialBalance.Calculate(component.damageModelParameters.RhaFactor,
            component.armourDensity, component.armourCostMultiplier);
        var spall = component.damageModelParameters.SpallFactor;
        return !float.IsFinite(spall) || spall < 0
            ? result with { IsValid = false, ValidationError = "Invalid runtime spall factor." }
            : result;
    }

    private static void UseRha(PlateStructure component, string reason)
    {
        Warn(component.armourTechID, reason);
        if (component.Blueprint != null)
        {
            component.Blueprint.armourTechID = "rha";
            component.blueprintRef.MarkModified();
        }
        // Assign the safe reference explicitly: even a malformed RHA JSON must
        // not cause recursion or an invalid fallback.
        component.armourTechID = "rha";
        component.armourDensity = (float)MaterialBalance.ReferenceDensity;
        component.armourCostMultiplier = (float)MaterialBalance.ReferenceCostMultiplier;
        component.damageModelParameters = new Sprocket.Vehicles.Colliders.DamageModelParameters(
            (float)MaterialBalance.ReferenceRhaFactor, component.armourDensity,
            0.0005f, component.damageModelParameters.Priority);
        component.Vehicle.RaiseDirtyFlags(VehicleDirtyFlags.DamageModel);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlateStructure), nameof(PlateStructure.Build))]
    private static void BeforeBuild(PlateStructure __instance)
    {
        var id = __instance.Blueprint?.armourTechID;
        if (id != null && MaterialDatabase.RejectedIds.Contains(id))
            UseRha(__instance, "Invalid Technology material in saved blueprint.");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlateStructure), nameof(PlateStructure.SetArmourMaterial))]
    private static void AfterSync(PlateStructure __instance)
    {
        var result = Read(__instance);
        if (!result.IsValid) UseRha(__instance, result.ValidationError);
        else __instance.armourCostMultiplier = result.EffectiveCostMultiplier;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlateStructure), nameof(PlateStructure.SyncWithArmourTech))]
    private static void AfterTechnologySync(PlateStructure __instance) => AfterSync(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlateStructure), nameof(PlateStructure.Build))]
    private static void AfterBuild(PlateStructure __instance)
    {
        // SetArmourMaterial inlines SyncWithArmourTech in the native build.
        // This final check also covers native inlining and materials loaded
        // without ever opening our dropdown. Vehicle totals are calculated later.
        var result = Read(__instance);
        var corrected = result.WasAdjusted;
        if (!result.IsValid || !MaterialBalance.TryMaterialCost(
                __instance.GetMass(MassType.Armour), result.EffectiveCostMultiplier,
                Sprocket.Constants.CostPerKgOfArmour, out var cost))
        {
            corrected = true;
            UseRha(__instance, result.IsValid ? "Total material cost exceeds supported range." : result.ValidationError);
            var mass = (double)__instance.armourVolume * __instance.armourDensity;
            if (!double.IsFinite(mass) || mass > MaterialBalance.MaxSupportedValue || mass < 0 ||
                !MaterialBalance.TryMaterialCost((float)mass, __instance.armourCostMultiplier,
                    Sprocket.Constants.CostPerKgOfArmour, out cost))
                throw new InvalidOperationException("Plate geometry exceeds supported mass/cost range even with RHA.");
            __instance.SetMass((float)mass, MassType.Armour);
            __instance.cachedMass = __instance.GetMass(MassType.Everything);
            __instance.RecalculateCenterOfMass();
        }
        else __instance.armourCostMultiplier = result.EffectiveCostMultiplier;
        // Preserve vanilla floating-point arithmetic exactly when no floor or
        // fallback was needed (particularly vanilla RHA).
        if (corrected) __instance.SetCost(cost, MassType.Armour, CostType.Material);
    }
}
