using System.Collections.Generic;
using EPiServer.DataAbstraction.Activities;
using EPiServer.Security;

namespace Swapcode.Optimizely.AuditLog.Activities
{
    /// <summary>
    /// Activity to log content <see cref="SecuritySaveType"/> changes.
    /// </summary>
    public sealed class ContentSecurityActivity : Activity
    {
        /// <summary>
        /// Name of this activity (used for example to register this activity).
        /// </summary>
        public const string ActivityTypeName = "ContentSecurity";

        /// <summary>
        /// Creates a new instance using the action type and dictionary containing the data that will be logged.
        /// </summary>
        /// <param name="action">What kind of action was done, <see cref="SecuritySaveType"/>.</param>
        /// <param name="extendedData">The data in dictionary that will be logged to the activity log.</param>
        public ContentSecurityActivity(SecuritySaveType action, IDictionary<string, string> extendedData) : base(ActivityTypeName, (int)action, extendedData) { }
    }
}
