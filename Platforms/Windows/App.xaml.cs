using Microsoft.UI.Xaml;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace EHMR.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // Ова е НАЈРАНИОТ можен hook - фаќа native WinUI исклучоци (XAML
            // rendering, layout, binding errors на UI threat) кои НЕ поминуваат
            // низ AppDomain.UnhandledException во сподeлениот App.xaml.cs.
            // Ова е веројатно она што ни недостасуваше за да го фатиме падот
            // по успешната најава / навигација кон dashboard.
            this.UnhandledException+=(s, e) =>
            {
                EHMR.Logger.LogException("WinUI native UnhandledException", e.Exception);

                // e.Handled = true значи апликацијата ќе продолжи да работи по
                // логирањето наместо целосно да се урне. Ако сакаш да видиш дали
                // навистина ова е причината (со native crash dialog), привремено
                // стави false и провери дали Windows прикажува crash при истата постапка.
                e.Handled=true;
            };
        }
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}