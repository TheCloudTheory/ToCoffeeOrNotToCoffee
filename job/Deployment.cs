using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace job
{
    public class Deployment : TableEntity
    {
        public Deployment(string serviceName)
        {
            PartitionKey = serviceName;
            RowKey = (DateTime.MaxValue.Ticks - DateTime.Now.Ticks).ToString();
            DateAndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        public int DurationInSeconds { get; set; }
        public string DateAndTime { get; set; }
        public string Version { get; set; }
        public string ServiceType { get; set; }
    }
}