namespace SprocketMaterialSelector;

internal enum MaterialAvailabilityStatus { Available, UnknownVehicleContext, MissingTechnologyFrame, NotInTechnologyFrame }

internal static class MaterialAvailability
{
    // Validate native metadata without adding a feature-specific date restriction.
    internal static bool HasValidCalendarDate(int year, int month, int day)
    {
        // Native TechDate supports year zero. Map its Gregorian calendar to year
        // 400 (the same leap-year cycle) solely for calendar validation.
        if (year < 0 || year > 9999) return false;
        try { _ = new DateTime(year == 0 ? 400 : year, month, day); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    internal static MaterialAvailabilityStatus Evaluate(bool hasKnownVehicleContext, bool hasTechnologyFrame, bool materialInFrame)
    {
        if (!hasKnownVehicleContext) return MaterialAvailabilityStatus.UnknownVehicleContext;
        if (!hasTechnologyFrame) return MaterialAvailabilityStatus.MissingTechnologyFrame;
        return materialInFrame ? MaterialAvailabilityStatus.Available : MaterialAvailabilityStatus.NotInTechnologyFrame;
    }

    internal static string Label(string id, string fallback) => id switch
    {
        "cwepHeavyEraCassette" => "Heavy ERA cassette (Kontakt-5-inspired; HEAT / APFSDS)",
        "cwepLightEraCassette" => "Light ERA cassette (HEAT; one use per cell)",
        "cwepNeraCassette" => "NERA sandwich cassette",
        "cwepGlassTextolite" => "Glass / textolite laminate",
        "cwepPassiveComposite" => "Passive composite cassette",
        _ => fallback
    };
}
