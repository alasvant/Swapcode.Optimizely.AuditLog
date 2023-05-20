# Swapcode.Optimizely.AuditLog
Optimizely CMS add-on to log changes to content access rights to activity log (change log).

# Install
- Preferred way is to install from Optimizely NuGet feed
  - https://nuget.optimizely.com/feed/packages.svc/
  - package id: `Swapcode.Optimizely.AuditLog`
- From this repository releases section
  - copy it to local disk and create local NuGet package source in your project
  - install the package from your local source

The package uses currently embedded XML files for localization, and you must add those to your solution.
- In your startup class, find the method `public void ConfigureServices(IServiceCollection services)`
  - find the line where you have `services.AddCms();` and after this line add the following
    - `services.AddAuditLog();`
