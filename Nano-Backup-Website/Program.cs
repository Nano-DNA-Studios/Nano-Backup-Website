using NanoBackupWebsite.Components;

namespace NanoBackupWebsite
{
    public class Program
    {
        private static int HTTPPort = 80;
        private static int HTTPSPort = 443;

        public static void Main(string[] args)
        {
            LoadEnv();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string certPath = builder.Configuration["CertPath"] ?? "";
            string certPassword = builder.Configuration["CertPassword"] ?? "";

            if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(certPassword) || !Path.Exists(certPath))
                throw new Exception("Certification Path or Password are not Set Properly");

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(HTTPPort); // HTTP
                options.ListenAnyIP(HTTPSPort, listenOptions =>
                {
                    listenOptions.UseHttps(certPath, certPassword);
                });
            });

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddScoped<FileNavigatorService>();
            builder.Services.AddControllers();

            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            app.MapControllers();

            app.Run();
        }

        private static void LoadEnv()
        {
            string envPath = ".env";

            if (!Path.Exists(envPath))
                return;

            foreach (string line in File.ReadLines(envPath))
            {
                //Skip Empty Lines and Comments 
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                var key = parts[0];
                var value = parts[1];

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
