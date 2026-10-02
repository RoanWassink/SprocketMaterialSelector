using System;

namespace SprocketMaterialSelector;

internal readonly record struct MaterialBalanceResult(
    double ThicknessEfficiency, double WeightEfficiency,
    float RequestedCostMultiplier, double MinimumCostMultiplier,
    float EffectiveCostMultiplier, bool WasAdjusted, bool IsValid,
    string ValidationError);

internal static class MaterialBalance
{
    internal const double ReferenceDensity = 7850;
    internal const double ReferenceRhaFactor = 1;
    internal const double ReferenceCostMultiplier = 2;
    internal const double PerformanceExponent = 4;
    internal const double MinEqualProtectionCostRatio = 0.40;
    // Conservative headroom for float aggregation. Reject rather than clamp.
    internal const double MaxSupportedValue = 1e12;

    internal static MaterialBalanceResult Calculate(float rha, float density, float requested)
    {
        MaterialBalanceResult Invalid(string reason) =>
            new(0, 0, requested, 0, 0, false, false, reason);
        if (!float.IsFinite(rha) || rha <= 0 ||
            !float.IsFinite(density) || density <= 0 ||
            !float.IsFinite(requested) || requested < 0)
            return Invalid("RHA factor and density must be positive and finite; requested cost must be non-negative and finite.");

        var thickness = rha / ReferenceRhaFactor;
        var weight = thickness * ReferenceDensity / density;
        var ratio = Math.Max(MinEqualProtectionCostRatio,
            Math.Max(Math.Pow(thickness, PerformanceExponent), Math.Pow(weight, PerformanceExponent)));
        var minimum = ReferenceCostMultiplier * weight * ratio;
        var effective = Math.Max(requested, minimum);
        if (!double.IsFinite(effective) || effective > MaxSupportedValue ||
            effective < float.Epsilon)
            return Invalid("Calculated cost exceeds supported range.");
        // Never round the floor down during conversion to the game's float.
        var rounded = (float)effective;
        if (rounded < effective) rounded = MathF.BitIncrement(rounded);
        return new(thickness, weight, requested, minimum, rounded,
            minimum > requested, true, "");
    }

    internal static bool TryMaterialCost(float mass, float multiplier, float costPerKg, out float cost)
    {
        cost = 0;
        var total = (double)mass * multiplier * costPerKg;
        if (!float.IsFinite(mass) || mass < 0 || !float.IsFinite(multiplier) || multiplier < 0 ||
            !float.IsFinite(costPerKg) || costPerKg <= 0 || !double.IsFinite(total) ||
            total > MaxSupportedValue || total < 0 || (total > 0 && total < float.Epsilon))
            return false;
        cost = (float)total;
        if (cost < total) cost = MathF.BitIncrement(cost);
        return true;
    }
}
