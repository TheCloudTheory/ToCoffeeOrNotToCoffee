using Microsoft.WindowsAzure.Storage.Table;

namespace api
{
    public class DeploymentTable : TableEntity
    {
        public int DurationInSeconds { get; set; }
        public string DateAndTime { get; set; }
        public string ServiceType { get; set; }
    }
}