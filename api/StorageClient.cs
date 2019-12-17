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
            var connectionString = configuration.GetSection("Storage").GetValue(typeof(string), "ConnectionString");
            var account = CloudStorageAccount.Parse(connectionString.ToString());
            
            Client = account.CreateCloudTableClient();
        }
    }
}