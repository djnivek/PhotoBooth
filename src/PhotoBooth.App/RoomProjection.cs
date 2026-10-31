using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PhotoBooth.Runtime;

namespace PhotoBooth.App;

public sealed partial class MainWindow
{
    private readonly Dictionary<string,BoothProjection> projectionClients=[];
    private BoothProjection? projection;
    private readonly DispatcherTimer resultReturn=new() {Interval=TimeSpan.FromMilliseconds(200)};
    private DateTimeOffset returnAt,lastReturnTick;
    private TextBlock? returnLabel;
    private bool startingNext,printingResult,resultShareReady;
    private readonly Func<DateTimeOffset> clock;
    public bool ResultReturnRunning=>resultReturn.IsEnabled;
    public int ResultReturnSeconds=>Math.Clamp((int)Math.Ceiling((returnAt-clock()).TotalSeconds),0,60);

    private void PrepareProjection()
    {
        projection=null;
        if(cloudKey.Length==0)return;
        try
        {
            var key=PhotoBooth.Imaging.SessionStore.Hash(System.Text.Encoding.UTF8.GetBytes(boothSettings.ApiBaseUrl+cloudKey));
            if(!projectionClients.TryGetValue(key,out var client))
            {
                client=new BoothProjection(Path.GetDirectoryName(store.Root)!,boothSettings.ApiBaseUrl,cloudKey,
                    boothSettings.ViewerEnabled,boothSettings.ViewerIncludeTests,automatic:cloudFactoryOverride is null && cameraFactoryOverride is null);
                projectionClients[key]=client;
            }
            projection=client;
        }
        catch(Exception) { /* Projection setup never blocks photographs or printing. */ }
    }
    private void PrepareReturnButton(Button button)
    {
        resultReturn.Stop();returnLabel=null;
        if(!boothSettings.ViewerEnabled)return;
        var content=new Grid {ColumnDefinitions=new ColumnDefinitions("*,72")};
        content.Children.Add(new TextBlock {Text="Une nouvelle séance",FontSize=25,FontWeight=FontWeight.SemiBold,VerticalAlignment=VerticalAlignment.Center});
        returnLabel=new TextBlock {Name="NewSessionCountdown",Text="60 s",FontSize=22,TextAlignment=TextAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
        Grid.SetColumn(returnLabel,1);content.Children.Add(returnLabel);
        button.Content=content;button.Height=70;
        lastReturnTick=clock();returnAt=lastReturnTick.AddSeconds(60);resultReturn.Start();
    }
    private async void ReturnTick(object? sender,EventArgs args)
    {
        var now=clock();
        if(!boothSettings.ViewerEnabled || screen!="result" || lifetime.IsCancellationRequested){resultReturn.Stop();return;}
        if(OwnedWindows.Any() || printingResult || cloud is not null && !resultShareReady) returnAt+=now-lastReturnTick;
        lastReturnTick=now;
        if(returnLabel is not null)returnLabel.Text=$"{ResultReturnSeconds} s";
        if(ResultReturnSeconds==0)
        {
            try {await StartNextSessionAsync();}
            catch(Exception) {resultReturn.Stop();}
        }
    }
    private void AddViewerSettings(StackPanel content,TextBlock info)
    {
        content.Children.Add(Text("Projection dans la salle",24,FontWeight.SemiBold));
        var enabled=new CheckBox {Name="ViewerEnabled",Content="Activer le diaporama dans la salle",IsChecked=boothSettings.ViewerEnabled};
        var tests=new CheckBox {Name="ViewerIncludeTests",Content="Inclure les séances avec caméra simulée",IsChecked=boothSettings.ViewerIncludeTests};
        var link=new TextBox {Name="ViewerLink",IsReadOnly=true,Text=projection?.ViewerUrl??"",PlaceholderText="Le lien apparaît après activation",TextWrapping=TextWrapping.Wrap};
        content.Children.Add(enabled);content.Children.Add(tests);
        content.Children.Add(Text("Retour à l’accueil après 60 secondes. L’écran de la salle annonce ensuite les nouvelles photos prêtes pendant 15 secondes. Ouvrez ce lien sur cet écran et activez son plein écran.",16));
        content.Children.Add(Button("Enregistrer la projection",async()=>
        {
            if(enabled.IsChecked==true && (!boothSettings.CloudEnabled || projection is null))throw new InvalidOperationException("Activez d’abord la connexion en ligne pour relier l’écran de la salle.");
            if(projection is not null)await projection.ConfigureAsync(enabled.IsChecked==true,tests.IsChecked==true);
            boothSettings=boothSettings with {ViewerEnabled=enabled.IsChecked==true,ViewerIncludeTests=tests.IsChecked==true};
            await boothSettings.SaveAsync(boothSettingsPath);
            link.Text=projection?.ViewerUrl??"";
            info.Text=boothSettings.ViewerEnabled?"Projection activée. Ouvrez le lien sur l’écran de la salle. La connexion est synchronisée en arrière-plan.":"Projection désactivée. Le retour à l’accueil reste manuel.";
            if(projection is not null && cloudFactoryOverride is null && cameraFactoryOverride is null)_=projection.FlushAsync();
            resultReturn.Stop();if(screen=="result")RenderResult();
        }));
        content.Children.Add(link);
        content.Children.Add(Button("Copier le lien du viewer",async()=>
        {
            if(string.IsNullOrEmpty(link.Text))throw new InvalidOperationException("Activez et enregistrez la projection pour obtenir son lien.");
            if(Clipboard is not null)await Clipboard.SetTextAsync(link.Text);
            info.Text="Lien copié. Il autorise la lecture des photos projetées ; gardez-le pour l’écran de la salle.";
        }));
    }
    private void AddProjectionNote(StackPanel panel)
    {
        if(boothSettings.ViewerEnabled && controller.Session?.UsedOriginalFallback!=true)
        {
            var note=Text("Vos photos seront bientôt sur l’écran de la salle. Rejoignez la fête après avoir scanné votre QR !",18);
            note.Name="ProjectionGuestMessage";note.Foreground=Muted;panel.Children.Add(note);
        }
    }
}
