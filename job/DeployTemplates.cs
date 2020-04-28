using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using jon;
using Microsoft.Azure.Management.Fluent;
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
        private static IAzure azure = Authentication.Authenticate();
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
            "loadBalancer",
            "sqlServer",
            "sqlDatabase",
            "virtualMachine"
        };

        private static string[] locations = new[] {
            "North Europe",
            "West Europe",
            "France Central",
            "UK South",
            "UK West",
            "Central US",
            "East US",
            "East US 2",
            "North Central US",
            "South Central US",
        };

        [FunctionName("DeployTemplates")]
        public static async Task Run(
            [TimerTrigger("0 0 */2 * * *")]TimerInfo myTimer,
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
                if (serviceName == "virtualMachine")
                {
                    DeployResourceGroupsForVirtualMachines(versionDirectory, deployments, log);
                }
                else
                {
                    DeployResourceGroup(serviceName, versionDirectory, deployments, log);
                }
            }

            return deployments;
        }

        private static string GenerateResourceGroupName(string serviceName)
        {
            var now = DateTime.Now;
            return $"{serviceName}-{now.ToString("yyyMMddHHmm")}-rg";
        }

        private static string GenerateDeploymentName()
        {
            return Guid.NewGuid().ToString();
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
                        .WithTag("Origin", "ToCoffeeOrNotToCoffee")
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

        private static void DeployResourceGroupsForVirtualMachines(
            string versionDirectory,
            List<Deployment> deployments,
            ILogger log
        )
        {
            foreach (var location in locations)
            {
                var normalizedLocationName = location.ToLowerInvariant().Replace(" ", string.Empty);
                var resourceGroupName = GenerateResourceGroupName($"vm{normalizedLocationName}");

                try
                {
                    azure.ResourceGroups.Define(resourceGroupName)
                            .WithRegion(location)
                            .Create();

                    foreach (var template in Directory.EnumerateFiles(versionDirectory))
                    {
                        DeployResource(versionDirectory, "virtualMachine", resourceGroupName, template, deployments, log);
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
                var deploymentName = GenerateDeploymentName();
                var deployment = azure.Deployments.Define(deploymentName)
                        .WithExistingResourceGroup(resourceGroupName)
                        .WithTemplate(File.ReadAllText(template))
                        .WithParameters(GetParametersForService(serviceName))
                        .WithMode(DeploymentMode.Complete)
                        .Create();

                var duration = XmlConvert.ToTimeSpan(deployment.Inner.Properties.Duration);
                var outputs = JsonConvert.DeserializeObject<IDictionary<string, Output>>(JsonConvert.SerializeObject(deployment.Outputs));
                deployments.Add(new Deployment(serviceName)
                {
                    DurationInSeconds = (int)duration.TotalSeconds,
                    Version = version,
                    ServiceType = outputs["serviceType"].Value
                });

                log.LogInformation("Deployment of {0} on {1} finished!", serviceName, resourceGroupName);
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

        private static string GetParametersForService(string serviceName)
        {
            switch (serviceName)
            {
                case "kubernetesService": return GenerateSecretsForAks();
                case "sqlServer": return GenerateSecretsForSqlServer();
                case "sqlDatabase": return GenerateSecretsForSqlServer();
                case "virtualMachine": return GenerateParametersForVirtualMachine();
                default: return "{}";
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

        private static string GenerateSecretsForSqlServer()
        {
            var parameters = new
            {
                administratorLoginPassword = new
                {
                    value = Environment.GetEnvironmentVariable("SQL_ADMIN_PASSWORD")
                }
            };

            return JsonConvert.SerializeObject(parameters);
        }

        private static string GenerateParametersForVirtualMachine()
        {
            var parameters = new
            {
                adminPassword = new
                {
                    value = Environment.GetEnvironmentVariable("VM_ADMIN_PASSWORD")
                },
                dnsLabelPrefix = new
                {
                    value = $"a{Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 10)}"
                }
            };

            return JsonConvert.SerializeObject(parameters);
        }
    }
}