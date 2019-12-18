using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace api
{
    public class StorageClient
    {
        public CloudTableClient Client { get; private set; }

        public StorageClient(IConfiguration configuration)
        {
            var connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
            var account = CloudStorageAccount.Parse(connectionString.ToString());
            
            Client = account.CreateCloudTableClient();
        }
    }
}