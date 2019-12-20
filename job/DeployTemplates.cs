using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;

namespace job
{
    public static class DeployTemplates
    {
        private static IAzure azure = Authenticate();
        private static string[] services = new[] {
            "storageAccount",
            "containerRegistry",
            "eventHubNamespace",
            "eventGridTopic",
            "applicationInsights",
            "appServicePlan",
            "webApp",
            "kubernetesService",
            "virtualNetwork",
            "loadBalancer"
        };

        [FunctionName("DeployTemplates")]
        public static async Task Run(
            [TimerTrigger("0 0 */1 * * *")]TimerInfo myTimer,
            [Table("deployments", Connection = "ToCoffeeStorage")] ICollector<Deployment> table,
            ILogger log
        )
        {
            var tasks = new List<Task>();
            foreach (var serviceName in services)
            {
                var task = Task.Run(() =>
                {
                    log.LogInformation($"Deploying {serviceName} service...");

                    var deployments = DeployResource(serviceName, log);
                    deployments.ForEach(_ => table.Add(_));
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks.ToArray());
        }

        private static List<Deployment> DeployResource(string serviceName, ILogger log)
        {
            var deployments = new List<Deployment>();
            var templatesDir = Path.Combine(Environment.GetEnvironmentVariable("MAIN_DIRECTORY"), "templates", serviceName);

            foreach (var versionDirectory in Directory.EnumerateDirectories(templatesDir))
            {
                DeployResourceGroup(serviceName, versionDirectory, deployments, log);
            }

            return deployments;
        }

        private static string GenerateResourceGroupName(string serviceName)
        {
            var now = DateTime.Now;
            return $"{serviceName}-{now.ToString("yyyMMddHHmm")}-rg";
        }

        private static string GenerateDeploymentName(string serviceName, string version)
        {
            var now = DateTime.Now;
            return $"{serviceName}-{now.ToString("yyyMMddHHmm")}-{version}";
        }

        private static void DeployResourceGroup(
            string serviceName,
            string versionDirectory,
            List<Deployment> deployments,
            ILogger log)
        {
            var resourceGroupName = GenerateResourceGroupName(serviceName);

            try
            {
                azure.ResourceGroups.Define(resourceGroupName)
                        .WithRegion(Region.EuropeWest)
                        .Create();

                foreach (var template in Directory.EnumerateFiles(versionDirectory))
                {
                    DeployResource(versionDirectory, serviceName, resourceGroupName, template, deployments, log);
                }
            }
            catch (CloudException ex)
            {
                log.LogError(ex, JsonConvert.SerializeObject(ex.Body.Details));
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error while deploying resource group {resourceGroupName}!");
            }
            finally
            {
                azure.ResourceGroups.DeleteByName(resourceGroupName);
            }
        }

        private static void DeployResource(
            string versionDirectory,
            string serviceName,
            string resourceGroupName,
            string template, List<Deployment> deployments,
            ILogger log)
        {
            try
            {
                var version = new DirectoryInfo(versionDirectory).Name;
                var deploymentName = GenerateDeploymentName(serviceName, version);
                var deployment = azure.Deployments.Define(deploymentName)
                        .WithExistingResourceGroup(resourceGroupName)
                        .WithTemplate(File.ReadAllText(template))
                        .WithParameters(serviceName == "kubernetesService" ? GenerateSecretsForAks() : "{}")
                        .WithMode(DeploymentMode.Complete)
                        .Create();

                var duration = XmlConvert.ToTimeSpan(deployment.Inner.Properties.Duration);
                deployments.Add(new Deployment(serviceName)
                {
                    DurationInSeconds = (int)duration.TotalSeconds,
                    Version = version
                });
            }
            catch (CloudException ex)
            {
                log.LogError(ex, JsonConvert.SerializeObject(ex.Body.Details));
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error while deploying template {template}!");
            }
        }

        private static string GenerateSecretsForAks()
        {
            var parameters = new
            {
                sshPublicKey = new
                {
                    value = Environment.GetEnvironmentVariable("SSH_PUBLIC_KEY")
                },
                spAppId = new
                {
                    value = Environment.GetEnvironmentVariable("APPLICATION_ID")
                },
                spClientSecret = new
                {
                    value = Environment.GetEnvironmentVariable("CLIENT_SECRET")
                }
            };

            return JsonConvert.SerializeObject(parameters);
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