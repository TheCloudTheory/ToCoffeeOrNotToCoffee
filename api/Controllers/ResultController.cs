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
        private readonly StorageClient storage;

        public ResultController(StorageClient storage)
        {
            this.storage = storage;
        }

        [HttpGet]
        public async Task<Deployment[]> Get()
        {
            var table = this.storage.Client.GetTableReference("deployments");
            var queryResult = await table.ExecuteQuerySegmentedAsync<DeploymentTable>(new TableQuery<DeploymentTable>(), null);
            var result = queryResult.Results.GroupBy(_ => _.PartitionKey).Select(_ => new Deployment(_.Key, _.Select(d => d.DurationInSeconds))).ToArray();

            return result;
        }
    }
}
