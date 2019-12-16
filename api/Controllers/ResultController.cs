using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResultController : ControllerBase
    {
        [HttpGet]
        public DeploymentResults Get()
        {
            return new DeploymentResults();
        }
    }
}
