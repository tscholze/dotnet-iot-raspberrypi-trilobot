using Microsoft.Extensions.Logging;

namespace TriloBot.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		   var builder = MauiApp
			   .CreateBuilder()
			   .UseMauiApp<App>()
			   .ConfigureFonts(fonts =>
			   {
				   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			   });

		   // Register HubConnectionService as singleton
		   builder.Services.AddSingleton<Services.HubConnectionService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
