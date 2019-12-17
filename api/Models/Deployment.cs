namespace api.Models
{
    public class Deployment
    {
        public string serviceName { get; private set; }
        public int duration { get; private set; }

        public Deployment(DeploymentTable table)
        {
            this.serviceName = table.PartitionKey;
            this.duration = table.DurationInSeconds;
        }
    }
}