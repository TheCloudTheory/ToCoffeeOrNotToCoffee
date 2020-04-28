using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace job
{
    public static class DeleteResourceGroups
    {
        private static IAzure azure = Authentication.Authenticate();

        [FunctionName("DeleteResourceGroups")]
        public static async Task Run(
            [TimerTrigger("0 0 */1 * * *")]TimerInfo myTimer,
            ILogger log
        )
        {
            var resourceGroups = await azure.ResourceGroups.ListByTagAsync("Origin", "ToCoffeeOrNotToCoffee");
            foreach (var resourceGroup in resourceGroups)
            {
                if(resourceGroup.ProvisioningState == "Succeeded") {
                    log.LogInformation($"Deleting {resourceGroup.Name}...");

                    // Do not wait for completion...
                    azure.ResourceGroups.DeleteByNameAsync(resourceGroup.Name).ContinueWith((_) => {
                        log.LogInformation($"{resourceGroup.Name} deleted!");
                    });
                }
            }
        }
    }
}