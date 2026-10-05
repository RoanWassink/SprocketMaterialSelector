namespace SprocketMaterialSelector;

// Modern armour policy mirrors Thermal postwar-candidate1. The saved MaxValue
// denotes selection of the actual last era, never a literal future vehicle year.
internal static class MaterialEraPolicy
{
    internal static readonly DateTime MinimumDate = new(1945, 9, 3);
    internal static bool? Evaluate(DateTime? vehicleDate, ReadOnlySpan<DateTime> eraStarts)
    {
        if (vehicleDate == null || eraStarts.Length == 0 || eraStarts[^1].Date == DateTime.MaxValue.Date) return null;
        for (int i = 1; i < eraStarts.Length; i++)
            if (eraStarts[i] <= eraStarts[i - 1]) return null;
        if (vehicleDate.Value.Date == DateTime.MaxValue.Date)
            return eraStarts[^1].Date >= MinimumDate;
        if (vehicleDate.Value.Date < MinimumDate || vehicleDate.Value.Date < eraStarts[0].Date) return false;
        return true;
    }
    internal static bool Allows(DateTime? vehicleDate, ReadOnlySpan<DateTime> eraStarts) => Evaluate(vehicleDate, eraStarts) == true;
}
