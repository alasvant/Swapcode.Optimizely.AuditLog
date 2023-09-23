using EPiServer.DataAbstraction;

namespace Swapcode.Optimizely.AuditLog
{
    /// <summary>
    /// Service interface to log CMS events to CMS Audit Log.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Writes the <see cref="ContentSecurityEventArg"/> to CMS audit log.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments.</param>
        void AccessRightsChanged(object sender, ContentSecurityEventArg e);
    }
}
