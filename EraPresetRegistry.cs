using BepInEx;
using SprocketEraBindings;

namespace SprocketMaterialSelector;

internal sealed record EraVisualPreset(string Cassette, string Mount, string Visual,
    string Collision, string Icon, string PartGuid, float CassetteMass, float MountMass);

internal static class EraPresetRegistry
{
    internal static readonly EraVisualPreset[] Presets = {
        new("reliktCassette","reliktMount","relikt-intact.obj","relikt-cassette-collision.obj","relikt-icon.png","ef15b8e5-d4c3-4d38-8b03-f07bb395ab63",30.8672f,23f),
        new("kontakt1Cassette","kontakt1Mount","kontakt1-flat.obj","kontakt1-flat-collision.obj","kontakt1-flat-icon.png","94c04114-0c58-5f4c-a3c3-cf1ca8c80e90",5.418f,2f),
        new("kontakt5Cassette","kontakt5Mount","kontakt5-flat.obj","kontakt5-flat-collision.obj","kontakt5-flat-icon.png","c79f599c-ae32-5aad-bc0b-66969fe9cbbf",30.8672f,23f),
        new("reliktTurretCassette","reliktTurretMount","relikt-turret-30.obj","relikt-turret-30-collision.obj","relikt-turret-30-icon.png","604635ff-4b0c-5f67-9f53-819c54a3547e",30.8672f,0.45371936f),
        new("nizhCassette","nizhMount","nizh-intact.obj","nizh-collision.obj","nizh-icon.png","87e198bc-e2c3-564c-ad56-c066703e05c4",30.8672f,0.48727967f),
        new("dupletCassette","dupletMount","duplet-intact.obj","duplet-collision.obj","duplet-icon.png","a6e1b01f-7290-5ca3-9081-eb6d61be4ba7",30.8672f,0.42699167f),
        new("dupletHullCassette","dupletHullMount","duplet-hull-intact.obj","duplet-hull-collision.obj","duplet-hull-icon.png","7f8c3ed8-aaa1-4177-8d13-64edf68d3e1b",73.92f,0.79191084f),
        new("dupletHullFrontCassette","dupletHullFrontMount","duplet-hull-front-intact.obj","duplet-hull-front-collision.obj","duplet-hull-three-zone-icon.png","f8eec062-1fb9-4dea-b808-3a0427304579",35.932f,0.39595542f),
        new("dupletHullMiddleCassette","dupletHullMiddleMount","duplet-hull-middle-intact.obj","duplet-hull-middle-collision.obj","duplet-hull-three-zone-icon.png","f8eec062-1fb9-4dea-b808-3a0427304579",37.072f,0.50436768f),
        new("dupletHullRearCassette","dupletHullRearMount","duplet-hull-rear-intact.obj","duplet-hull-rear-collision.obj","duplet-hull-three-zone-icon.png","f8eec062-1fb9-4dea-b808-3a0427304579",35.932f,0.39595542f),
    };
    private static EraPartBindingCatalogue catalogue = EraPartBindings.Parse("{\"schemaVersion\":1,\"matchMode\":\"componentId\",\"bindings\":[]}");
    internal static void Start()
    {
        // One immutable session snapshot; Save/refresh cannot reclassify spent keys.
        try { catalogue=EraPartBindings.Parse(File.ReadAllText(Path.Combine(Paths.ConfigPath,"sprocket.era.bindings.json"))); }
        catch(Exception ex){Plugin.ModLog.LogWarning("Placed ERA bindings unavailable; active classification disabled: "+ex.Message);}
    }
    internal static bool Visual(string? component,out EraVisualPreset preset,out bool cassette)
    {
        foreach(var item in Presets)
        {
            if(component==item.Cassette){preset=item;cassette=true;return true;}
            if(component==item.Mount){preset=item;cassette=false;return true;}
        }
        preset=null!;cassette=false;return false;
    }
    internal static bool Cassette(string? component,out EraPartBinding binding)
        => catalogue.TryGetCassette(component,out binding!);
    internal static bool Mount(string? component,out EraPartBinding binding)
        => catalogue.TryGetMount(component,out binding!);
}

