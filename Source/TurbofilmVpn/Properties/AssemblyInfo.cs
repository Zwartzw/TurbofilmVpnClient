using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;

// product info
[assembly: AssemblyTitle("Turbofilm Vpn")]
[assembly: AssemblyProduct("Turbofilm Vpn")]
[assembly: AssemblyDescription("Vemeo vpn only for turbofilm.tv.")]
[assembly: AssemblyCompany("medved.co")]
[assembly: AssemblyCopyright("© Sergey Alekseev 2013")]
// build info
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
// version info
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyInformationalVersion("1.0~EarlyAlpha")]
// interoperation info
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: ComVisible(false)]
[assembly: Guid("3c26d05a-bdd6-4fbb-b0e8-44ebae554d94")]
