using BepInEx;
using Sprocket.TechTrees;
using Sprocket.UI;
using Sprocket.Vehicles.PlateStructures;
using Sprocket.Vehicles;

namespace SprocketMaterialSelector;

internal static class ResponseUi
{
    internal static ArmourResponseCatalogue? Catalogue;
    private static readonly HashSet<string> warnings = new();
    private static readonly HashSet<string> CandidateIds = new(StringComparer.Ordinal)
    { "cwepGlassTextolite", "cwepNeraCassette", "cwepLightEraCassette", "cwepPassiveComposite", "cwepHeavyEraCassette" };

    internal static void Reload()
    {
        Catalogue = null;
        var path = Path.Combine(Paths.ConfigPath, "sprocket.armour.responses.json");
        if (!File.Exists(path)) return; // Existing custom settings are never rewritten.
        try
        {
            Catalogue = ArmourResponses.Parse(File.ReadAllText(path));
            Plugin.ModLog.LogInfo($"Armour responses: {Catalogue.Responses.Count} candidate descriptions; impact switch={Catalogue.Enabled}. ShellSelector owns impact/state, not this plugin.");
        }
        catch (Exception ex)
        {
            if (warnings.Add(ex.Message)) Plugin.ModLog.LogWarning($"Armour response catalogue ignored; ordinary passive materials retained: {ex.Message}");
        }
    }

    internal static ArmourResponse? Find(string id) => Catalogue?.Responses.FirstOrDefault(r => r.CompatibleMaterialIds.Contains(id));
    private sealed record Context(string DesignDate, string? NativeEra, string TechDate, bool HasTech, bool? PostwarDateAllowed);
    private static readonly HashSet<string> availabilitySnapshots = new(StringComparer.Ordinal);
    private static Context ReadContext(PlateStructure plate)
    {
        var designDate = "unavailable";
        string? eraName = null;
        var techDate = "unavailable";
        var hasTech = false;
        bool? postwarDateAllowed = null;
        try
        {
            var design = plate.Vehicle?.DesignInfo;
            if (design != null)
            {
                designDate = design.Date.ToString();
                var eras = VehicleClassifications.eras;
                if (eras != null && eras.Length > 0)
                {
                    var starts = new DateTime[eras.Length];
                    var valid = true;
                    for (var i = 0; i < eras.Length; i++)
                    {
                        if (eras[i] == null) { valid = false; break; }
                        var date = eras[i].StartDate;
                        starts[i] = new DateTime(date.Year, date.Month, date.Day);
                    }
                    if (valid)
                    {
                        var date = design.Date;
                        postwarDateAllowed = MaterialEraPolicy.Evaluate(new DateTime(date.Year, date.Month, date.Day), starts);
                    }
                }
                // Name is diagnostic only; a failed name lookup cannot override a valid date.
                try { eraName = VehicleClassifications.GetEra(design.Date)?.Name; }
                catch (Exception ex) { if (warnings.Add("era-name-read")) Plugin.ModLog.LogWarning("Armour diagnostic era name unavailable: " + ex.Message); }
            }
            var tech = plate.Vehicle?.Tech;
            hasTech = tech != null;
            if (tech != null) techDate = tech.Date.ToString();
        }
        catch (Exception ex)
        {
            if (warnings.Add("era-read")) Plugin.ModLog.LogWarning("Armour availability context unavailable: " + ex.Message);
        }
        return new(designDate, eraName, techDate, hasTech, postwarDateAllowed);
    }

    internal static bool EraAllowed(PlateStructure plate, string materialId)
    {
        if (!CandidateIds.Contains(materialId) && Find(materialId) == null) return true;
        var context = ReadContext(plate);
        try
        {
            var inFrame = context.HasTech && plate.Vehicle.Tech.TryGetTech(materialId, out _);
            return MaterialAvailability.Evaluate(context.PostwarDateAllowed, context.HasTech, inFrame) == MaterialAvailabilityStatus.Available;
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
            var status = MaterialAvailability.Evaluate(context.PostwarDateAllowed, context.HasTech, inFrame);
            details.Add($"{id}:catalogIndex={index},nativeTech={inFrame},status={status}");
            if (index < 0 || status != MaterialAvailabilityStatus.Available) missing.Add(MaterialAvailability.Label(id, id));
        }
        var snapshot = $"plate={plate.Pointer},selected={plate.armourTechID},designDate={context.DesignDate},nativeEra={context.NativeEra ?? "unknown"},techDate={context.TechDate},postwarDateAllowed={context.PostwarDateAllowed?.ToString() ?? "unknown"},responseEnabled={Catalogue?.Enabled.ToString() ?? "no catalogue"}; " + string.Join("; ", details);
        if (availabilitySnapshots.Count < 64 && availabilitySnapshots.Add(snapshot))
            Plugin.ModLog.LogInfo("[Armour availability] " + snapshot);
        if (missing.Count == 0) return;
        if (context.PostwarDateAllowed == null) ui.InfoField("Vehicle date or era timeline unavailable: modern armour choices are hidden.", 2);
        else if (context.PostwarDateAllowed != true) ui.InfoField("Modern armour requires a vehicle design dated 1945-09-03 or later.", 2);
        else ui.InfoField("Some modern armour is missing from this vehicle's available technology or material files.", 2);
        ui.InfoField("Unavailable: " + string.Join(", ", missing), 2);
    }

    internal static void Describe(IGUIElementDrawer ui, PlateStructure plate)
    {
        var response = Find(plate.armourTechID);
        if (response == null) return;
        ui.InfoField($"Response: {response.ResponseId} | Postwar candidate | provisional metadata {response.HistoricalDate}", 2);
        var compatible = ArmourResponses.PassiveMatches(response, plate.armourDensity,
            plate.damageModelParameters.RhaFactor, plate.damageModelParameters.SpallFactor);
        if (!compatible)
        {
            ui.InfoField("Custom material differs from this preset: passive protection only.", 2);
            if (warnings.Add("physical:" + plate.armourTechID)) Plugin.ModLog.LogWarning("Response physical mismatch for " + plate.armourTechID + "; preserve custom data and require passive fallback in ShellSelector.");
        }
        ui.InfoField(Catalogue!.Enabled && compatible && EraAllowed(plate, plate.armourTechID)
            ? "Experimental response enabled; requires Shell Selector."
            : "Response disabled: passive protection only.", 2);
        ui.InfoField(response.Geometry.Mode == "declaredCassette"
            ? "Internal laminate approximation; mass covers the complete cassette."
            : "Requires measured upstream steel traversal and real downstream gap.", 2);
        ui.InfoField($"Normal thickness range: {response.Geometry.MinNormalThicknessMm:0}-{response.Geometry.MaxNormalThicknessMm:0} mm", 2);
        if (response.Kind == "nera") ui.InfoField("Scaled steel / rubber / steel thirds; bare elastomer is not NERA.", 2);
        if (response.Kind == "heavyEra")
        {
            ui.InfoField("Kontakt-5-inspired gameplay approximation; not measured historical performance.", 2);
            ui.InfoField($"HEAT / APFSDS share one use per {response.CellPitchM:0.00} m cell; spent cells retain passive protection.", 2);
            ui.InfoField("Requires the heavy-ERA Shell Selector adapter and a game restart. No tandem protection is modelled.", 2);
        }
        if (response.Kind == "lightEra") ui.InfoField($"Single-charge HEAT: first hit per {response.CellPitchM:0.00} m cell. No APFSDS or tandem benefit.", 2);
        ui.InfoField($"Native armour material cost: {plate.GetCost(Sprocket.Vehicles.MassType.Armour, Sprocket.Vehicles.CostType.Material):0.00} | assembly: {plate.GetCost(Sprocket.Vehicles.MassType.Armour, Sprocket.Vehicles.CostType.Assembly):0.00}", 2);
    }
}
