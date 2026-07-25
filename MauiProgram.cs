using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using EHMR.Backups.Services;
using Microsoft.Extensions.Configuration;
using EHMR.Backups.Encryption;
using EHMR.Domain.SparkForm;
using EHMR.Backups.Models;



#if WINDOWS
using Microsoft.UI.Xaml.Media;
using WinColor = Windows.UI.Color;
#endif

namespace EHMR
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;
        public static IMauiContext CurrentMauiContext { get; private set; } = default!;
        public static MauiApp CreateMauiApp()        {
            var builder = MauiApp.CreateBuilder();

            // =====================================================
            // APP CORE
            // =====================================================
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp();
#if WINDOWS
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("CustomPickerColors", (handler, view) =>
            {
                handler.PlatformView.Foreground=new SolidColorBrush(WinColor.FromArgb(255, 0, 0, 0));       // black
                handler.PlatformView.Background=new SolidColorBrush(WinColor.FromArgb(255, 255, 255, 255)); // white

                handler.PlatformView.Loaded+=(s, e) =>
                {
                    handler.PlatformView.Resources["ComboBoxItemForeground"]=
                        new SolidColorBrush(WinColor.FromArgb(255, 0, 0, 0));
                    handler.PlatformView.Resources["ComboBoxItemForegroundSelected"]=
                        new SolidColorBrush(WinColor.FromArgb(255, 0, 0, 0));
                    handler.PlatformView.Resources["ComboBoxDropDownBackground"]=
                        new SolidColorBrush(WinColor.FromArgb(255, 255, 255, 255));
                    handler.PlatformView.Resources["ComboBoxDropDownBackgroundPointerOver"]=
                        new SolidColorBrush(WinColor.FromArgb(240, 245, 245, 245));
                };
            });
#endif
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
            var configuration = new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .Build();

            builder.Configuration.AddConfiguration(configuration);

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();
            builder.Services.RegisterEHMR();
            //  services.Configure<EncryptionOptions>(Configuration.GetSection("Encryption"));
            builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));

            var keyCheck = configuration.GetSection("Encryption")["Key"];
            if(string.IsNullOrWhiteSpace(keyCheck))
                throw new InvalidOperationException(
                    $"Encryption:Key not found. Config base path: {AppContext.BaseDirectory}. "+
                    $"Does appsettings.json exist there? {File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))}");
            builder.Services.Configure<EncryptionOptions>(
     builder.Configuration.GetSection(EncryptionOptions.SectionName));

            builder.Services.AddOptions<EncryptionOptions>()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Encryption:Key is missing or empty — check appsettings.json is copied to output and the section name matches.")
                .ValidateOnStart();

            builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
            SparkTemplateInitializer.Register();
            // =====================================================
            // BUILD APP
            // =====================================================
            var app = builder.Build();

            ServiceProvider=app.Services;

#if ANDROID||WINDOWS
            CurrentMauiContext=app.Services.GetService<IMauiContext>();
#endif

            return app;

            static WinColor ToWinColor(Microsoft.Maui.Graphics.Color c) =>
      WinColor.FromArgb((byte)(c.Alpha*255), (byte)(c.Red*255), (byte)(c.Green*255), (byte)(c.Blue*255));


        } }
}