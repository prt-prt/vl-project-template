using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    // =======================================================
    // PARAMS
    // =======================================================

    [Parameter("Path to vvvvc.exe compiler")]
    readonly string CompilerPath;

    [Parameter("Project name (defaults to folder name)")]
    readonly string ProjectName;

    // =======================================================
    // PATHS
    // =======================================================

    string Version = "";
    string ResolvedProjectName => ProjectName ?? Path.GetFileName(RootDirectory / ..);

    const string RuntimeId = "win-x64";

    AbsolutePath ArtifactsDirectory => RootDirectory / .. / "artifacts";
    AbsolutePath VersionFile => RootDirectory / .. / "Version.props";

    // These will be resolved based on project name
    AbsolutePath VvvvPropsFile => RootDirectory / .. / $"{ResolvedProjectName}.props";
    AbsolutePath VvvvSourceFile => RootDirectory / .. / $"{ResolvedProjectName}.vl";

    // =======================================================
    // TARGETS
    // =======================================================

    Target Clean => _ => _
        .Executes(() =>
        {
            Console.WriteLine("Cleaning artifacts folder...");
            if (Directory.Exists(ArtifactsDirectory))
            {
                foreach (var file in Directory.GetFiles(ArtifactsDirectory, "*", SearchOption.AllDirectories))
                    File.Delete(file);
                foreach (var dir in Directory.GetDirectories(ArtifactsDirectory))
                    Directory.Delete(dir, true);
            }
        });

    Target GetVersion => _ => _
       .Executes(() =>
       {
           try
           {
               Version = XDocument.Load(VersionFile).Descendants("Version").FirstOrDefault()?.Value ?? "0.0.0";
               Console.WriteLine($"Building version {Version}");
           }
           catch
           {
               Console.WriteLine($"Could not extract version from {VersionFile}, using 0.0.0");
               Version = "0.0.0";
           }
       });

    Target Compile => _ => _
        .DependsOn(Clean)
        .DependsOn(GetVersion)
        .Requires(() => !string.IsNullOrWhiteSpace(CompilerPath))
        .Executes(() =>
        {
            Console.WriteLine($"Project: {ResolvedProjectName}");
            Console.WriteLine($"Source: {VvvvSourceFile}");
            Console.WriteLine($"Props: {VvvvPropsFile}");

            if (!File.Exists(VvvvSourceFile))
                throw new FileNotFoundException($"Could not find {VvvvSourceFile}");

            if (!File.Exists(VvvvPropsFile))
                throw new FileNotFoundException($"Could not find {VvvvPropsFile}");

            // Ensure RuntimeIdentifier is set correctly in props file
            var propsXdoc = XDocument.Load(VvvvPropsFile);
            var ridElement = propsXdoc.Descendants(XName.Get("RuntimeIdentifier", "http://schemas.microsoft.com/developer/msbuild/2003")).FirstOrDefault();
            if (ridElement != null)
            {
                ridElement.Value = RuntimeId;
                propsXdoc.Save(VvvvPropsFile);
            }

            // Compile
            Console.WriteLine($"Compiling for {RuntimeId}...");
            var buildProcess = ProcessTasks.StartProcess(CompilerPath, $"{VvvvSourceFile} --output-type WinExe --rid {RuntimeId} --clean");
            buildProcess.WaitForExit();

            if (buildProcess.ExitCode != 0)
                throw new Exception($"Compilation failed with exit code {buildProcess.ExitCode}");

            // Delete src folder from output (contains VL source, not needed for distribution)
            var srcFolder = ArtifactsDirectory / RuntimeId / ResolvedProjectName / "src";
            if (Directory.Exists(srcFolder))
            {
                Console.WriteLine("Removing src folder from output...");
                Directory.Delete(srcFolder, true);
            }

            // Create portable zip
            var buildOutput = ArtifactsDirectory / RuntimeId / ResolvedProjectName;
            var zipName = $"{ResolvedProjectName.ToLowerInvariant()}_{Version}_{RuntimeId}.zip";
            var zipPath = ArtifactsDirectory / zipName;

            Console.WriteLine($"Creating {zipName}...");
            buildOutput.ZipTo(zipPath);

            Console.WriteLine($"Build complete! Output: {zipPath}");
        });
}
