using BarcodeScanning;
using CentersBarCode.Services;
using CentersBarCode.ViewModels;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace CentersBarCode;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeScanning()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).AddAudio();

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<RecordsViewModel>();
        builder.Services.AddTransient<AttachCardViewModel>();
        builder.Services.AddTransient<ManualAddViewModel>();
        builder.Services.AddSingleton<AppShellViewModel>();

        // Register GoogleAuthService as both the interface and concrete type
        // This allows injection of either the interface or concrete type
        builder.Services.AddSingleton<GoogleAuthService>();
        builder.Services.AddSingleton<IGoogleAuthService>(sp => sp.GetRequiredService<GoogleAuthService>());

        // Register Authentication Service
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

        // Register Database Service
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        // Register HTTP Client and API Services
        builder.Services.AddHttpClient<IApiService, ApiService>();
        builder.Services.AddSingleton<IStudentService, StudentService>();
        builder.Services.AddSingleton<ICenterService, CenterService>();

        // Register pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RecordsPage>();
        builder.Services.AddTransient<AttachCardPage>();
        builder.Services.AddTransient<ManualAddPage>();
        builder.Services.AddSingleton<SplashScreen>();

        // Add essentials for secure storage
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

        return builder.Build();
    }
}
