# Swapcode.Optimizely.AuditLog
Optimizely CMS add-on to log changes to content access rights to activity log (change log).

# Install
- Get the NuGet package from this repository releases
- copy it to local disk and create local NuGet package source in your project
- install the package from your local source (remeber to check the `Include prerelease`)

The package uses currently embedded XML files for localization, and you must add those to your solution.
- In your startup class, find the method `public void ConfigureServices(IServiceCollection services)`
  - find the line where you have `services.AddCms();` and after this line add the following
    - `services.AddEmbeddedLocalization<AuditLogInitializationModule>();`
