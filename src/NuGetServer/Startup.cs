using BaGet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

namespace NuGetServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<GitHubClient>();

            services.Configure<GitHubOptions>(Configuration.GetSection("GitHub"));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration);

            services.AddBaGetWebApplication(app =>
            {
                // Use SQLite as BaGet's database and store packages on the local file system.
                app.AddSqliteDatabase();
                app.AddFileStorage();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "github",
                    pattern: "github-webhook",
                    defaults: new { controller = "Webhook", action = "GitHubWebhook" }
                );

                // Add BaGet's endpoints.
                endpoints.MapRazorPages();

                var baget = new BaGetEndpointBuilder();
                baget.MapServiceIndexRoutes(endpoints);
                //baget.MapPackagePublishRoutes(endpoints);
                baget.MapSymbolRoutes(endpoints);
                baget.MapSearchRoutes(endpoints);
                baget.MapPackageMetadataRoutes(endpoints);
                baget.MapPackageContentRoutes(endpoints);

                endpoints.MapControllerRoute(
                    name: "upload-package",
                    pattern: "api/v2/package",
                    defaults: new { controller = "PackagePublish", action = "Upload" },
                    constraints: new { httpMethod = new HttpMethodRouteConstraint("PUT") })
                    .RequireAuthorization();
            });
        }
    }
}