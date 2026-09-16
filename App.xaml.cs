using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace MarketStockManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // WPF binding (StringFormat) podrazumevano koristi en-US.
        // Ovim tabele prikazuju brojeve i datume isto kao ostatak aplikacije (sistemska kultura).
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Došlo je do neočekivane greške:\n{e.Exception.Message}",
            "MarketStock Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
