using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HoldTheMine.EditorTools
{
    /// <summary>
    /// WebGL build for GitHub Pages: no compression (Pages can't send the Content-Encoding headers
    /// Brotli/Gzip builds need), decompression fallback on as a safety net.
    /// Menu: Hold the Mine → Build WebGL, or from the command line:
    ///   unity build &lt;project&gt; --target WebGL --execute-method HoldTheMine.EditorTools.WebGLBuild.Build --output &lt;folder&gt;
    /// </summary>
    public static class WebGLBuild
    {
        const string DefaultOutput = "Builds/WebGL";

        [MenuItem("Hold the Mine/Build WebGL")]
        public static void BuildFromMenu() => Run(DefaultOutput);

        /// <summary>Entry point for batch mode; honours -buildOutput &lt;path&gt;.</summary>
        public static void Build()
        {
            string output = DefaultOutput;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-buildOutput");
            if (i >= 0 && i + 1 < args.Length) output = args[i + 1];
            var report = Run(output);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void ApplyWebSettings()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.productName = "Hold the Mine";
            PlayerSettings.companyName = "leogrzn";
            AssetDatabase.SaveAssets();
        }

        static BuildReport Run(string output)
        {
            ApplyWebSettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/HoldTheMine.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[HoldTheMine] WebGL build {report.summary.result}: {output} ({report.summary.totalSize / (1024f * 1024f):0.0} MB, {report.summary.totalErrors} errors)");
            return report;
        }
    }
}
