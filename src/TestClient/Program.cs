using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace TestClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.devicecodecredential?view=azure-dotnet
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Device-Code-Flow
            
            // How to configure client and server app registrations:
            // https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/1.%20Desktop%20app%20calls%20Web%20API
            var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
            {
                TenantId = "65835d77-014d-4568-9bac-9804d2200f87",
                ClientId = "aeeee31c-bf5c-4835-82e9-2e19842c6fba",
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "hackathon-test",
                    UnsafeAllowUnencryptedStorage = false,
                }
            });

            var request = new TokenRequestContext(
                scopes: new string[] { "api://a4227f47-fd47-4586-a64b-609c3f0ebfd7/upload_package" }, // server app scope
                tenantId: "65835d77-014d-4568-9bac-9804d2200f87");


            try
            {
                AccessToken result = await credential.GetTokenAsync(request);

                Console.WriteLine();
                Console.WriteLine("Success!");

                Console.WriteLine();
                Console.WriteLine(result.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine("Sad panda!");
                Console.WriteLine();
                Console.WriteLine(e);
            }
        }
    }
}
