namespace api.Models
{
    public class Deployment
    {
        public string serviceName { get; private set; }
        public int[] last24DeploymentsDuration { get; private set; }

        public Deployment(string serviceName, int[] last24DeploymentsDurationInSeconds)
        {
            this.serviceName = serviceName;
            this.last24DeploymentsDuration = last24DeploymentsDurationInSeconds;
        }
    }
}