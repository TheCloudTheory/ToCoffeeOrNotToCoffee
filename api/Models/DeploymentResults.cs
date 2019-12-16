using System.Collections.Generic;

namespace api.Models
{
    public class DeploymentResults
    {
        public IEnumerable<Deployment> Deployments { get; private set; }

        public DeploymentResults()
        {
            Deployments = new[] {
                new Deployment("Virtual Machine", new[] {93, 123, 45, 76, 55, 156, 230, 12, 98, 56, 34, 90, 98, 45, 123, 111, 23, 45, 67, 89, 74, 23, 67, 120}),
                new Deployment("Web App", new[] {34, 12, 11, 76, 125, 116, 130, 120, 98, 46, 34, 190, 28, 45, 53, 111, 123, 45, 67, 89, 44, 123, 67, 220}),
                new Deployment("Azure Kubernetes Service", new[] {931, 1232, 456, 767, 555, 1563, 2301, 1234, 980, 566, 324, 910, 928, 454, 1233, 1111, 235, 456, 671, 893, 748, 234, 679, 1203})
            };
        }
    }
}