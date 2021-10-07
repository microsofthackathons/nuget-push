using Azure.Core;
using Azure.Identity;
using BaGet.Protocol;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FakeGet
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config
                    .AddCommand<PushCommand>("push")
                    .WithDescription("Upload a package to a NuGet server.")
                    .WithExample(new[] { "package.nupkg", "--source", "https://api.nuget.org/v3/index.json" });

                config
                    .AddCommand<RestoreCommand>("restore")
                    .WithDescription("Download packages.");
            });

            app.Run(args);
        }
    }

    public class PushCommand : AsyncCommand<PushCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [Description("Path of package to push")]
            [CommandArgument(0, "[packagePath]")]
            [NotNull]
            public string PackagePath { get; init; }

            [CommandOption("-s|--source")]
            [Description("The package source URL to upload the package")]
            [DefaultValue("https://api.nuget.org/v3/index.json")]
            public string PackageSource { get; init; }

            [CommandOption("-k|--api-key")]
            [Description("The API key for the server")]
            public string ApiKey { get; init; }

            [CommandOption("--interactive")]
            [DefaultValue(false)]
            [Description("Allow the command to block and require manual action for operations like authentication.")]
            public bool Interactive { get; init; }
        }

        public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrEmpty(settings.PackagePath))
            {
                return ValidationResult.Error("You must provide a package path");
            }

            return base.Validate(context, settings);
        }

        public override async Task<int> ExecuteAsync(CommandContext ctx, Settings settings)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), settings.PackagePath);
            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]Could not find '{settings.PackagePath}'[/]");
                return 1;
            }

            using var http = new HttpClient();

            var publishUrl = await DiscoverPublishUrlAsync(http, settings.PackageSource);

            if (await UploadPackageAsync(settings, http, publishUrl, interactiveRetry: false))
            {
                return 0;
            }

            return 1;
        }

        private async Task<string> DiscoverPublishUrlAsync(HttpClient http, string packageSource)
        {
            var clientFactory = new NuGetClientFactory(http, packageSource);
            var serviceIndexClient = clientFactory.CreateServiceIndexClient();

            var serviceIndex = await serviceIndexClient.GetAsync();

            return serviceIndex.GetRequiredResourceUrl(new[] { "PackagePublish/2.0.0" }, "PackagePublish");
        }

        private async Task<bool> UploadPackageAsync(
            Settings settings,
            HttpClient http,
            string publishUrl,
            bool interactiveRetry)
        {
            using var package = File.OpenRead(settings.PackagePath);

            using var uploadRequest = new HttpRequestMessage();

            uploadRequest.RequestUri = new System.Uri(publishUrl);
            uploadRequest.Method = HttpMethod.Put;
            uploadRequest.Content = new StreamContent(package);

            if (interactiveRetry)
            {
                var token = await AcquireTokenAsync();
                uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                AnsiConsole.MarkupLine("");
            }
            else
            {
                uploadRequest.Headers.Add("X-NuGet-ApiKey", settings.ApiKey);
            }

            var fileName = Path.GetFileName(settings.PackagePath);

            AnsiConsole.WriteLine($"Pushing {fileName} to '{publishUrl}'...");
            AnsiConsole.WriteLine($"  PUT {publishUrl}");

            var stopwatch = Stopwatch.StartNew();
            var response = await http.SendAsync(uploadRequest);

            AnsiConsole.WriteLine($"  {response.ReasonPhrase} {publishUrl} {stopwatch.ElapsedMilliseconds}ms");
            AnsiConsole.WriteLine();

            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Accepted)
            {
                AnsiConsole.WriteLine("The package was successfully pushed.");
                return true;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                AnsiConsole.MarkupLine("[red]Error: The provided package is invalid.[/]");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: A package with the provided ID and version already exists.[/]");
                return false;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (!string.IsNullOrEmpty(settings.ApiKey) && !settings.Interactive)
                {
                    AnsiConsole.MarkupLine(
                        "[red]Error: Your API key was rejected. " +
                        "Consider trying again with --interactive[/]");
                    return false;
                }

                else if (!settings.Interactive)
                {
                    AnsiConsole.MarkupLine(
                        "[red]Error: Unauthorized. Please try again with --interactive enabled " +
                        "or providing an API key with --api-key[/]");
                    return false;
                }

                else if (interactiveRetry)
                {
                    AnsiConsole.MarkupLine("[red]Error: Interactive login failed. Please try again.[/]");
                    return false;
                }

                else
                {
                    // Retry with authorization.
                    return await UploadPackageAsync(
                        settings,
                        http,
                        publishUrl,
                        interactiveRetry: true);
                }
            }

            AnsiConsole.MarkupLine($"[red]Unexpected response {response.StatusCode} - {response.ReasonPhrase}.[/]");
            return false;
        }

        private async Task<string> AcquireTokenAsync()
        {
            var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
            {
                TenantId = "65835d77-014d-4568-9bac-9804d2200f87",
                ClientId = "aeeee31c-bf5c-4835-82e9-2e19842c6fba",
            });

            var request = new TokenRequestContext(
                scopes: new string[] { "api://a4227f47-fd47-4586-a64b-609c3f0ebfd7/upload_package" },
                tenantId: "65835d77-014d-4568-9bac-9804d2200f87");

            var result = await credential.GetTokenAsync(request);

            return result.Token;
        }
    }

    public class RestoreCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            AnsiConsole.MarkupLine("Fake it till you make it :)");
            return 0;
        }
    }
}
