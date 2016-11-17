using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInsights.TelemetryReplication
{
    internal class ServiceProviderTelemetryReplicatorsFactory : ITelemetryReplicatorsFactory
    {
        private readonly IServiceProvider serviceProvider;
        public ServiceProviderTelemetryReplicatorsFactory(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            this.serviceProvider = serviceProvider;
        }
        public IEnumerable<ITelemetryReplicator> Create()
        {
            return serviceProvider.GetService<IEnumerable<ITelemetryReplicator>>();
        }
    }
}
