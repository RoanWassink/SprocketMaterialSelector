using BepInEx;
using Sprocket.TechTrees;
using Sprocket.UI;
using Sprocket.Vehicles.PlateStructures;
using Sprocket.Vehicles;

namespace SprocketMaterialSelector;

internal static class ResponseUi
{
    internal static ArmourResponseCatalogue? Catalogue;
    internal static TooltipPreconditioning? TooltipPreconditioningData;
    private static readonly HashSet<string> warnings = new();
    private static readonly HashSet<string> CandidateIds = new(StringComparer.Ordinal)
    { "cwepGlassTextolite", "cwepNeraCassette", "cwepLightEraCassette", "cwepPassiveComposite", "cwepHeavyEraCassette" };

    internal static void Reload()
    {
        Catalogue = null;
        TooltipPreconditioningData = null;
        var path = Path.Combine(Paths.ConfigPath, "sprocket.armour.responses.json");
        if (!File.Exists(path)) return; // Existing custom settings are never rewritten.
        try
        {
            var json = File.ReadAllText(path);
            var parsed = ArmourResponses.Parse(json);
            var tooltipData = TooltipPreconditioning.FromValidatedCatalogue(json);
            Catalogue = parsed;
            TooltipPreconditioningData = tooltipData;
            Plugin.ModLog.LogInfo($"Armour responses: {Catalogue.Responses.Count} candidate descriptions; impact switch={Catalogue.Enabled}. ShellSelector owns impact/state, not this plugin.");
        }
        catch (Exception ex)
        {
            if (warnings.Add(ex.Message)) Plugin.ModLog.LogWarning($"Armour response catalogue ignored; ordinary passive materials retained: {ex.Message}");
        }
    }

    private static bool ShellConsumerLoaded()
    {
        try
        {
            var loader = BepInEx.Unity.IL2CPP.IL2CPPChainloader.Instance;
            if (loader == null) return false;
            if (loader.Plugins.TryGetValue("sprocket.shellselector", out var canonical) && canonical.Instance != null) return true;
            // Compatibility with an existing Shell installation; never the primary ID.
            return loader.Plugins.TryGetValue("nl.roan.sprocket.shellselector", out var legacy) && legacy.Instance != null;
        }
        catch (Exception ex) { if (warnings.Add("shell-presence")) Plugin.ModLog.LogWarning("Shell consumer presence unavailable: " + ex.Message); return false; }
    }

    internal static string Tooltip(PlateStructure plate, string materialId, string label)
    {
        var response = Find(materialId);
        var compatible = response == null || ArmourResponses.PassiveMatches(response, plate.armourDensity,
            plate.damageModelParameters.RhaFactor, plate.damageModelParameters.SpallFactor);
        return MaterialTooltip.Build(materialId, label,
            new(plate.damageModelParameters.RhaFactor, plate.armourDensity,
                plate.damageModelParameters.SpallFactor, plate.armourCostMultiplier),
            Catalogue, TooltipPreconditioningData, ShellConsumerLoaded(), EraAllowed(plate, materialId), compatible);
    }

    internal static ArmourResponse? Find(string id) => Catalogue?.Responses.FirstOrDefault(r => r.CompatibleMaterialIds.Contains(id));
    private sealed record Context(string DesignDate, string? NativeEra, string TechDate, bool HasTech, bool HasKnownVehicleContext);
    private static readonly HashSet<string> availabilitySnapshots = new(StringComparer.Ordinal);
    private static Context ReadContext(PlateStructure plate)
    {
        var designDate = "unavailable";
        string? eraName = null;
        var techDate = "unavailable";
        var hasTech = false;
        var hasContext = false;
        try
        {
            var vehicle = plate.Vehicle;
            var design = vehicle?.DesignInfo;
            if (design != null)
            {
                var date = design.Date;
                // Calendar validation only: no feature floor, era-name rule or horizon.
                if (!MaterialAvailability.HasValidCalendarDate(date.Year, date.Month, date.Day))
                    throw new InvalidOperationException("Invalid native vehicle date.");
                designDate = date.ToString();
                hasContext = true;
                try { eraName = VehicleClassifications.GetEra(date)?.Name; }
                catch (Exception ex) { if (warnings.Add("era-name-read")) Plugin.ModLog.LogWarning("Armour diagnostic era name unavailable: " + ex.Message); }
            }
            var tech = vehicle?.Tech;
            if (tech != null)
            {
                var date = tech.Date;
                if (!MaterialAvailability.HasValidCalendarDate(date.Year, date.Month, date.Day))
                    throw new InvalidOperationException("Invalid native technology frame date.");
                techDate = date.ToString();
                hasTech = true;
            }
        }
        catch (Exception ex)
        {
            hasContext = false;
            hasTech = false;
            if (warnings.Add("context-read")) Plugin.ModLog.LogWarning("Armour native technology context unavailable: " + ex.Message);
        }
        return new(designDate, eraName, techDate, hasTech, hasContext);
    }

    internal static string NativeDensityLabel(PlateStructure plate, string materialId)
    {
        try
        {
            if (plate.Vehicle?.Tech?.TryGetTech(materialId, out var tech) == true && tech != null)
            {
                var density = tech.GetFloat("density", float.NaN);
                if (float.IsFinite(density) && density > 0) return density.ToString("0") + " kg/m³";
            }
        }
        catch (Exception ex) { if (warnings.Add("recipe-read")) Plugin.ModLog.LogWarning("Armour native recipe display unavailable: " + ex.Message); }
        return "native recipe unavailable";
    }

    internal static float? NativeRequestedCost(PlateStructure plate, string materialId)
    {
        try
        {
            if (plate.Vehicle?.Tech?.TryGetTech(materialId, out var tech) == true && tech != null)
            {
                var cost = tech.GetFloat("costMultiplier", float.NaN);
                if (float.IsFinite(cost) && cost >= 0) return cost;
            }
        }
        catch (Exception ex) { if (warnings.Add("recipe-read")) Plugin.ModLog.LogWarning("Armour native recipe display unavailable: " + ex.Message); }
        return null;
    }

    internal static bool EraAllowed(PlateStructure plate, string materialId)
    {
        var context = ReadContext(plate);
        try
        {
            var inFrame = context.HasTech && plate.Vehicle.Tech.TryGetTech(materialId, out _);
            return MaterialAvailability.Evaluate(context.HasKnownVehicleContext, context.HasTech, inFrame) == MaterialAvailabilityStatus.Available;
        }
        catch (Exception ex)
        {
            if (warnings.Add("tech-read")) Plugin.ModLog.LogWarning("Armour availability lookup unavailable: " + ex.Message);
            return false;
        }
    }

    internal static void DescribeAvailability(IGUIElementDrawer ui, PlateStructure plate)
    {
        var context = ReadContext(plate);
        var details = new List<string>();
        var missing = new List<string>();
        foreach (var id in CandidateIds)
        {
            var index = MaterialDatabase.Materials.FindIndex(m => m.Id == id);
            var inFrame = false;
            try { inFrame = context.HasTech && plate.Vehicle.Tech.TryGetTech(id, out _); }
            catch (Exception ex) { if (warnings.Add("tech-read")) Plugin.ModLog.LogWarning("Armour availability lookup unavailable: " + ex.Message); }
            var status = MaterialAvailability.Evaluate(context.HasKnownVehicleContext, context.HasTech, inFrame);
            details.Add($"{id}:catalogIndex={index},nativeTech={inFrame},status={status}");
            if (index < 0 || status != MaterialAvailabilityStatus.Available) missing.Add(MaterialAvailability.Label(id, id));
        }
        var snapshot = $"plate={plate.Pointer},selected={plate.armourTechID},designDate={context.DesignDate},nativeEra={context.NativeEra ?? "unknown"},techDate={context.TechDate},knownVehicleContext={context.HasKnownVehicleContext},responseEnabled={Catalogue?.Enabled.ToString() ?? "no catalogue"}; " + string.Join("; ", details);
        if (availabilitySnapshots.Count < 64 && availabilitySnapshots.Add(snapshot))
            Plugin.ModLog.LogInfo("[Armour availability] " + snapshot);
        if (missing.Count == 0) return;
        if (!context.HasKnownVehicleContext || !context.HasTech)
            ui.InfoField("Armour choices are currently unavailable.", 1);
        else ui.InfoField("Some armour is unavailable for this era or installation.", 1);
    }

    internal static void Describe(IGUIElementDrawer ui, PlateStructure plate)
    {
        var id = plate.armourTechID;
        var response = Find(id);
        var compatible = response == null || ArmourResponses.PassiveMatches(response, plate.armourDensity,
            plate.damageModelParameters.RhaFactor, plate.damageModelParameters.SpallFactor);
        var summary = MaterialTooltip.Summary(id,
            new(plate.damageModelParameters.RhaFactor, plate.armourDensity,
                plate.damageModelParameters.SpallFactor, plate.armourCostMultiplier),
            Catalogue, TooltipPreconditioningData, ShellConsumerLoaded(), EraAllowed(plate,id), compatible);
        foreach (var line in summary) ui.InfoField(line, line.Length > 55 ? 2 : 1);
    }
}
