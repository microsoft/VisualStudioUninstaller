using Microsoft.VS.ConfigurationManager;
using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Collections.Generic;

namespace Microsoft.VS.Uninstaller
{
    internal static class VisualStudioSpecific
    {
        internal static void VSFilters(Primitives ip)
        {
            ip.Filters.Add(Filter.CreateFilter("Replace Visual Studio with shorter version", "Microsoft Visual Studio ", "VS "));
            ip.Filters.Add(Filter.CreateFilter("Shorten Microsoft", "Microsoft ", "MS "));
            ip.Filters.Add(Filter.CreateFilter("Shorten Team Foundation Server", "Team Foundation Server ", "TFS "));
            ip.Filters.Add(Filter.CreateFilter("Shorten Visual C++", "Visual C++ ", "VC "));
        }

        internal static void VSUninstallActions(Primitives ip)
        {

            ip.UninstallActions.Add(
                UninstallAction.CreateUninstallAction(
                    new List<ArchitectureConfiguration> { ArchitectureConfiguration.x86, ArchitectureConfiguration.x64 },
                    new List<OperatingSystemConfiguration> { OperatingSystemConfiguration.Windows81 },
                    "2999226",
                    UninstallAction.TemplateType.Pre,
                    UninstallAction.WixObjectType.MSU
                    )
                );
            ip.UninstallActions.Add(
                UninstallAction.CreateUninstallAction(
                new List<ArchitectureConfiguration> { ArchitectureConfiguration.x86, ArchitectureConfiguration.x64 },
                new List<OperatingSystemConfiguration> { OperatingSystemConfiguration.Windows8 },
                "2999226",
                UninstallAction.TemplateType.Pre,
                UninstallAction.WixObjectType.MSU
                )
            );

            ip.UninstallActions.Add(
                UninstallAction.CreateUninstallAction(
                new List<ArchitectureConfiguration> { ArchitectureConfiguration.x86, ArchitectureConfiguration.x64 },
                new List<OperatingSystemConfiguration> { OperatingSystemConfiguration.Windows7 },
                "2999226",
                UninstallAction.TemplateType.Pre,
                UninstallAction.WixObjectType.MSU
                )
            );

        }
    }
}
