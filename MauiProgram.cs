using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EHMR
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;
        public static IMauiContext CurrentMauiContext { get; private set; } = default!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // =====================================================
            // APP CORE
            // =====================================================
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp();

            // =====================================================
            // FONTS
            // =====================================================
            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FontIcons");
                fonts.AddFont("fa-solid-900.ttf", "FASolid");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                ;
            });
            builder.Services.AddSingleton(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive=true,
                Converters=
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
            });

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();
            builder.Services.RegisterEHMR();

            // =====================================================
            // BUILD APP
            // =====================================================
            var app = builder.Build();

            ServiceProvider=app.Services;

#if ANDROID||WINDOWS
            CurrentMauiContext=app.Services.GetService<IMauiContext>();
#endif

            return app;
        }
    }
}