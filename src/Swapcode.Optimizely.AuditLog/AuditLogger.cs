using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.Activities;
using EPiServer.Security;
using Microsoft.Extensions.Logging;
using Swapcode.Optimizely.AuditLog.Activities;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// Implementation of <see cref="IAuditLogger"/>.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly IActivityRepository _activityRepository;
        private readonly ILogger<AuditLogger> _logger;

        /// <summary>
        /// Creates nww instance.
        /// </summary>
        /// <param name="activityRepository">Instance of <see cref="IActivityRepository"/>.</param>
        /// <param name="logger">Instance of <see cref="ILogger{AuditLogger}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="activityRepository"/> or <paramref name="logger"/> is null.</exception>
        public AuditLogger(IActivityRepository activityRepository, ILogger<AuditLogger> logger)
        {
            _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs the access right change.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Content security change event arguments.</param>
        public void AccessRightsChanged(object sender, ContentSecurityEventArg e)
        {
            try
            {
                if (e is null)
                {
                    _logger.LogError("AccessRightsChanged called with null for ContentSecurityEventArg.");
                    return;
                }

                // what access rights changes were made, target can be user or group (including visitor groups if those are set to be usable to protect content)
                // Note, permissions list can be null
                var permissions = e.ContentSecurityDescriptor?
                    .Entries?
                    .Select(entry => $"{entry.EntityType} {entry.Name} access level set to {entry.Access}.")
                    .ToList();

                // this is always null/empty, why? one would assume we would get the creator info here
                //string creator = e.ContentSecurityDescriptor.Creator;

                // this is guranteed to return a valid principal, so use this instead of creator
                string userFromContext = PrincipalInfo.CurrentPrincipal.Identity.Name;

                // log also using the logger implementation
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    // create the log message of the access rights change(s)
                    string msg = $"Access rights changed by '{userFromContext}' to content id {e.ContentLink}, save type: {e.SecuritySaveType}. Following changes were made: {(permissions is null ? "<unknown>" : string.Join(" ", permissions))}.";

                    _logger.LogInformation(msg);
                }

                var activity = new ContentSecurityActivity(e.SecuritySaveType, CreateActivityData(userFromContext, e, permissions));

                // This stinks, but there really is no other option for now
                // https://github.com/dotnet/runtime/issues/64849#issuecomment-1030678735
                _activityRepository
                    .SaveAsync(activity)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle content security saved event.");
            }
        }

        private static Dictionary<string, string> CreateActivityData(string username, ContentSecurityEventArg e, List<string> permissions)
        {
            // NOTE! The UI formats the dictionary entries using br as separator
            // and to not to care how the UI behaves, just add permissions to dictionary
            // with a Change-X key, so we get those "nicely" printed out in UI
            // Also it seems the entries are printed out in the order they were inserted to the dictionary

            // e.ContentLink will include version information if access rights change is done from edit mode
            // if done from admin access rights it is without version information
            // so if we want to show same version we should call the extension method e.ContentLink?.ToReferenceWithoutVersion()

            Dictionary<string, string> messages = new()
            {
                { "Message", $"Access rights changed by '{username}'." },
                { "Target", $"Content id '{e.ContentLink}'." },
                { "Change", $"Save type '{e.SecuritySaveType}'." }
            };

            if (permissions is not null)
            {
                for (int i = 0; i < permissions.Count; i++)
                {
                    messages.Add($"Change-{i + 1}", permissions[i]);
                }
            }

            return messages;
        }
    }
}
