using System.Collections.Generic;
using System.Linq;

namespace api.Models
{
    public class Deployment
    {
        public string serviceName { get; private set; }
        public int[] durations { get; private set; }

        public Deployment(string key, IEnumerable<int> durations)
        {
            this.serviceName = key;
            this.durations = durations.ToArray();
        }
    }
}