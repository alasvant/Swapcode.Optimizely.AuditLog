using System;

namespace Swapcode.Optimizely.AuditLog
{
    internal static class InternalExtensions
    {
        /// <summary>
        /// Tries to get the requested service from the <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <typeparam name="TService">Type of the service to get.</typeparam>
        /// <param name="serviceProvider">Instance of <see cref="IServiceProvider"/>, if null this method will always return false.</param>
        /// <param name="service">Instance of the requested service if this method returns true, otherwise the default value of the requested type.</param>
        /// <returns>True if the requested service was found, otherwise false.</returns>
        internal static bool TryGetService<TService>(this IServiceProvider serviceProvider, out TService service) where TService : class
        {
            if (serviceProvider?.GetService(typeof(TService)) is TService foundService)
            {
                service = foundService;
                return true;
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Convinience method to get the requested service from the <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <typeparam name="TService">Type of the service to get.</typeparam>
        /// <param name="serviceProvider">Instance of <see cref="IServiceProvider"/>, if null the null is returned.</param>
        /// <returns>The requeste dservice or null if not found.</returns>
        internal static TService GetService<TService>(this IServiceProvider serviceProvider) where TService : class
        {
            return serviceProvider?.GetService(typeof(TService)) as TService;
        }
    }
}
