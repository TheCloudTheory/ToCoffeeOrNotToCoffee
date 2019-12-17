using System;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace job
{
    public static class DeployTemplates
    {
        private static IAzure azure = Authenticate();

        [FunctionName("DeployTemplates")]
        public static void Run([TimerTrigger("0 * */1 * * *")]TimerInfo myTimer, ILogger log)
        {
            var resourceGroupName = GenerateResourceGroupName("virtualMachine");
            var deploymentName = GenerateResourceGroupName("virtualMachine");

            azure.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(Region.USWest)
                    .Create();

            azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithTemplate("{}")
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Complete)
                    .Create();
        }

        private static string GenerateResourceGroupName(string serviceName)
        {
            var now = DateTime.Now;
            return $"{serviceName}-{now.ToString("yyyMMddHHmm")}-rg";
        }

        private static string GenerateDeploymentName(string serviceName)
        {
            var now = DateTime.Now;
            return $"{serviceName}-{now.ToString("yyyMMddHHmm")}";
        }

        private static IAzure Authenticate()
        {
            var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            return azure;
        }
    }
}
