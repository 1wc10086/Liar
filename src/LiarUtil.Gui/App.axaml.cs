using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LiarUtil.Core.Services;
using LiarUtil.Gui.Services;
using LiarUtil.Gui.ViewModels;
using LiarUtil.Gui.Views;

namespace LiarUtil.Gui;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var snackbar = new SnackbarService();
        var settings = AppSettings.Load();
        var loc = new LocalizationService(settings);
        var pickers = new FilePickerService(snackbar, loc);
        var processing = new ProcessingService(
            new RtonService(),
            new TextureService(),
            new RsbService(),
            new BnkService(),
            new PamService(),
            new PpfService(),
            new PopFxService(),
            new Cfw2Service(),
            new ParticleService(),
            new TrailService(),
            new ReanimService(),
            new DzService(),
            new PakService(),
            new ArcvService(),
            new XprService(),
            new AtlasService(),
            new NewtonService(),
            new RsbPatchService(),
            new FontWidgetDatService(),
            new PaxService(),
            new WemService(),
            new TextTableService(),
            new SpsService(),
            new SnrService(),
            new CafService(),
            new WmaService(),
            new XmService(),
            new XmaService(),
            new XnbAudioService(),
            new XnbFontService(),
            new Cfu2Service());
        var viewModel = new MainViewModel(snackbar, settings, loc, pickers, processing);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new MainWindow { DataContext = viewModel };
                desktop.MainWindow = window;
                if (window.Content is MainView mainView)
                {
                    mainView.AttachSnackbar(snackbar);
                    mainView.AttachPickers(pickers);
                }
                break;
            case ISingleViewApplicationLifetime singleView:
                var view = new MainView { DataContext = viewModel };
                view.AttachSnackbar(snackbar);
                view.AttachPickers(pickers);
                singleView.MainView = view;
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
