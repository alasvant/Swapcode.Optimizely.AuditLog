using System;
using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.Activities;
using EPiServer.DataAbstraction.Activities.Internal;
using EPiServer.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swapcode.Optimizely.AuditLog.Activities;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// AuditLog extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class AuditLogApplicationBuilderExtensions
    {
        /// <summary>
        /// Enables audit logging to CMS audit log.
        /// </summary>
        /// <param name="app">Instance of <see cref="IApplicationBuilder"/>.</param>
        /// <returns><paramref name="app"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="app"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Required service <see cref="IContentSecurityEvents"/> or <see cref="IActivityTypeRegistry"/> or <see cref="IAuditLogger"/> is not available.</exception>
        public static IApplicationBuilder UseAuditLog(this IApplicationBuilder app)
        {
            if(app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var contenSecurityEvents = app.ApplicationServices.GetRequiredService<IContentSecurityEvents>();
            var auditLogger = app.ApplicationServices.GetRequiredService<IAuditLogger>();

            RegisterContentSecurityActivity(app.ApplicationServices.GetRequiredService<IActivityTypeRegistry>());

            contenSecurityEvents.ContentSecuritySaved += auditLogger.AccessRightsChanged;

            return app;
        }

        /// <summary>
        /// Registers the ContentSecurityActivity
        /// </summary>
        /// <param name="activityTypeRegistry">Instance of <see cref="IActivityTypeRegistry"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="activityTypeRegistry"/> is null reference.</exception>
        private static void RegisterContentSecurityActivity(IActivityTypeRegistry activityTypeRegistry)
        {
            // this is similiar code that the Episerver implementations uses to register the activities
            ActivityType activityType = new(ContentSecurityActivity.ActivityTypeName,
                from SecuritySaveType x in Enum.GetValues(typeof(SecuritySaveType))
                select new ActionType((int)x, x.ToString()));

            // the implementation calls AddOrUpdate so it is safe to always call it
            // the backing type currently is ConcurrentDictionary not database
            activityTypeRegistry.Register(activityType);
        }
    }
}
