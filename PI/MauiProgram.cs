using Microsoft.Extensions.Logging;
using PI.Services;
using PI.ViewModels;
using PI.Views;

namespace PI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<ICredentialRepository, CredentialRepository>();

            // ViewModels & Views
            builder.Services.AddTransient<MasterPasswordViewModel>();
            builder.Services.AddTransient<MasterPasswordPage>();

            builder.Services.AddTransient<CredentialListViewModel>();
            builder.Services.AddTransient<CredentialListPage>();

            builder.Services.AddTransient<CredentialDetailViewModel>();
            builder.Services.AddTransient<CredentialDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
