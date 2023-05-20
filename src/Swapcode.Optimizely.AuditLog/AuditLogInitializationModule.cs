using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.Activities;
using EPiServer.DataAbstraction.Activities.Internal;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Microsoft.Extensions.Logging;
using Swapcode.Optimizely.AuditLog.Activities;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// Initialization module to register <see cref="ContentSecurityActivity"/> and handling the logging of content security changes.
    /// </summary>
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public sealed class AuditLogInitializationModule : IInitializableModule
    {
        /// <summary>
        /// Logger reference.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Is the module initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Has the _logger been initialized.
        /// </summary>
        private bool _isLoggerInitialized;

        /// <summary>
        /// Reference to activity repository.
        /// </summary>
        private IActivityRepository _activityRepository;

        /// <inheritdoc/>
        public void Initialize(InitializationEngine context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_isInitialized)
            {
                return;
            }

            // get the service provider
            var serviceProvider = context.Locate?.Advanced;

            if (serviceProvider is null)
            {
                throw new ArgumentException("InitializationEngine instances 'Locate' property is null or the returned 'ServiceProviderHelper' instances 'Advanced' property didn't return instance of IServiceProvider.");
            }

            // initialize logger used by this instance
            InitializeLogger(serviceProvider);

            try
            {
                // get IActivityTypeRegistry
                var activityTypeRegistry = serviceProvider.GetService<IActivityTypeRegistry>() ?? throw new Exception("IActivityTypeRegistry service not found.");

                // register our custom activity type
                RegisterContentSecurityActivity(activityTypeRegistry);

                // get IActivityRepository service
                _activityRepository = serviceProvider.GetService<IActivityRepository>() ?? throw new Exception("Failed to get IActivityRepository.");

                // hook up content security events
                var contentSecurityEvents = serviceProvider.GetService<IContentSecurityEvents>() ?? throw new Exception("Failed to get IContentSecurityEvents.");

                contentSecurityEvents.ContentSecuritySaved += ContentSecuritySaved;

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log initialization failed. Audit logging not active.");
            }
        }

        /// <inheritdoc/>
        public void Uninitialize(InitializationEngine context)
        {
            if (_isInitialized)
            {
                try
                {
                    var serviceProvider = context?.Locate?.Advanced;

                    if (serviceProvider is not null && serviceProvider.TryGetService(out IContentSecurityEvents contentSecurityEvents))
                    {
                        contentSecurityEvents.ContentSecuritySaved -= ContentSecuritySaved;
                        _isInitialized = false;
                    }
                    else
                    {
                        _logger?.LogError("Uninitialize called but couldn't get the IContentSecurityEvents service to unregister event handler.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Uninitialize failed.");
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="IContentSecurityRepository.ContentSecuritySaved"/> event and logs the changes.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">ContentSecuritySaved event <see cref="ContentSecurityEventArg"/> where from the log message is created</param>
        private void ContentSecuritySaved(object sender, ContentSecurityEventArg e)
        {
            try
            {
                // what access rights changes were made, target can be user or group (including visitor groups if those are set to be usable to protect content)
                var permissions = e.ContentSecurityDescriptor?.Entries?.Select(entry => $"{entry.EntityType} {entry.Name} access level set to {entry.Access}.").ToList();

                // this is always null/empty, why? one would assume we would get the creator info here
                //string creator = e.ContentSecurityDescriptor.Creator;

                // this is guranteed to return a valid principal, so use this instead of creator
                string userFromContext = PrincipalInfo.CurrentPrincipal.Identity.Name;

                // log also using the logger implementation
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    // create the log message of the access rights change(s)
                    string msg = $"Access rights changed by '{userFromContext}' to content id {e.ContentLink}, save type: {e.SecuritySaveType}. Following changes were made: {string.Join(" ", permissions)}";

                    _logger.LogInformation(msg);
                }

                var activity = new ContentSecurityActivity(e.SecuritySaveType, CreateActivityData(userFromContext, e, permissions));

                var result = _activityRepository.SaveAsync(activity).GetAwaiter().GetResult();

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"New activity saved with id: {result}.");
                }
            }
            catch (Exception ex)
            {
                // important to handle exceptions here so that it will not cause issues in the UI even if this fails
                _logger.LogError(ex, "Failed to handle content security saved event.");
            }
        }

        #region Helper methods

        private static Dictionary<string, string> CreateActivityData(string username, ContentSecurityEventArg e, List<string> permissions)
        {
            // NOTE! The UI formats the dictionary entries using br as separator
            // and to not to care how the UI behaves, just add permissions to dictionary
            // with a Change-X key, so we get those "nicely" printed out in UI
            // Also it seems the entries are printed out in the order they were inserted to the dictionary

            Dictionary<string, string> messages = new()
            {
                { "Message", $"Access rights changed by '{username}'." },
                { "Target", $"Content id '{e.ContentLink}'." },
                { "Change", $"Save type '{e.SecuritySaveType}'." }
            };

            for (int i = 0; i < permissions.Count; i++)
            {
                messages.Add($"Change-{i+1}", permissions[i]);
            }

            return messages;
        }

        /// <summary>
        /// Registers the ContentSecurityActivity
        /// </summary>
        /// <param name="activityTypeRegistry">Instance of <see cref="IActivityTypeRegistry"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="activityTypeRegistry"/> is null reference.</exception>
        private static void RegisterContentSecurityActivity(IActivityTypeRegistry activityTypeRegistry)
        {
            if (activityTypeRegistry == null)
            {
                throw new ArgumentNullException(nameof(activityTypeRegistry));
            }

            // this is similiar code that the Episerver implementations uses to register the activities
            ActivityType activityType = new(ContentSecurityActivity.ActivityTypeName,
                from SecuritySaveType x in Enum.GetValues(typeof(SecuritySaveType))
                select new ActionType((int)x, x.ToString()));

            // the implementation calls AddOrUpdate so it is safe to always call it
            // the backing type currently is ConcurrentDictionary not database
            activityTypeRegistry.Register(activityType);
        }

        private void InitializeLogger(IServiceProvider serviceProvider)
        {
            if (!_isLoggerInitialized)
            {
                // try to get the logger factory and create the logger for this module
                // or fallback to internal null logger
                if (serviceProvider is not null && serviceProvider.TryGetService(out ILoggerFactory loggerFactory))
                {
                    _logger = loggerFactory.CreateLogger<AuditLogInitializationModule>();
                }
                else
                {
                    _logger = new NullLogger();
                }

                _isLoggerInitialized = true;
            }
        }

        #endregion
    }
}
