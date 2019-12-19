using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResultController : ControllerBase
    {
        private static IDictionary<string, Deployment[]> Cache = new Dictionary<string, Deployment[]>();

        private readonly StorageClient storage;

        public ResultController(StorageClient storage)
        {
            this.storage = storage;
        }

        [HttpGet]
        public async Task<Deployment[]> Get()
        {
            var now = DateTime.Now.ToString("yyyyMMddHH");

            if (Cache.ContainsKey(now))
            {
                return Cache[now];
            }

            var table = this.storage.Client.GetTableReference("deployments");
            var queryResult = await table.ExecuteQuerySegmentedAsync<DeploymentTable>(new TableQuery<DeploymentTable>(), null);
            var result = queryResult.Results.GroupBy(_ => _.PartitionKey)
                            .Select(_ => new Deployment(_.Key, _.Take(24).Select(d => d))).ToArray();

            Cache.Clear();
            Cache.Add(now, result);

            return result;
        }
    }
}
