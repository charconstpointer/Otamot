using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ServiceProcess;

namespace Otamot.Agent
{
    public class Service
    {
        public ServiceController Controller { get; }
        public ServiceControllerStatus Status { get; set; } = ServiceControllerStatus.ContinuePending;
        private readonly ICollection<IPolicy> _policies = new List<IPolicy>();
        public IEnumerable<IPolicy> Policies => _policies.ToImmutableList();

        public Service(ServiceController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void AddPolicy(IPolicy policy)
        {
            _policies.Add(policy);
        }
    }
}