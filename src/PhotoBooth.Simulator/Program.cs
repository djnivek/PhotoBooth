using System.Globalization;
using System.Text;
using System.Text.Json;
using PhotoBooth.Core;

if (args.Length > 2 || (args.Length > 0 && args[0] is not ("1" or "4")) ||
    (args.Length > 1 && args[1] is not ("original" or "surprise")))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/PhotoBooth.Simulator -- [1|4] [original|surprise]");
    return 2;
}
var count = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 4;
var mode = args.Length > 1 && args[1] == "surprise" ? PhotoMode.Surprise : PhotoMode.Original;
var session = new Session(new SessionOptions(count, mode));
Console.WriteLine("SIMULATION — no camera, no OpenAI request, no network, no real QR, no print.");
for (var shot = 1; shot <= count; shot++)
{
    Console.WriteLine($"Live view requested: {session.WantsLiveView}. Positioning for shot {shot}/{count}.");
    session.StartCountdown();
    while (session.Stage == SessionStage.Countdown)
    {
        Console.WriteLine($"{session.SecondsRemaining} (live view remains visible)");
        session.Tick(); // Virtual time, no real countdown in this command-line simulator.
    }
    session.RecordCapture(new CapturedPhoto(Guid.NewGuid(), $"simulated/original-{shot}.svg"));
}
if (mode == PhotoMode.Surprise) session.AssignStyle("paper-cut-v1");
var result = session.Captures.Select((photo, i) => new FinalPhoto(photo.Id,
    mode == PhotoMode.Original ? photo.LocalAssetKey : $"simulated/transformed-{i + 1}.svg")).ToArray();
session.CompleteProcessing(result, "simulated/sheet.svg");

var output = Path.Combine("artifacts", "simulation", session.Id.ToString());
Directory.CreateDirectory(output);
var layout = PrintLayout.FourBySix(count);
var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{layout.Width}\" height=\"{layout.Height}\" viewBox=\"0 0 {layout.Width} {layout.Height}\"><rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
for (var i = 0; i < count; i++)
{
    var cell = layout.Photos[i];
    var label = $"SIMULATION {i + 1} — {mode}";
    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{cell.X}\" y=\"{cell.Y}\" width=\"{cell.Width}\" height=\"{cell.Height}\" fill=\"#e8e8e8\" stroke=\"#333\"/><text x=\"{cell.X + 32}\" y=\"{cell.Y + 64}\" font-family=\"sans-serif\" font-size=\"32\">{label}</text>");
}
svg.Append("</svg>");
await File.WriteAllTextAsync(Path.Combine(output, "sheet.svg"), svg.ToString());
await File.WriteAllTextAsync(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(new
{
    simulated = true, session.Id, photoCount = count, mode = mode.ToString(), session.StyleId,
    stage = session.Stage.ToString(), printStage = session.PrintStage.ToString(),
    downloads = "Not implemented: simulation references are not files or public links.",
    layout
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Ready: {session.Results.Count} results, print state: {session.PrintStage}. No automatic printing.");
Console.WriteLine($"Generated layout fixture and manifest: {output}");
return 0;
