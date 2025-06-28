using CentersBarCode.Services;
using CentersBarCode.ViewModels;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace CentersBarCode;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseBarcodeReader()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		 // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });

		 // Register services
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<RecordsViewModel>();
		builder.Services.AddTransient<AttachCardViewModel>();
		builder.Services.AddSingleton<AppShellViewModel>();
		
		// Register GoogleAuthService as both the interface and concrete type
        // This allows injection of either the interface or concrete type
        builder.Services.AddSingleton<GoogleAuthService>();
        builder.Services.AddSingleton<IGoogleAuthService>(sp => sp.GetRequiredService<GoogleAuthService>());
		
		// Register Authentication Service
		builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
		
		// Register Database Service
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		
		// Register pages
		builder.Services.AddTransient<Views.MainPage>();
		builder.Services.AddTransient<Views.LoginPage>();
		builder.Services.AddTransient<Views.RecordsPage>();
		builder.Services.AddTransient<Views.AttachCardPage>();
		builder.Services.AddSingleton<Views.SplashScreen>();
		
		// Add essentials for secure storage
		builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

		return builder.Build();
	}
}
