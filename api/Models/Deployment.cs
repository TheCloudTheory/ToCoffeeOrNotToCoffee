using System.Collections.Generic;
using System.Linq;

namespace api.Models
{
    public class Deployment
    {
        public string serviceName { get; private set; }
        public DeploymentMetadata[] metadata { get; private set; }

        public Deployment(string serviceName, IEnumerable<DeploymentTable> metadata)
        {
            this.serviceName = serviceName;
            this.metadata = metadata.Select(_ => new DeploymentMetadata(_.DurationInSeconds, _.DateAndTime)).ToArray();
        }
    }

    public class DeploymentMetadata
    {
        public int duration { get; private set; }
        public string dateAndTime { get; private set; }

        public DeploymentMetadata(int duration, string dateAndTime)
        {
            this.duration = duration;
            this.dateAndTime = dateAndTime;
        }
    }
}