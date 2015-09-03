# Set up solution paths.
$ToolsDir = split-path -parent $MyInvocation.MyCommand.Path
$SolutionDir = split-path -parent $ToolsDir

# Restore NuGet packages prior to build.
# This ensures ItemDefinitionGroups for native projects are read at the appropriate time.
$NuGetCommand = join-path $SolutionDir '.nuget\nuget.exe'
&$NuGetCommand restore
