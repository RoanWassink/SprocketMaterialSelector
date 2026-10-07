using System.Globalization;
using System.Text.Json;

namespace SprocketMaterialSelector;

// Presentation only. These values never change the material or the shell consumer.
internal sealed record TooltipPreconditioning(IReadOnlyList<string> MaterialIds,
    double MinimumThickness, double MinimumAngle, double MaximumAngle,
    double FullAngle, double FullThickness, double PerLayerCap, double TotalCap)
{
    internal static TooltipPreconditioning FromValidatedCatalogue(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var p = doc.RootElement.GetProperty("preconditioning");
        double N(string name) => p.GetProperty(name).GetDouble();
        return new(p.GetProperty("steelMaterialIds").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            N("minSteelThicknessMm"), N("minObliquityDegrees"), N("maxObliquityDegrees"),
            N("angleFullDegrees"), N("thicknessFullMm"), N("disturbancePerLayerCap"), N("cumulativeDisturbanceCap"));
    }
}

internal sealed record TooltipPassive(double RhaFactor, double Density, double SpallFactor, double EffectiveCost);

internal static class MaterialTooltip
{
    private static string N(double value) => double.IsFinite(value)
        ? value.ToString("0.###", CultureInfo.InvariantCulture) : "unavailable";
    private static string Percent(double fraction) => N(fraction * 100) + "%";
    private static string Curve(IReadOnlyList<CurvePoint> curve, string unit) =>
        string.Join(", ", curve.Select(p => N(p.Input) + unit + "=" + Percent(p.Weight)));

    private static string BaseSummary(TooltipPassive p)
    {
        if (!double.IsFinite(p.RhaFactor) || !double.IsFinite(p.Density)) return "Base armour protection.";
        if (p.Density < 7850 && p.RhaFactor < 1) return "Lighter than steel; needs more thickness than RHA.";
        if (p.RhaFactor > 1 && p.Density > 7850) return "More protection per thickness than RHA, but heavier.";
        if (p.RhaFactor > 1) return "More protection than RHA at the same thickness.";
        if (p.RhaFactor < 1) return "Less protection than RHA at the same thickness.";
        return "General-purpose armour protection.";
    }

    private static (double Heat, double Rod, bool AfterSteel, bool HeadOnZero) Strengths(
        ArmourResponse r, ArmourResponseCatalogue c, TooltipPreconditioning? p)
    {
        var heat = r.Geometry.Mode == "resolvedLayers" ? 0 : Math.Min(c.MaximumAdditionalHeatLoss,
            r.Geometry.AngularCurve.Max(x => x.Weight) * (1-r.Calibration.HeatRetention));
        if (r.Kind == "lightEra") return (heat,0,false,true);
        var cap = p?.TotalCap ?? 0;
        var disturbed = r.Calibration.IntactRodRetention - cap *
            (r.Calibration.IntactRodRetention-r.Calibration.DisturbedRodRetention);
        var afterSteel = disturbed < r.Calibration.IntactRodRetention;
        var curve = r.Geometry.KineticAngularCurve ?? r.Geometry.AngularCurve;
        var rod = Math.Min(c.MaximumAdditionalKineticLoss,curve.Max(x=>x.Weight)*
            (1-Math.Min(r.Calibration.IntactRodRetention,disturbed)));
        // Native curves interpolate. Check the actual head-on point, not a kind label.
        var headOn = curve[0].Input > 0 || curve[0].Input == 0 && curve[0].Weight == 0;
        return (heat,rod,afterSteel,headOn);
    }

    private static bool Disabled(ArmourResponseCatalogue? c, bool shell, bool eligible, bool compatible) =>
        c == null || !c.Enabled || !shell || !eligible || !compatible;

    internal static IReadOnlyList<string> Summary(string materialId, TooltipPassive passive,
        ArmourResponseCatalogue? catalogue, TooltipPreconditioning? pre,
        bool shellLoaded, bool nativeEligible, bool compatible)
    {
        var r = catalogue?.Responses.FirstOrDefault(x=>x.CompatibleMaterialIds.Contains(materialId));
        var primes = pre?.MaterialIds.Contains(materialId) == true;
        if (r == null && !primes) return new[] { BaseSummary(passive) };
        if (Disabled(catalogue,shellLoaded,nativeEligible,compatible)) return new[] { "Extra protection disabled." };
        if (r == null) return new[] { "Angled steel can help armour behind it stop APFSDS." };
        var s=Strengths(r,catalogue!,pre);
        var heat=s.Heat>0 ? "HEAT: up to " + Percent(s.Heat) + " less penetration." : "HEAT: base armour only.";
        var rod=s.Rod>0 ? "APFSDS: up to " + Percent(s.Rod) + " less penetration" +
            (s.AfterSteel ? " after steel." : " at favourable angles.") : "APFSDS: base armour only.";
        return new[] { heat + " " + rod,
            "Use " + N(r.Geometry.MinNormalThicknessMm) + "-" + N(r.Geometry.MaxNormalThicknessMm) + " mm thickness." };
    }

    internal static string Build(string materialId, string label, TooltipPassive passive,
        ArmourResponseCatalogue? catalogue, TooltipPreconditioning? pre,
        bool shellLoaded, bool nativeEligible, bool compatible)
    {
        var lines=new List<string> { label.Length>80 ? label[..77]+"..." : label };
        var r=catalogue?.Responses.FirstOrDefault(x=>x.CompatibleMaterialIds.Contains(materialId));
        var primes=pre?.MaterialIds.Contains(materialId)==true;
        if (r==null && !primes) { lines.Add(BaseSummary(passive)); lines.Add("HEAT and APFSDS use this material's base protection."); }
        else if (Disabled(catalogue,shellLoaded,nativeEligible,compatible))
        { lines.Add("Extra protection disabled."); lines.Add(BaseSummary(passive)); }
        else if (r==null)
        {
            lines.Add(BaseSummary(passive));
            lines.Add("Angled plates can help layers behind them stop APFSDS.");
            lines.Add("Use at least " + N(pre!.MinimumThickness) + " mm, angled " + N(pre.MinimumAngle) + "-" + N(pre.MaximumAngle) + " degrees.");
        }
        else
        {
            var s=Strengths(r,catalogue!,pre);
            lines.Add(s.Heat>0 ? "HEAT: up to " + Percent(s.Heat) + " less penetration." : "HEAT: base armour only.");
            lines.Add(s.Rod>0 ? "APFSDS: up to " + Percent(s.Rod) + " less penetration" +
                (s.AfterSteel ? " after steel." : " at favourable angles.") : "APFSDS: base armour only.");
            if (s.Rod>0 && s.HeadOnZero) lines.Add("No extra APFSDS protection head-on.");
            lines.Add("Use " + N(r.Geometry.MinNormalThicknessMm) + "-" + N(r.Geometry.MaxNormalThicknessMm) + " mm thickness.");
            if (r.Geometry.Mode=="resolvedLayers") lines.Add("Leave a " + N(r.Geometry.MinMeasuredGapMm) + "-" + N(r.Geometry.MaxMeasuredGapMm) + " mm gap behind steel.");
            if (r.Kind is "lightEra" or "heavyEra") lines.Add("ERA works once per area; afterwards only base armour remains.");
        }
        return string.Join("\n",lines);
    }

    internal static string Status(ArmourResponseCatalogue? catalogue, bool shellLoaded, bool nativeEligible, bool compatible)
    {
        if (catalogue == null) return "No valid response catalogue loaded; native passive armour only.";
        if (!catalogue.Enabled) return "Additional responses disabled in configuration; native passive armour only.";
        if (!shellLoaded) return "Shell Selector is not loaded; native passive armour only.";
        if (!nativeEligible) return "Material is unavailable in this native technology context; no additional response.";
        if (!compatible) return "Active material recipe differs from this response preset; native passive armour only.";
        return "Additional response configured; compatible Shell Selector and eligible impacts required. Runtime activation is not verified here.";
    }

    internal static string BuildTechnical(string materialId, string label, TooltipPassive passive,
        ArmourResponseCatalogue? catalogue, TooltipPreconditioning? preconditioning,
        bool shellLoaded, bool nativeEligible, bool compatible)
    {
        var lines = new List<string> {
            label,
            "Passive armour factor: " + N(passive.RhaFactor) + "x RHA (native rhaFactor).",
            "Density: " + N(passive.Density) + " kg/m3; effective armour cost multiplier: " + N(passive.EffectiveCost) + "x.",
            "Native spallFactor: " + N(passive.SpallFactor) + " (a coefficient, not a damage percentage)."
        };
        var response = catalogue?.Responses.FirstOrDefault(r => r.CompatibleMaterialIds.Contains(materialId));
        var primesRod = preconditioning?.MaterialIds.Contains(materialId) == true;
        if (response == null)
        {
            lines.Add("HEAT / APFSDS: native passive protection; no loaded direct shell-response modifier for this material.");
            if (!primesRod) return string.Join("\n", lines);
        }
        lines.Add(Status(catalogue, shellLoaded, nativeEligible, compatible));
        if (primesRod)
        {
            var p = preconditioning!;
            lines.Add("Configured APFSDS preconditioning: successful traversal at >=" + N(p.MinimumThickness) +
                " mm normal thickness and " + N(p.MinimumAngle) + "-" + N(p.MaximumAngle) + " deg from normal can strengthen later layer responses.");
            lines.Add("Disturbance ramps to full at " + N(p.FullThickness) + " mm / " + N(p.FullAngle) +
                " deg; per-layer cap " + N(p.PerLayerCap) + "; total cap " + N(p.TotalCap) + ". No physical rod rotation is modelled.");
        }
        if (response == null) return string.Join("\n", lines);

        var c = response.Calibration;
        var g = response.Geometry;
        lines.Add("Configured values below are additional to native passive armour, not fixed millimetres of protection.");
        if (g.Mode == "resolvedLayers")
            lines.Add("HEAT: no added loss in the current resolved-layer rod adapter; only APFSDS records the required upstream steel traversal. Native passive protection remains.");
        else
            lines.Add("HEAT: remaining-penetration retention " + N(c.HeatRetention) + "x; peak angular extra loss " +
                Percent(g.AngularCurve.Max(p => p.Weight) * (1-c.HeatRetention)) + " before cumulative limits.");
        if (response.Kind == "lightEra")
            lines.Add("APFSDS: no additional kinetic protection from light ERA; native passive resistance remains.");
        else
        {
            var disturbanceCap = preconditioning?.TotalCap ?? 0;
            var disturbed = c.IntactRodRetention - disturbanceCap * (c.IntactRodRetention - c.DisturbedRodRetention);
            lines.Add("APFSDS: base retention " + N(c.IntactRodRetention) + "x without prior disturbance; " +
                N(disturbed) + "x at configured disturbance cap " + N(disturbanceCap) + ", before angular weighting and cumulative limits.");
        }
        lines.Add("Eligible normal thickness: " + N(g.MinNormalThicknessMm) + "-" + N(g.MaxNormalThicknessMm) + " mm.");
        lines.Add((g.Mode == "resolvedLayers" ? "Rod angle weights" : "HEAT angle weights") +
            " (degrees from plate normal): " + Curve(g.AngularCurve, "deg") + ".");
        if (response.Kind != "lightEra")
            lines.Add("APFSDS angle weights: " + Curve(g.KineticAngularCurve ?? g.AngularCurve, "deg") + ".");
        lines.Add("Curve weights interpolate between points; outside the curve there is no added response.");
        if (g.Mode == "resolvedLayers")
        {
            lines.Add("Requires actual upstream steel traversal and a measured gap of " + N(g.MinMeasuredGapMm) +
                "-" + N(g.MaxMeasuredGapMm) + " mm; gap weights: " + Curve(g.MeasuredGapCurve, "mm") + ".");
        }
        else lines.Add("Complete-cassette approximation; the material mass covers the cassette.");
        if (response.Kind is "lightEra" or "heavyEra")
            lines.Add("Finite ERA: " + N(response.CellPitchM) + " m cells; " +
                (response.Kind == "heavyEra" ? "HEAT and APFSDS share the same spent cell." : "HEAT uses the cell; no APFSDS benefit.") +
                " After the qualifying first hit, that cell retains passive protection only. No tandem protection is modelled.");
        lines.Add("Total extra-loss caps across layers: HEAT " + Percent(catalogue!.MaximumAdditionalHeatLoss) +
            "; kinetic " + Percent(catalogue.MaximumAdditionalKineticLoss) + ".");
        lines.Add("Only original HEAT / APFSDS projectiles receive these configured modifiers; other threats use native passive properties.");
        lines.Add("Values are gameplay configuration, not verified historical resistance. Restart after response-setting changes so Shell Selector reads the same catalogue.");
        return string.Join("\n", lines);
    }
}
