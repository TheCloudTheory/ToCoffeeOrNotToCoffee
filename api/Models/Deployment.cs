using System.Collections.Generic;
using System.Linq;

namespace api.Models
{
    public class Deployment
    {
        public string serviceName { get; private set; }
        public DeploymentMetadata[] metadata { get; private set; }

        public Deployment(string key, IEnumerable<DeploymentTable> metadata)
        {
            this.serviceName = MapKeyToServiceName(key);
            this.metadata = metadata.Select(_ => new DeploymentMetadata(_.DurationInSeconds, _.DateAndTime)).ToArray();
        }

        private string MapKeyToServiceName(string key)
        {
            switch (key)
            {
                case "appServicePlan": return "App Service Plan";
                case "applicationInsights": return "Application Insights";
                case "containerRegistry": return "Container Registry";
                case "eventGridTopic": return "Event Grid(Topic)";
                case "eventHubNamespace": return "Event Hub(Namespace)";
                case "kubernetesService": return "Kubernetes Service";
                case "storageAccount": return "Storage Account(V2)";
                case "webApp": return "Web App";
                case "virtualNetwork": return "Virtual Network";
                case "loadBalancer": return "Load Balancer(Basic)";
                case "sqlServer": return "Azure SQL Server";
                case "sqlDatabase": return "Azure SQL Database";
                default: return key;
            }
        }
    }

    public class DeploymentMetadata
    {
        public int duration { get; private set; }
        public string dateAndTime { get; private set; }

        public DeploymentMetadata(int duration, string dateAndTime)
        {
            this.duration = duration;
            this.dateAndTime = dateAndTime;
        }
    }
}