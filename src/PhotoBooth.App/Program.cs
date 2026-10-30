using Avalonia;

namespace PhotoBooth.App;

internal static class Program
{
    internal static string? SmokeOutput { get; private set; }
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if(args.FirstOrDefault()=="--self-test")
            {
                if(args.Length!=2)throw new ArgumentException("Usage: EstellArt.exe --self-test <output-directory>");
                SmokeOutput=Path.GetFullPath(args[1]);
                PackageSelfTest.RunAsync(SmokeOutput).GetAwaiter().GetResult();return 0;
            }
            if(args.FirstOrDefault()=="--smoke-ui")
            {
                if(args.Length!=2)throw new ArgumentException("Usage: EstellArt.exe --smoke-ui <output-directory>");
                SmokeOutput=Path.GetFullPath(args[1]);Directory.CreateDirectory(SmokeOutput);
                Environment.SetEnvironmentVariable("PHOTOBOOTH_DATA_DIR",Path.Combine(SmokeOutput,"isolated-ui-"+Guid.NewGuid().ToString("N")));
            }
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch(Exception exception)
        {
            var root=SmokeOutput??Environment.GetEnvironmentVariable("PHOTOBOOTH_DATA_DIR")??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EstellArt");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"startup-error.txt"),exception.ToString());
            return 1;
        }
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
