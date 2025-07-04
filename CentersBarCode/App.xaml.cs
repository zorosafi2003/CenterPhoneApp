using CentersBarCode.Services;
using CentersBarCode.ViewModels;

namespace CentersBarCode;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();

        // Start with splash screen to handle proper initialization
        MainPage = loginPage;
    }

    protected override void OnStart()
    {
        base.OnStart();
        System.Diagnostics.Debug.WriteLine("App OnStart called");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("App OnSleep called");
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("App OnResume called");
    }
}
