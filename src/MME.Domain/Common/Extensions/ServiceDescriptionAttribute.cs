using Microsoft.Extensions.DependencyInjection;

namespace MME.Domain.Common.Extensions
{
    public class ServiceDescriptionAttribute : Attribute
    {
        public ServiceDescriptionAttribute(Type serviceType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
        }

        public Type ServiceType { get; set; }

        public ServiceLifetime Lifetime { get; set; }
    }
}
