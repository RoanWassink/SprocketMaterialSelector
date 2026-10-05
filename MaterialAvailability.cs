namespace SprocketMaterialSelector;

internal enum MaterialAvailabilityStatus { Available, UnknownDesignEra, RequiresColdWar, MissingTechnologyFrame, NotInTechnologyFrame }

internal static class MaterialAvailability
{
    internal static MaterialAvailabilityStatus Evaluate(string? actualNativeEra, bool hasTechnologyFrame, bool materialInFrame)
    {
        if (string.IsNullOrWhiteSpace(actualNativeEra)) return MaterialAvailabilityStatus.UnknownDesignEra;
        if (!ArmourResponses.ColdWarEra(actualNativeEra)) return MaterialAvailabilityStatus.RequiresColdWar;
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
