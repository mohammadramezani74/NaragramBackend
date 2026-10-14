using CleanArchitecture.Application.Bots.Flows;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanArchitecture.Application.Bots
{
    /// <summary>
    /// فهرست جریان‌ها و اتصال‌هایی که در کد وجود دارند. موقع بالا آمدن برنامه
    /// اسمبلی‌ها گشته می‌شوند (همان کاری که برای IEndpoint انجام می‌شود)؛ پس
    /// افزودن یک جریان یا اتصال تازه فقط نوشتن کلاسش است، ثبت دستی لازم نیست.
    /// </summary>
    public sealed class BotPartsRegistry
    {
        private readonly Dictionary<string, BotFlowDescriptor> _flows = new(StringComparer.Ordinal);
        private readonly List<Type> _connectorTypes = new();
        private readonly HashSet<Assembly> _scanned = new();

        public IReadOnlyCollection<BotFlowDescriptor> Flows => _flows.Values;

        public IReadOnlyList<Type> ConnectorTypes => _connectorTypes;

        public BotFlowDescriptor? FindFlow(string? key)
            => key is not null && _flows.TryGetValue(key, out var flow) ? flow : null;

        internal IReadOnlyList<Type> Scan(Assembly assembly)
        {
            if (!_scanned.Add(assembly))
                return Array.Empty<Type>();

            var types = assembly.DefinedTypes.Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }).ToList();

            foreach (var type in types.Where(t => typeof(IBotFlow).IsAssignableFrom(t)))
            {
                var attr = type.GetCustomAttribute<BotFlowAttribute>();
                if (attr is null)
                    continue;

                if (_flows.ContainsKey(attr.Key))
                    throw new InvalidOperationException($"Duplicate bot flow key '{attr.Key}' on {type.FullName}.");

                var parameters = type.GetCustomAttributes<FlowParamAttribute>()
                    .Select(p => new BotFlowParamDescriptor(p.Key, p.Title, p.Required, p.Options))
                    .ToList();

                _flows[attr.Key] = new BotFlowDescriptor(attr.Key, attr.Title, attr.Description, type.AsType(), parameters);
            }

            var connectors = types.Where(t => typeof(IBotConnector).IsAssignableFrom(t)).Select(t => t.AsType()).ToList();
            _connectorTypes.AddRange(connectors);
            return connectors;
        }
    }

    public static class BotPartsServiceCollectionExtensions
    {
        /// <summary>
        /// جریان‌ها و اتصال‌های یک اسمبلی را ثبت می‌کند. می‌شود چند بار برای
        /// اسمبلی‌های مختلف صدا زد (Application، Infrastructure و ...).
        /// </summary>
        public static IServiceCollection AddBotParts(this IServiceCollection services, Assembly assembly)
        {
            var registry = services
                .Where(d => d.ServiceType == typeof(BotPartsRegistry))
                .Select(d => d.ImplementationInstance)
                .OfType<BotPartsRegistry>()
                .FirstOrDefault();

            if (registry is null)
            {
                registry = new BotPartsRegistry();
                services.AddSingleton(registry);
            }

            foreach (var connector in registry.Scan(assembly))
            {
                services.AddScoped(connector);
                services.AddScoped(typeof(IBotConnector), sp => sp.GetRequiredService(connector));

                // اتصال‌ها معمولاً پشت یک interface مخصوص خودشان هستند (مثلاً IHrConnector)
                // تا جریان‌ها به پیاده‌سازی وابسته نباشند و بشود منبع را عوض کرد.
                foreach (var contract in connector.GetInterfaces()
                             .Where(i => i != typeof(IBotConnector) && typeof(IBotConnector).IsAssignableFrom(i)))
                {
                    services.AddScoped(contract, sp => sp.GetRequiredService(connector));
                }
            }

            return services;
        }
    }
}
