using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;

namespace Aeolus;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS10_0_19041_0_OR_GREATER
                    events.AddWindows(windows => windows
                        .OnWindowCreated((window) => Aeolus.WinUI.App.SetWindowSize(window)));
#endif
            })
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
#if WINDOWS
		builder.Services.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
#endif
        builder.Services.AddTransient<ProjectDirectories>();
        builder.Services.AddTransient<ProjectDirectoryProjects>();
        builder.Services.AddTransient<RecentProjects>();
        builder.Services.AddTransient<OptionsPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<ProjectLoadFailedPage>();
        builder.Services.AddTransient<App>();
        builder.UseMauiCommunityToolkit();
        return builder.Build();
	}
}
