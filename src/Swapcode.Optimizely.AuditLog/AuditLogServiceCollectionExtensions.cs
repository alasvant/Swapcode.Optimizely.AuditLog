using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// AuditLog <see cref="IServiceCollection"/> extension methods.
    /// </summary>
    public static class AuditLogServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AuditLog.
        /// </summary>
        /// <param name="services">Instance of <see cref="IServiceCollection"/>.</param>
        /// <returns>Same instance given in <paramref name="services"/>.</returns>
        /// <exception cref="ArgumentNullException">The instance of <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddAuditLog(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IAuditLogger, AuditLogger>();

            // register embedded XML localization files from this assembly
            return services.AddEmbeddedLocalization<AuditLogger>();
        }
    }
}
