using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Sprocket.UI;
using Sprocket.Vehicles;
using Sprocket.Vehicles.PlateStructures;
using Sprocket.Vehicles.PlateStructures.Design;
using UnityEngine.Events;

namespace SprocketMaterialSelector;

[BepInPlugin("sprocket.materialselector", "Sprocket Material Selector", "0.5.0")]
[BepInDependency("sprocket.jsoneditor")]
[BepInDependency("sprocket.shellselector", ">=0.13.0 <0.14.0")]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource ModLog = null!;
    internal static ConfigEntry<bool> SectionOpen = null!;

    public override void Load()
    {
        ModLog = Log;
        try
        {
            var migration = ConfigMigration.CopyLegacyIfNeeded(
                Path.Combine(Paths.ConfigPath, "nl.roan.sprocket.materialselector.cfg"),
                Path.Combine(Paths.ConfigPath, "sprocket.materialselector.cfg"));
            if (migration != ConfigMigrationResult.NoLegacyFile)
            {
                Config.Reload();
                if (migration == ConfigMigrationResult.Copied)
                    Log.LogInfo("Material settings migrated; the legacy configuration is retained as a backup.");
            }
        }
        catch (Exception ex)
        {
            Log.LogError("Material configuration migration failed; existing settings were left untouched. " + ex.Message);
            return;
        }
        SectionOpen = Config.Bind(
            "UI",
            "Armour material section open",
            true,
            "Whether the Armour material section in plate-structure panels starts open.");

        try
        {
            ResponseUi.Reload();
            MaterialDatabase.Reload();
            var harmony = new Harmony("sprocket.materialselector");
            harmony.PatchAll(typeof(MaterialSelectorPanel));
            harmony.PatchAll(typeof(RuntimeMaterialBalance));
            try { new Harmony("sprocket.materialselector.relikt.visual").PatchAll(typeof(ReliktPrototype)); new Harmony("sprocket.materialselector.relikt.icon").PatchAll(typeof(ReliktIcon)); EraPresetRegistry.Start(); ReliktSpentVisuals.Start(); new Harmony("sprocket.materialselector.relikt.spent").PatchAll(typeof(ReliktSpentVisuals)); }
            catch (Exception ex) { Log.LogWarning("Optional Relikt prototype unavailable: " + ex.Message); }

            Log.LogInfo(
                $"Sprocket Material Selector loaded. " +
                $"{MaterialDatabase.Materials.Count} armour materials found in Technology.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Sprocket Material Selector failed to load: {ex}");
        }
    }
}

internal sealed class ArmourMaterial
{
    internal string Id { get; init; } = "";
    internal string Label { get; set; } = "";
    internal string SourceFile { get; init; } = "";
    internal float RhaFactor { get; init; }
    internal float Density { get; init; }
    internal float SpallFactor { get; init; }
    internal float CostMultiplier { get; init; }
    internal MaterialBalanceResult Balance { get; init; }
}

internal static class MaterialDatabase
{
    internal static readonly List<ArmourMaterial> Materials = new();
    internal static readonly HashSet<string> RejectedIds = new(StringComparer.Ordinal);
    internal static Il2CppSystem.Collections.Generic.List<string> Labels { get; private set; } = new();

    internal static void Reload()
    {
        Materials.Clear();
        RejectedIds.Clear();
        Labels = new Il2CppSystem.Collections.Generic.List<string>();

        var technologyDir = Path.Combine(
            Paths.GameRootPath,
            "Sprocket_Data",
            "StreamingAssets",
            "Technology");

        if (!Directory.Exists(technologyDir))
            throw new DirectoryNotFoundException($"Technology folder not found: {technologyDir}");

        foreach (var file in Directory.EnumerateFiles(
                     technologyDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                using var doc = TechnologyReader.Parse(File.ReadAllText(file));
                var root = doc.RootElement;

                if (!TechnologyReader.TryMaterial(root, out var typeElement, out var properties, out var rhaElement))
                    continue;

                var id = typeElement.GetString();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                float Number(JsonElement e) =>
                    e.TryGetSingle(out var f) ? f : (float)e.GetDouble();

                var rha = Number(rhaElement);
                var density = properties.TryGetProperty("density", out var d) ? Number(d) : 0f;
                var spall = properties.TryGetProperty("spallFactor", out var s) ? Number(s) : 0f;
                var cost = properties.TryGetProperty("costMultiplier", out var c) ? Number(c) : 0f;
                var balance = MaterialBalance.Calculate(rha, density, cost);
                if (!balance.IsValid || !float.IsFinite(spall) || spall < 0)
                {
                    RejectedIds.Add(id);
                    Plugin.ModLog.LogWarning($"[Material Balance] Rejected {id}: rhaFactor={rha}, density={density}. " +
                        (balance.IsValid ? "Invalid spall factor." : balance.ValidationError));
                    continue;
                }
                var report = $"[Material Balance] {id}: requestedCost={cost:0.000}, " +
                    $"minimumCost={balance.MinimumCostMultiplier:0.000}, effectiveCost={balance.EffectiveCostMultiplier:0.000}, " +
                    $"thicknessEfficiency={balance.ThicknessEfficiency:0.000}, weightEfficiency={balance.WeightEfficiency:0.000}";
                if (balance.WasAdjusted) Plugin.ModLog.LogInfo(report);
                else Plugin.ModLog.LogDebug(report);

                Materials.Add(new ArmourMaterial
                {
                    Id = id,
                    Label = FriendlyName(Path.GetFileNameWithoutExtension(file)),
                    SourceFile = file,
                    RhaFactor = rha,
                    Density = density,
                    SpallFactor = spall,
                    CostMultiplier = cost,
                    Balance = balance
                });
            }
            catch (Exception ex)
            {
                Plugin.ModLog.LogWarning(
                    $"Ignoring Technology file '{Path.GetFileName(file)}': {ex.Message}");
            }
        }

        MaterialEditorRefresh.ApplyLabels(Materials);
        // One entry per technology id. Prefer vanilla/common materials first.
        var deduped = Materials
            .GroupBy(m => m.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        Materials.Clear();
        Materials.AddRange(deduped.Where(m => !RejectedIds.Contains(m.Id)));

        Materials.Sort((a, b) =>
        {
            static int Rank(string id) =>
                id == "rha" ? 0 :
                id == "sheetMetal" ? 1 :
                id == "sand" ? 2 : 3;

            var rank = Rank(a.Id).CompareTo(Rank(b.Id));
            return rank != 0
                ? rank
                : string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var material in Materials)
        {
            var details = material.Density > 0
                ? $"{material.Label}  ({material.RhaFactor:0.##}x RHA, {material.Density:0} kg/mÂ³)"
                : $"{material.Label}  ({material.RhaFactor:0.##}x RHA)";
            Labels.Add(details);
        }

        Plugin.ModLog.LogInfo(
            "Armour materials: " +
            string.Join(", ", Materials.Select(m => $"{m.Label} [{m.Id}]")));
    }

    private static string FriendlyName(string value)
    {
        value = value.Replace('_', ' ').Replace('-', ' ');
        value = Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");
        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}

internal static class Ui
{
    internal static UnityAction Callback(Action action) =>
        DelegateSupport.ConvertDelegate<UnityAction>(action)!;

    internal static UnityAction<int> IntCallback(Action<int> action) =>
        DelegateSupport.ConvertDelegate<UnityAction<int>>(action)!;

    internal static Il2CppSystem.Action<bool> BoolCallback(Action<bool> action) =>
        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>(action)!;

    internal static void Guard(string feature, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Plugin.ModLog.LogError($"{feature}: {ex}");
        }
    }
}

[HarmonyPatch]
internal static class MaterialSelectorPanel
{
    private static bool loggedOnGuiHit;
    private static bool loggedNoUiDrawer;
    private sealed record PendingMassUpdate(
        PlateStructure Component, string MaterialId, float Density,
        float ArmourMass, float CachedMass, float VehicleMass);
    private static readonly Dictionary<IntPtr, PendingMassUpdate> pendingMassUpdates = new();
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlateStructureEditor), nameof(PlateStructureEditor.OnGUI))]
    private static void Draw(PlateStructureEditor __instance, IGUILayout layout) =>
        Ui.Guard("Armour material", () =>
        {
            if (MaterialDatabase.Materials.Count == 0)
                return;

            if (!loggedOnGuiHit)
            {
                loggedOnGuiHit = true;
                Plugin.ModLog.LogInfo("PlateStructureEditor.OnGUI hook reached.");
            }

            var component = __instance.Component;
            if (component == null)
                return;

            var ui = layout.TryCast<IGUIElementDrawer>();
            if (ui == null)
            {
                if (!loggedNoUiDrawer)
                {
                    loggedNoUiDrawer = true;
                    Plugin.ModLog.LogWarning("PlateStructureEditor.OnGUI received an IGUILayout that cannot be cast to IGUIElementDrawer.");
                }
                return;
            }

            // QoL uses this same pattern: finish the game's current foldout,
            // then add a native Sprocket section to the inspector.
            layout.EndAllDropdowns();
            layout.BeginDropdown(
                "Armour material",
                Plugin.SectionOpen.Value,
                Ui.BoolCallback(open => Plugin.SectionOpen.Value = open));

            try
            {
                var currentId = ReadArmourTechId(component) ?? "rha";
                ResponseUi.DescribeAvailability(ui, component);
                var visible = MaterialDatabase.Materials.Where(m => ResponseUi.EraAllowed(component, m.Id)).ToList();
                if (visible.Count == 0) return;
                var labels = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (var material in visible) labels.Add(MaterialAvailability.Label(material.Id, material.Label) + " (" + ResponseUi.NativeDensityLabel(component, material.Id) + ")");
                var selectedIndex = visible.FindIndex(
                    m => string.Equals(m.Id, currentId, StringComparison.Ordinal));

                if (selectedIndex < 0)
                    selectedIndex = 0;

                var tooltipLabel = MaterialDatabase.Materials.FirstOrDefault(m => m.Id == currentId)?.Label ?? currentId;
                var materialTooltip = ResponseUi.Tooltip(component, currentId, MaterialAvailability.Label(currentId, tooltipLabel));
                ui.Dropdown(
                    "Material",
                    labels.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(),
                    selectedIndex,
                    Ui.IntCallback(index =>
                    {
                        Ui.Guard("Set armour material", () =>
                        {
                            if (index < 0 || index >= visible.Count) return;
                            var materialId = visible[index].Id;
                            if (!ResponseUi.EraAllowed(component, materialId)) return;
                            ApplyMaterial(component, MaterialDatabase.Materials.FindIndex(m => m.Id == materialId));

                            // Like QoL's inspector features: explicitly request a redraw,
                            // otherwise the old text can remain on screen.
                            __instance.RequestRedraw();
                        });
                    }),
                    materialTooltip);
                var editTip=new UITooltip("Material editor", "Create and edit materials, special behavior and protection settings.");
                ui.Button("Edit materials",Ui.Callback(()=>SprocketJsonEditor.JsonEditor.Open(new MaterialEditorSession(()=>{MaterialEditorRefresh.Reload(component);__instance.RequestRedraw();}))),ref editTip);
                ui.InfoField("Protection", materialTooltip, 1, UnityEngine.Color.white);

                var current = visible[Math.Clamp(selectedIndex, 0, visible.Count - 1)];

                if (current.Id == currentId)
                {
                    // The active native component recipe is authoritative, not an
                    // arbitrary duplicate file discovered for this technology ID.
                    var nativeRequest = ResponseUi.NativeRequestedCost(component, currentId);
                    var requested = ArmourResponses.RequestedPrice(nativeRequest ?? component.armourCostMultiplier,
                        ResponseUi.Find(currentId));
                    var active = MaterialBalance.Calculate(component.damageModelParameters.RhaFactor,
                        component.armourDensity, requested);
                    ui.InfoField($"{component.damageModelParameters.RhaFactor:0.##}x RHA | " +
                        $"{component.armourDensity:0} kg/m³", 2);
                    if (active.IsValid) ui.InfoField($"Weight efficiency: {active.WeightEfficiency:0.##}x | " +
                        $"Cost: {component.armourCostMultiplier:0.00}x", 2);
                    if (nativeRequest != null && active.IsValid && active.WasAdjusted)
                        ui.InfoField($"Requested: {requested:0.00}x | Balanced minimum applied", 2);
                }
                else ui.InfoField("This material is unavailable for the vehicle's current era.", 1);

                ResponseUi.Describe(ui, component);


            }
            finally
            {
                // Several native editors can share one inspector layout.
                // Never leave our foldout open for the next editor.
                layout.EndAllDropdowns();
            }
        });

    private static string? ReadArmourTechId(PlateStructure component) =>
        component.armourTechID;

    private static void ApplyMaterial(PlateStructure component, int index)
    {
        if (index < 0 || index >= MaterialDatabase.Materials.Count)
            return;

        var material = MaterialDatabase.Materials[index];
        var blueprint = component.Blueprint;
        if (blueprint == null)
            throw new InvalidOperationException("Plate structure has no blueprint.");

        var oldDensity = component.armourDensity;
        var oldArmourMass = component.GetMass(MassType.Armour);
        var oldCachedMass = component.CachedMass;
        var oldVehicleMass = component.Vehicle.Mass;

        // Build reads this blueprint field; changing only the runtime technology
        // would let the next vanilla build restore the previous material.
        blueprint.armourTechID = material.Id;
        component.SetArmourMaterial(material.Id);
        component.blueprintRef.MarkModified();
        pendingMassUpdates[component.Pointer] = new PendingMassUpdate(
            component, material.Id, component.armourDensity,
            oldArmourMass, oldCachedMass, oldVehicleMass);

        // Confirmed vanilla route: OnAssignedMeshModified calls RequestRebuild,
        // which marks this component and raises PartConfiguration on its gateway.
        // The game's scheduled build updates armour mass, cached mass, cost and COM.
        component.RequestRebuild();

        Plugin.ModLog.LogInfo(
            $"Armour material -> {material.Label} [{material.Id}] | " +
            $"density {oldDensity:0.###} -> {component.armourDensity:0.###} kg/m³ | " +
            $"before build: armour mass {oldArmourMass:0.###}, " +
            $"CachedMass {oldCachedMass:0.###}, Vehicle.Mass {oldVehicleMass:0.###} | " +
            "vanilla rebuild queued (PartConfiguration).");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.Build))]
    private static void AfterVehicleBuild(Vehicle __instance) =>
        Ui.Guard("Verify armour mass after vanilla build", () =>
        {
            foreach (var item in pendingMassUpdates.ToArray())
            {
                var before = item.Value;
                var component = before.Component;
                if (component.Vehicle.Pointer != __instance.Pointer || component.RequiresRebuild)
                    continue;

                pendingMassUpdates.Remove(item.Key);
                var armourMass = component.GetMass(MassType.Armour);
                var cachedMass = component.CachedMass;
                Plugin.ModLog.LogInfo(
                    $"After vanilla build [{component.armourTechID}] | " +
                    $"armour mass {before.ArmourMass:0.###} -> {armourMass:0.###} | " +
                    $"CachedMass {before.CachedMass:0.###} -> {cachedMass:0.###} | " +
                    $"Vehicle.Mass {before.VehicleMass:0.###} -> {__instance.Mass:0.###} | " +
                    $"effectiveCostMultiplier={component.armourCostMultiplier:0.000}, " +
                    $"armourMaterialCost={component.GetCost(MassType.Armour, CostType.Material):0.###}");

                if (component.armourTechID != before.MaterialId ||
                    Math.Abs(component.armourDensity - before.Density) > 0.01f ||
                    Math.Abs(cachedMass - component.GetMass(MassType.Everything)) > 0.01f)
                    Plugin.ModLog.LogWarning("Vanilla build did not retain the selected material or synchronize CachedMass.");
            }
        });
}

















