using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ErganiManager.Core.Interfaces;
using ErganiManager.Core.Models;
using ErganiManager.UI.ViewModels;
using ErganiManager.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ErganiManager.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Shut down when the MainWindow is closed — essential so the process
            // actually exits when the user closes any top-level window.
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            _ = BootstrapAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async System.Threading.Tasks.Task BootstrapAsync(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        var connectionState = Program.Services.GetRequiredService<IConnectionStateService>();
        var state = await connectionState.EvaluateAsync();
        Log.Information("Startup connection state: {State}.", state);

        switch (state)
        {
            case AppConnectionState.FirstRun:
            case AppConnectionState.SchemaIncomplete:
                ShowDatabaseSetup(desktop, isRetry: state == AppConnectionState.SchemaIncomplete);
                break;

            case AppConnectionState.Normal:
            case AppConnectionState.Degraded:
                ShowLogin(desktop, isDegraded: state == AppConnectionState.Degraded);
                break;
        }
    }

    // ── Window transitions ────────────────────────────────────────────────────

    /// <summary>
    /// Transitions to a new MainWindow and closes the old one.
    /// Always call this instead of setting desktop.MainWindow directly.
    /// </summary>
    private static void TransitionTo(
        IClassicDesktopStyleApplicationLifetime desktop, Window next)
    {
        var previous = desktop.MainWindow;
        desktop.MainWindow = next;
        next.Show();
        previous?.Close();
    }

    public void ShowDatabaseSetup(
        IClassicDesktopStyleApplicationLifetime desktop, bool isRetry = false)
    {
        var vm = Program.Services.GetRequiredService<DatabaseSetupViewModel>();

        if (isRetry)
            vm.StatusMessage =
                "⚠️ The database was configured but the schema could not be applied " +
                "(a previous setup attempt failed). Fix the settings below and try again.";

        // When setup completes successfully, close this window and show Login.
        vm.SetupCompleted += (_, _) => ShowLogin(desktop, isDegraded: false);

        TransitionTo(desktop, new DatabaseSetupView { DataContext = vm });
    }

    public void ShowLogin(
        IClassicDesktopStyleApplicationLifetime desktop, bool isDegraded)
    {
        var vm = Program.Services.GetRequiredService<LoginViewModel>();
        vm.IsDegradedMode = isDegraded;
        vm.LoginSucceeded += (_, session) => OnLoginSucceeded(desktop, session);

        TransitionTo(desktop, new LoginView { DataContext = vm });
    }

    private void OnLoginSucceeded(
        IClassicDesktopStyleApplicationLifetime desktop, UserSession session)
    {
        Window next;

        if (session.Role == AppUserRole.Operator)
        {
            var vm = Program.Services.GetRequiredService<TerminalViewModel>();
            vm.Initialize(session);
            next = new TerminalView { DataContext = vm };
        }
        else
        {
            var vm = Program.Services.GetRequiredService<AdminShellViewModel>();
            vm.Initialize(session);
            next = new AdminShellView { DataContext = vm };
        }

        TransitionTo(desktop, next);
    }
}
