using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line WebGL build entry point, e.g.
///   Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL
///            -executeMethod WebGLBuildCli.Build -logFile Logs/webgl-build.log
/// Output goes to Builds/WebGL (git-ignored) unless -buildPath &lt;dir&gt; is given.
/// </summary>
public static class WebGLBuildCli
{
    public static void Build()
    {
        var args = System.Environment.GetCommandLineArgs();
        var outDir = "Builds/WebGL";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-buildPath") outDir = args[i + 1];
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log($"[WebGLBuildCli] result={summary.result} totalSize={summary.totalSize / (1024.0 * 1024.0):F1} MB " +
                  $"errors={summary.totalErrors} warnings={summary.totalWarnings} output={summary.outputPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
