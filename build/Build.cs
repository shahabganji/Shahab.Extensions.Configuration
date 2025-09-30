using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;


[GitHubActions(
    "build", GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = ["main", "develop"],
    OnPushTags = ["*"],
    ImportSecrets = [nameof(NuGetApiKey)] 
)]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestDirectory => RootDirectory / "test";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    GitHubActions GitHubActions => GitHubActions.Instance;
    string BranchSpec => GitHubActions?.Ref;
    string BuildNumber => GitHubActions?.RunNumber.ToString();

    [Parameter("The key to push to Nuget")][Secret] readonly string NuGetApiKey;

    [Required][GitVersion(Framework = "net8.0", NoFetch = true, NoCache = true)] readonly GitVersion Versioning;
    string SemVer;

    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;
    bool IsTag => BranchSpec != null && BranchSpec.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);

    Target CalculateNugetVersion => _ => _
        .Executes(() =>
        {
            SemVer = Versioning.FullSemVer;
            if (IsPullRequest)
            {
                Log.Information(
                    "Branch spec {BranchSpec} is a pull request. Adding build number {Buildnumber}",
                    BranchSpec, BuildNumber);

                SemVer = string.Join('.', Versioning.SemVer.Split('.').Take(3).Union([
                    BuildNumber
                ]));
            }

            Log.Information("SemVer = {SemVer}", SemVer);
        });


    Target Restore => _ => _
        .Unlisted()
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(_ => _
                .SetProjectFile(Solution)
                .EnableNoCache()
            );
        });

    Target Compile => _ => _
        .Unlisted()
        .DependsOn(Restore)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (summary, semVer) => summary
                    .AddPair("Version", semVer)));

            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetAssemblyVersion(SemVer)
                .SetFileVersion(SemVer)
                .SetInformationalVersion(SemVer)
                .EnableContinuousIntegrationBuild()
            );
        });

    Target Test => _ => _
        .Unlisted()
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(_ => _
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
            );
        });

    Target Format => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetFormat(_ => _
                .EnableVerifyNoChanges()
                .SetVerbosity(DotNetVerbosity.detailed)
            );
        });


    Target Pack => _ => _
        .DependsOn(CalculateNugetVersion)
        .DependsOn(Format)
        .DependsOn(Test)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (c, semVer) => c
                    .AddPair("Packed version", semVer)));

            Project[] projects =
            [
                Solution.src.Shahab_Extensions_Configuration_SqlServerConfiguration,
                Solution.src.Shahab_SqlServerConfiguration_AspNetCore,
            ];

            DotNetTasks.DotNetPack(s => s
                .CombineWith(projects, (settings, project) => settings
                    .SetProject(project)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetConfiguration(Configuration == Configuration.Debug ? "Debug" : "Release")
                    .EnableNoBuild()
                    .EnableNoLogo()
                    .EnableNoRestore()
                    .EnableContinuousIntegrationBuild() // Necessary for deterministic builds
                    .SetVersion(SemVer)));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .OnlyWhenDynamic(() => IsTag)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");

            Assert.NotEmpty(packages);

            DotNetTasks.DotNetNuGetPush(s => s
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));
        });

    Target Verify => _ => _
        .DependsOn(Format)
        .DependsOn(Test);

    Target CD => _ => _.DependsOn(Push);

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft Visual Studio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Push);
}
