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

[BepInPlugin("nl.roan.sprocket.materialselector", "Sprocket Material Selector", "0.3.1")]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource ModLog = null!;
    internal static ConfigEntry<bool> SectionOpen = null!;

    public override void Load()
    {
        ModLog = Log;
        SectionOpen = Config.Bind(
            "UI",
            "Armour material section open",
            true,
            "Whether the Armour material section in plate-structure panels starts open.");

        try
        {
            MaterialDatabase.Reload();
            var harmony = new Harmony("nl.roan.sprocket.materialselector");
            harmony.PatchAll(typeof(MaterialSelectorPanel));

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
    internal string Label { get; init; } = "";
    internal string SourceFile { get; init; } = "";
    internal float RhaFactor { get; init; }
    internal float Density { get; init; }
    internal float SpallFactor { get; init; }
    internal float CostMultiplier { get; init; }
}

internal static class MaterialDatabase
{
    internal static readonly List<ArmourMaterial> Materials = new();
    internal static Il2CppSystem.Collections.Generic.List<string> Labels { get; private set; } = new();

    internal static void Reload()
    {
        Materials.Clear();
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
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) ||
                    !root.TryGetProperty("properties", out var properties) ||
                    !properties.TryGetProperty("rhaFactor", out var rhaElement))
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

                Materials.Add(new ArmourMaterial
                {
                    Id = id,
                    Label = FriendlyName(Path.GetFileNameWithoutExtension(file)),
                    SourceFile = file,
                    RhaFactor = rha,
                    Density = density,
                    SpallFactor = spall,
                    CostMultiplier = cost
                });
            }
            catch (Exception ex)
            {
                Plugin.ModLog.LogDebug(
                    $"Ignoring Technology file '{Path.GetFileName(file)}': {ex.Message}");
            }
        }

        // One entry per technology id. Prefer vanilla/common materials first.
        var deduped = Materials
            .GroupBy(m => m.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        Materials.Clear();
        Materials.AddRange(deduped);

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
                var selectedIndex = MaterialDatabase.Materials.FindIndex(
                    m => string.Equals(m.Id, currentId, StringComparison.Ordinal));

                if (selectedIndex < 0)
                    selectedIndex = 0;

                ui.Dropdown(
                    "Material",
                    MaterialDatabase.Labels.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(),
                    selectedIndex,
                    Ui.IntCallback(index =>
                    {
                        Ui.Guard("Set armour material", () =>
                        {
                            ApplyMaterial(component, index);

                            // Like QoL's inspector features: explicitly request a redraw,
                            // otherwise the old text can remain on screen.
                            __instance.RequestRedraw();
                        });
                    }),
                    "Armour technology for this entire plate structure. " +
                    "Entries are read automatically from Sprocket_Data/StreamingAssets/Technology.");

                var current = MaterialDatabase.Materials[
                    Math.Clamp(selectedIndex, 0, MaterialDatabase.Materials.Count - 1)];

                ui.InfoField(
                    $"{current.RhaFactor:0.##}x RHA | " +
                    $"{current.Density:0} kg/mÂ³ | " +
                    $"spall {current.SpallFactor:0.####}",
                    2);

                var tip = new UITooltip(
                    "Reload armour materials",
                    "Rescans Sprocket_Data/StreamingAssets/Technology without restarting the game.");

                ui.Button(
                    "Reload armour materials",
                    Ui.Callback(() =>
                    {
                        MaterialDatabase.Reload();
                        __instance.RequestRedraw();
                    }),
                    ref tip);
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
                    $"Vehicle.Mass {before.VehicleMass:0.###} -> {__instance.Mass:0.###}");

                if (component.armourTechID != before.MaterialId ||
                    Math.Abs(component.armourDensity - before.Density) > 0.01f ||
                    Math.Abs(cachedMass - component.GetMass(MassType.Everything)) > 0.01f)
                    Plugin.ModLog.LogWarning("Vanilla build did not retain the selected material or synchronize CachedMass.");
            }
        });
}
