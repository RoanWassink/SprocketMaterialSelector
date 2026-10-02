using SprocketMaterialSelector;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
foreach (var (name, rha, density, requested, minimum, effective) in new[]
{
    ("Steel", .70f, 7850f, .60f, .56, .60),
    ("RHA", 1f, 7850f, 2f, 2d, 2d),
    ("HHS", 1.16f, 7850f, 5f, 4.200683, 5d),
    ("Aluminium", .42f, 2700f, 4f, 5.430076, 5.430076),
    ("Titanium", .72f, 4430f, 9f, 6.761174, 9d),
    ("Heavy", 1.5f, 17500f, 7f, 6.812679, 7d),
    ("Cheat1", 1.5f, 3000f, .1f, 1863.066261, 1863.066261),
    ("Cheat2", 2f, 1000f, .01f, 1907779.85114, 1907779.85114)
})
{
    var result = MaterialBalance.Calculate(rha, density, requested);
    Check(result.IsValid, name + " validity");
    Check(Math.Abs(result.MinimumCostMultiplier - minimum) < Math.Max(.00001, minimum * .000001), name + " minimum");
    Check(Math.Abs(result.EffectiveCostMultiplier - effective) < Math.Max(.00001, effective * .000001), name + " effective");
    Check(result.EffectiveCostMultiplier >= result.MinimumCostMultiplier && result.EffectiveCostMultiplier >= requested, name + " floor");
    Check(MaterialBalance.Calculate(rha, density, result.EffectiveCostMultiplier).EffectiveCostMultiplier == result.EffectiveCostMultiplier, name + " idempotence");
}
foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
{
    Check(!MaterialBalance.Calculate(bad, 7850, 2).IsValid, "invalid RHA");
    Check(!MaterialBalance.Calculate(1, bad, 2).IsValid, "invalid density");
}
foreach (var bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue })
    Check(!MaterialBalance.Calculate(1, 7850, bad).IsValid, "invalid requested cost");
Check(MaterialBalance.Calculate(1, 7850, 0).EffectiveCostMultiplier == 2, "zero request must get floor");
Check(!MaterialBalance.Calculate(5, 100, 0).IsValid, "SuperFoam rejected rather than clamped");
Check(!MaterialBalance.Calculate(float.MaxValue, float.Epsilon, 0).IsValid, "extreme calculation");
Check(MaterialBalance.TryMaterialCost(2335.61f, 2, .242995858f, out var cost) && cost > 1135 && cost < 1136, "vanilla RHA cost");
Check(!MaterialBalance.TryMaterialCost(float.MaxValue, 2, .24f, out _), "total cost limit");
Check(!MaterialBalance.TryMaterialCost(float.NaN, 2, .24f, out _), "invalid mass");
Check(!MaterialBalance.TryMaterialCost(1, float.PositiveInfinity, .24f, out _), "invalid multiplier");
Check(MaterialBalance.TryMaterialCost(0, 2, .24f, out cost) && cost == 0, "empty mesh");
var packDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Optional Balanced Materials"));
var ids = new HashSet<string>(StringComparer.Ordinal);
var packFiles = Directory.GetFiles(packDir, "*.json");
Check(packFiles.Length == 11, "pack material count");
foreach (var file in packFiles)
{
    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
    var root = doc.RootElement;
    Check(ids.Add(root.GetProperty("type").GetString()!), "unique pack ID");
    var props = root.GetProperty("properties");
    var result = MaterialBalance.Calculate(props.GetProperty("rhaFactor").GetSingle(),
        props.GetProperty("density").GetSingle(), props.GetProperty("costMultiplier").GetSingle());
    Check(result.IsValid, file + " valid balance");
    Check(!result.WasAdjusted, file + " explicit pack price covers floor");
    var spall = props.GetProperty("spallFactor").GetSingle();
    Check(float.IsFinite(spall) && spall >= 0, file + " spall");
}
Console.WriteLine($"PASS: {checks} material balance and pack checks.");
