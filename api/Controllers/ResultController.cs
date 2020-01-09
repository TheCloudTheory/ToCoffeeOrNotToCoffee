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

            var queryResult = await ExecuteQuery();
            var result = queryResult.GroupBy(_ => string.IsNullOrEmpty(_.ServiceType) ? _.PartitionKey : _.ServiceType)
                            .Select(_ => new Deployment(_.Key, _.Take(24).Select(d => d))).ToArray();

            Cache.Clear();
            Cache.Add(now, result);

            return result;
        }

        private async Task<List<DeploymentTable>> ExecuteQuery()
        {
            var query = new TableQuery<DeploymentTable>();
            var table = this.storage.Client.GetTableReference("deployments");
            TableContinuationToken ct = null;
            var results = new List<DeploymentTable>();

            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync<DeploymentTable>(query, ct);
                ct = queryResult.ContinuationToken;
                results.AddRange(queryResult.Results);
            }
            while (ct != null && string.IsNullOrWhiteSpace(ct.NextPartitionKey) == false);

            return results;
        }
    }
}
