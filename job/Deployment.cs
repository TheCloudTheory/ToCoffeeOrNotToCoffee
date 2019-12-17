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
        }

        public int DurationInSeconds { get; set; }
    }
}