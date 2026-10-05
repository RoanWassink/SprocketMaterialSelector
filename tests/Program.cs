using System.Text.Json;
using System.Text.Json.Nodes;
using SprocketMaterialSelector;

var checks = 0;
void Check(bool test, string message) { checks++; if (!test) throw new Exception(message); }
var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../candidate"));
var json = File.ReadAllText(Path.Combine(directory,"config/sprocket.armour.responses.json"));
var catalog = ArmourResponses.Parse(json);
Check(!catalog.Enabled, "Candidate must default disabled");
Check(catalog.Responses.Count == 5, "Five additive candidates");
Check(MaterialBalance.Calculate(1,7850,2).EffectiveCostMultiplier == 2, "RHA price unchanged");
Check(MaterialEraPolicy.MinimumDate == new DateTime(1945,9,3), "Modern armour date floor");
var controlCost = 785 * 2 * .24299585819244385;
var lines = new List<string>{"responseId,areaM2,totalMassKg,cassetteThicknessMm,cassetteMassKg,backingThicknessMm,backingMassKg,volumeM3,effectiveMultiplier,materialCostUnits,ratioToRha,nativeAssembly,passiveResistanceMm"};
foreach (var response in catalog.Responses)
{
    var id = response.CompatibleMaterialIds.Single();
    using var techDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"Technology",response.ResponseId+".json")));
    var tech = techDoc.RootElement;
    var properties = tech.GetProperty("properties");
    Check(tech.GetProperty("type").GetString() == id, "Exact ID bridge");
    Check(tech.GetProperty("date").GetString() == "1945.09.03", "Full ColdWar availability");
    Check(properties.GetProperty("density").GetDouble() == response.PassiveMaterial.Density, "Density catalogue/tech agreement");
    Check(properties.GetProperty("costMultiplier").GetDouble() == response.PassiveMaterial.RequestedCostMultiplier, "Requested price agreement");
    Check(response.Calibration.IntactRodRetention == (response.Kind == "heavyEra" ? .85 : 1), "Only heavy ERA has intact rod response");
    Check(response.Calibration.DisturbanceContribution == 0, "No arbitrary cassette yaw contribution");
    var b = MaterialBalance.Calculate((float)response.PassiveMaterial.RhaFactor,(float)response.PassiveMaterial.Density,
        (float)response.PassiveMaterial.RequestedCostMultiplier);
    Check(b.IsValid && b.EffectiveCostMultiplier >= response.PassiveMaterial.RequestedCostMultiplier, "Native floor retained");
    Check(ArmourResponses.RequestedPrice(0,response)==response.PassiveMaterial.RequestedCostMultiplier,"Cannot underprice new response ID");
    Check(ArmourResponses.RequestedPrice(25,response)==25,"Custom higher price retained");
    Check(ArmourResponses.RequestedPrice(2,null)==2,"Legacy material request unaffected");
    Check(ArmourResponses.PassiveMatches(response,(float)response.PassiveMaterial.Density,
        (float)response.PassiveMaterial.RhaFactor,(float)response.PassiveMaterial.SpallFactor),"Native float physical agreement");
    Check(!ArmourResponses.PassiveMatches(response,(float)response.PassiveMaterial.Density*1.1f,
        (float)response.PassiveMaterial.RhaFactor,(float)response.PassiveMaterial.SpallFactor),"Custom physical mismatch fails response compatibility");
    var t = response.Kind == "heavyEra" ? 70d : response.Kind is "nera" or "lightEra" ? 30d : 100d;
    Check(t >= response.Geometry.MinNormalThicknessMm && t <= response.Geometry.MaxNormalThicknessMm, "Reference normal thickness eligible");
    var cassetteMass = ArmourResponses.ArealMass(response.PassiveMaterial.Density,t);
    var backingMass = 785 - cassetteMass;
    var backingT = backingMass / 7.85;
    var materialCost = (cassetteMass*b.EffectiveCostMultiplier + backingMass*2)*.24299585819244385;
    Check(backingMass > 0 && Math.Abs(backingMass+cassetteMass-785) < 1e-9, "Equal covered area/mass budget");
    var minimumRatio = response.Kind == "nera" ? 1.75 : 1.5;
    Check(materialCost/controlCost >= minimumRatio, "Candidate material-price target");
    var nativeRoundedMass = ArmourResponses.ArealMass((float)response.PassiveMaterial.Density,t);
    Check(Math.Abs(nativeRoundedMass-cassetteMass)<.001, "Density float rounding bounded");
    if (response.Kind == "nera")
        Check(Math.Abs(cassetteMass-(7850*.01+1200*.01+7850*.01)) < 1e-6, "True scaled NERA thirds mass");
    if (response.Kind == "glassTextolite")
        Check(response.Geometry.MinMeasuredGapMm == 5 && response.Geometry.MaxMeasuredGapMm == 300, "Real gap provenance bounds");
    if (response.Kind == "lightEra")
        Check(response.CellPitchM == .25 && response.Calibration.IntactRodRetention == 1 && response.Calibration.DisturbedRodRetention == 1, "Light ERA has no KE bonus");
    lines.Add(string.Join(",",new object[]{response.ResponseId,1,785,t,cassetteMass,backingT,backingMass,
        (t+backingT)*.001,b.EffectiveCostMultiplier,materialCost,materialCost/controlCost,"pendingLive",
        response.PassiveMaterial.RhaFactor*t+backingT}.Select(x=>Convert.ToString(x,System.Globalization.CultureInfo.InvariantCulture))));
    foreach(var scale in new[]{.25,1,4})
        Check(Math.Abs(ArmourResponses.ArealMass(response.PassiveMaterial.Density,t*scale)/cassetteMass-scale)<1e-10,"Mass scales physically");
}
void Reject(Action<JsonObject> edit,string message)
{
    var root = JsonNode.Parse(json)!.AsObject(); edit(root); var rejected=false;
    try { ArmourResponses.Parse(root.ToJsonString()); }
    catch(Exception ex) when(ex is FormatException or JsonException or InvalidOperationException) { rejected=true; }
    Check(rejected,message);
}
JsonObject Entry(JsonObject o) => o["responses"]![0]!.AsObject();
Reject(o=>o["schemaVersion"]=2,"Unsupported schema");
Reject(o=>o["unknown"]=1,"Unknown root field");
Reject(o=>Entry(o)["minimumEra"]="earlywar","Pre-ColdWar response rejected");
Reject(o=>Entry(o)["kind"]="tandemERA","Unsupported tandem response");
Reject(o=>Entry(o)["compatibleMaterialIds"]![0]="rha","Ordinary RHA never becomes response");
Reject(o=>o["responses"]![1]!["compatibleMaterialIds"]![0]="cwepGlassTextolite","Duplicate material binding");
Reject(o=>o["responses"]![1]!["responseId"]="glassTextolite","Duplicate response IDs");
Reject(o=>Entry(o)["passiveMaterial"]!["density"]=-1,"Invalid density");
Reject(o=>Entry(o)["passiveMaterial"]!["requestedCostMultiplier"]=-1,"Invalid price");
Reject(o=>Entry(o)["calibration"]!["disturbedRodRetention"]=.2,"Loss cap enforced");
Reject(o=>Entry(o)["geometry"]!["minNormalThicknessMm"]=500,"Invalid normal thickness bounds");
Reject(o=>Entry(o)["geometry"]!["angularCurve"]![1]!["angleDegrees"]=0,"Unordered angle curve");
Reject(o=>Entry(o)["geometry"]!["maxMeasuredGapMm"]=1,"Inverted measured gap bounds");
Reject(o=>Entry(o)["era"]!["cellPitchM"]=.25,"Passive response cannot have ERA cells");
Reject(o=>o["responses"]![2]!["era"]!["cellPitchM"]=0,"ERA needs bounded cells");
Reject(o=>o["preconditioning"]!["cumulativeDisturbanceCap"]=1,"Bounded disturbance");
Reject(o=>o["preconditioning"]!["angleFullDegrees"]=20,"Zero angle ramp denominator rejected");
var duplicateJson=json.Replace("\"schemaVersion\": 1,","\"schemaVersion\": 1, \"schemaVersion\": 1,");
var duplicateRejected=false; try{ArmourResponses.Parse(duplicateJson);}catch(FormatException){duplicateRejected=true;}
Check(duplicateRejected,"Duplicate fields rejected");
// Regression checks for native Technology syntax and material-only filtering.
void Tech(string text, bool expectedMaterial, string message)
{
    using var doc = TechnologyReader.Parse(text);
    Check(TechnologyReader.TryMaterial(doc.RootElement,out _,out _,out _)==expectedMaterial,message);
}
Tech("{\"type\":\"combustionEngine\",\"properties\":{\"torqueCoefficient\":1,},}",false,"Stock engine trailing comma silently skipped");
Tech("{\"type\":\"trackSuspension\",\"properties\":{\"maxArmLength\":1000,},}",false,"Stock suspension trailing comma silently skipped");
Tech("{\"type\":\"customArmour\",\"properties\":{\"rhaFactor\":.5,}}".Replace(":.5",":0.5"),true,"Actual material trailing comma accepted");
Tech("{\"type\":\"engine\",\"description\":\"rhaFactor\",\"properties\":{\"nested\":{\"rhaFactor\":1}}}",false,"No filename/string/nested false material match");
Tech("{\"type\":\"badMaterial\",\"properties\":{\"rhaFactor\":\"invalid\",}}",true,"Bad material value remains eligible for validation/warning");
Tech("{\"type\":\"engine\",\"properties\":null}",false,"Non-object properties skipped safely");
var strictTrailingRejected=false;
try { ArmourResponses.Parse(json.TrimEnd().Substring(0,json.TrimEnd().Length-1)+",}"); }
catch(JsonException) { strictTrailingRejected=true; }
Check(strictTrailingRejected,"Response parser still rejects trailing commas");
var malformedTechRejected=false;
try { using var invalid=TechnologyReader.Parse("{\"type\":]"); }
catch(JsonException) { malformedTechRejected=true; }
Check(malformedTechRejected,"Genuinely malformed native JSON still rejects");
if(args.Length>0)
{
    var names=new[]{"InterwarSuspension","LatewarEngine","MidwarEngine","WWILayingDrive","WWIReturnRollerArray","WWIReturnRollerMount","WWIRoadwheelArray","WWIRoadwheelMount","WWISprocket","WWITraverseMotor"};
    foreach(var name in names) Tech(File.ReadAllText(Path.Combine(args[0],name+".json")),false,"Actual stock file skipped without parser warning: "+name);
    var parsed=0;var materialCount=0;var otherCount=0;var parseFailures=0;
    foreach(var file in Directory.EnumerateFiles(args[0],"*.json"))
    {
        try
        {
            using var doc=TechnologyReader.Parse(File.ReadAllText(file)); parsed++;
            if(TechnologyReader.TryMaterial(doc.RootElement,out _,out _,out _)) materialCount++; else otherCount++;
        }
        catch(JsonException) {parseFailures++;}
    }
    Console.WriteLine($"Read-only installed Technology scan: {parsed} parsed; {materialCount} material candidates; {otherCount} other assets skipped; {parseFailures} malformed JSON files.");
}

Check(MaterialAvailability.Evaluate(true,true,true)==MaterialAvailabilityStatus.Available,"ColdWar material in actual technology frame shown");
Check(MaterialAvailability.Evaluate(true,true,false)==MaterialAvailabilityStatus.NotInTechnologyFrame,"Missing native technology diagnosed separately");
Check(MaterialAvailability.Evaluate(true,false,false)==MaterialAvailabilityStatus.MissingTechnologyFrame,"Missing technology frame diagnosed");
Check(MaterialAvailability.Evaluate(null,true,true)==MaterialAvailabilityStatus.UnknownDesignDate,"Promoted tech frame does not supply missing date/timeline evidence");
Check(MaterialAvailability.Evaluate(false,true,true)==MaterialAvailabilityStatus.RequiresPostwarDesign,"Promoted technology does not unlock ERA before date floor");
Check(MaterialAvailability.Evaluate(true,true,true)==MaterialAvailabilityStatus.Available,"Global response disabled does not hide eligible passive cassette");
Check(MaterialAvailability.Label("cwepLightEraCassette","oldLabel").Contains("ERA"),"Clear ERA menu label");
Check(MaterialAvailability.Label("customUserMaterial","Custom title")=="Custom title","Preserve unrelated custom material display name");

var heavy = catalog.Responses.Single(r => r.Kind == "heavyEra");
Check(heavy.Geometry.KineticAngularCurve is { Count: 4 }, "Heavy ERA has distinct required kinetic angle curve");
Check(heavy.Geometry.AngularCurve[0].Weight == 1 && heavy.Geometry.KineticAngularCurve![0].Weight == 0, "Normal HEAT/rod responses differ");
Check(heavy.CellPitchM == .25, "Heavy/light grid pitch agrees");
Check(heavy.Calibration.IntactRodRetention == .85 && heavy.Calibration.DisturbedRodRetention == .85, "Heavy rod response does not require steel preconditioning");
Check(heavy.Geometry.MinNormalThicknessMm == 60 && heavy.Geometry.MaxNormalThicknessMm == 80, "Heavy normal thickness contract");
Check(ArmourResponses.ArealMass(heavy.PassiveMaterial.Density,70) == 280, "Reference heavy cassette areal mass");
Check(Math.Abs((280*10.5+505*2)/(785*2)-2.515923566878981)<1e-12, "Native price comparison uses RHA multiplier two");
Check(MaterialAvailability.Label("cwepHeavyEraCassette","fallback").Contains("Kontakt-5-inspired"), "Heavy label discloses approximation");
JsonObject Heavy(JsonObject o) => o["responses"]![4]!.AsObject();
Reject(o => Heavy(o)["geometry"]!.AsObject().Remove("kineticAngularCurve"), "Heavy missing kinetic curve rejected");
Reject(o => Heavy(o)["geometry"]!["kineticAngularCurve"] = new JsonArray(), "Heavy empty kinetic curve rejected");
Reject(o => Heavy(o)["geometry"]!["kineticAngularCurve"]![1]!["angleDegrees"] = 0, "Heavy unordered kinetic curve rejected");
Reject(o => Heavy(o)["geometry"]!["kineticAngularCurve"]![1]!["weight"] = 2, "Heavy invalid kinetic weight rejected");
Reject(o => Entry(o)["geometry"]!["kineticAngularCurve"] = Heavy(o)["geometry"]!["kineticAngularCurve"]!.DeepClone(), "Legacy kind forbids heavy-only field");
Reject(o => Heavy(o)["era"]!["cellPitchM"] = 0, "Heavy needs finite cells");
Reject(o => Heavy(o)["geometry"]!["mode"] = "resolvedLayers", "Heavy must declare complete cassette");
Reject(o => Heavy(o)["calibration"]!["heatRetention"] = .39, "Heavy respects original chemical cap");
Reject(o => Heavy(o)["calibration"]!["intactRodRetention"] = .64, "Heavy respects original kinetic cap");
// Exact parity with the inspected Thermal reference, including malformed timelines.
var timelines = new DateTime[][] {
    new[] { new DateTime(1910,1,1), new DateTime(1939,9,1), new DateTime(1945,9,3) },
    new[] { new DateTime(1910,1,1), new DateTime(1945,9,3), new DateTime(1945,12,1), new DateTime(1960,1,1) },
    new[] { new DateTime(1910,1,1), new DateTime(1945,1,1) },
    new[] { new DateTime(1910,1,1), new DateTime(1945,9,2) },
    new[] { new DateTime(1960,1,1), new DateTime(2026,1,1) },
    Array.Empty<DateTime>(),
    new[] { new DateTime(1910,1,1), new DateTime(1910,1,1) },
    new[] { new DateTime(1960,1,1), new DateTime(1945,9,3) },
    new[] { new DateTime(1910,1,1), DateTime.MaxValue },
    new[] { DateTime.MaxValue }
};
var dates = new DateTime?[] { null, new DateTime(1900,1,1), new DateTime(1910,1,1),
    new DateTime(1945,1,1), new DateTime(1945,9,2), new DateTime(1945,9,3),
    new DateTime(1945,12,1), new DateTime(1950,1,1), new DateTime(1960,1,1),
    new DateTime(1991,12,31), new DateTime(1992,1,1), new DateTime(2026,1,1),
    new DateTime(2099,1,1), new DateTime(9998,12,31), DateTime.MaxValue };
foreach (var timeline in timelines)
    foreach (var date in dates)
        Check(MaterialEraPolicy.Allows(date,timeline) == SprocketThermalSight.ThermalEraPolicy.Allows(date,timeline),
            "Material date policy agrees with current Thermal reference");
Check(!MaterialEraPolicy.Allows(new DateTime(1945,9,2),timelines[0]), "Day before floor blocked");
Check(MaterialEraPolicy.Allows(new DateTime(1945,9,3),timelines[0]), "Exact floor included");
Check(MaterialEraPolicy.Allows(new DateTime(1945,12,1),timelines[1]), "Later same-year custom era allowed");
Check(MaterialEraPolicy.Allows(new DateTime(2099,1,1),timelines[1]), "No invented 1991 ceiling");
Check(MaterialEraPolicy.Allows(DateTime.MaxValue,timelines[1]), "Sentinel uses actual later final start");
Check(!MaterialEraPolicy.Allows(DateTime.MaxValue,timelines[2]), "Same-year pre-floor final era sentinel blocked");
Check(!MaterialEraPolicy.Allows(DateTime.MaxValue,timelines[3]), "Final start one day before floor sentinel blocked");
Check(!MaterialEraPolicy.Allows(new DateTime(1950,1,1),timelines[4]), "Date before known first era blocked");
Check(MaterialEraPolicy.Evaluate(new DateTime(2026,1,1),timelines[6]) == null, "Duplicate timeline diagnosed unknown");
Check(MaterialAvailability.Evaluate(MaterialEraPolicy.Evaluate(DateTime.MaxValue,timelines[8]),true,true)
    == MaterialAvailabilityStatus.UnknownDesignDate, "Sentinel era metadata does not unlock technology");
Check(MaterialAvailability.Evaluate(true,true,false) == MaterialAvailabilityStatus.NotInTechnologyFrame,
    "Valid later custom era still requires actual native technology");
Check(MaterialAvailability.Evaluate(false,true,true) == MaterialAvailabilityStatus.RequiresPostwarDesign,
    "Promoted frame cannot override too early design date");

Console.WriteLine($"PASS: {checks} catalogue, compatibility and mass/material-cost checks. Native assembly/impact/live validation remains pending.");
Console.WriteLine(string.Join(Environment.NewLine,lines));
