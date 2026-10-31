using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PhotoBooth.Core;
using PhotoBooth.Windows;

namespace PhotoBooth.App;

public sealed partial class MainWindow
{
    private Button? adminButton;
    private Button? resultFiles;
    private bool unlockOpen, operatorAccess;
    private WindowsKioskGuard? shortcutGuard;
    private bool restoringFullscreen;
    private readonly CornerHold cornerHold = new();
    private readonly DispatcherTimer cornerTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private IPointer? cornerPointer;
    private void InstallAdminCorner(Grid root)
    {
        var corner = new Border { Name = "AdminCorner", Width = 64, Height = 64, Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
        // Keep the invisible gesture below guest controls. A Retour or Annuler
        // button near the corner must receive its own pointer input.
        root.Children.Insert(0, corner);
        void Cancel() { cornerHold.Cancel(); cornerTimer.Stop(); cornerPointer?.Capture(null); cornerPointer = null; }
        corner.PointerPressed += (_, e) =>
        {
            if (!kioskLocked || controller.IsBusy ||
                (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(corner).Properties.IsLeftButtonPressed)) return;
            cornerPointer = e.Pointer; e.Pointer.Capture(corner); cornerHold.Begin(); cornerTimer.Start(); e.Handled = true;
        };
        corner.PointerMoved += (_, e) => { var p = e.GetPosition(corner); if (p.X < 0 || p.Y < 0 || p.X > 64 || p.Y > 64) Cancel(); };
        corner.PointerReleased += (_, _) => Cancel();
        corner.PointerCaptureLost += (_, _) => { cornerHold.Cancel(); cornerTimer.Stop(); cornerPointer = null; };
        Deactivated += (_, _) => { Cancel(); cooking.Stop(); };
        Activated += (_, _) => { if (screen == "processing") cooking.Start(); };
        cornerTimer.Tick += (_, _) => { if (!cornerHold.Poll()) return; Cancel(); _ = RequestOperatorAsync(); };
    }
    private void EnterFullscreen()
    {
        // Headless tests have no HWND. The real Windows window must install its guard
        // before guest fullscreen can be activated.
        if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { Handle: not 0 } && shortcutGuard is null)
            shortcutGuard = new WindowsKioskGuard();
        kioskLocked = true; CanResize = false; WindowDecorations = WindowDecorations.None;
        Topmost = true; WindowState = WindowState.FullScreen;
        foreach (var dialog in OwnedWindows) dialog.Topmost = true;
        if (adminButton is not null) adminButton.IsVisible = false;
        if (resultFiles is not null) resultFiles.IsVisible = false;
    }
    private async Task LeaveFullscreenAsync()
    {
        var next = boothSettings with { FullscreenEnabled = false };
        await next.SaveAsync(boothSettingsPath); boothSettings = next;
        kioskLocked = false; shortcutGuard?.Dispose(); shortcutGuard = null;
        Topmost = false; foreach (var dialog in OwnedWindows) dialog.Topmost = false;
        WindowDecorations = WindowDecorations.Full; CanResize = true;
        WindowState = WindowState.Normal; if (adminButton is not null) adminButton.IsVisible = true;
        if (resultFiles is not null) resultFiles.IsVisible = true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != WindowStateProperty || !kioskLocked || restoringFullscreen || WindowState == WindowState.FullScreen) return;
        restoringFullscreen = true;
        try { WindowState = WindowState.FullScreen; }
        finally { restoringFullscreen = false; }
    }
    private async Task RequestOperatorAsync()
    {
        if (unlockOpen || controller.IsBusy) return;
        unlockOpen = true;
        try
        {
            if (exitGate is not null)
            {
                var dialog = Dialog("Code des réglages", 480, 430);
                var legacy = exitGate.Export().CodeLength == 0;
                var pin = new TextBox { Name = "UnlockCode", PasswordChar = '●', MaxLength = legacy ? 12 : 4 };
                var error = Text("", 17); var authorized = false;
                dialog.Content = new StackPanel { Spacing = 20, Margin = new Thickness(30), Children =
                {
                    Text("Retour aux réglages", 28, FontWeight.SemiBold), Text(legacy ? "Code existant" : "Votre code de quatre caractères",18,FontWeight.SemiBold), pin, error,
                    Button("Déverrouiller", async () =>
                    {
                        authorized = exitGate.TryAuthorizeExit(pin.Text ?? ""); await SaveKioskAsync();
                        if (!authorized) { pin.Text = ""; error.Text = "Code incorrect ou verrouillage de 30 secondes."; return; }
                        dialog.Close();
                    }, true),
                    Button("Retour à la fête", () => { dialog.Close(); return Task.CompletedTask; })
                } };
                await dialog.ShowDialog(this);
                if (!authorized) return;
            }
            operatorAccess = true;
            try { await ShowSettingsAsync(); } finally { operatorAccess = false; }
        }
        finally { unlockOpen = false; }
    }
}
