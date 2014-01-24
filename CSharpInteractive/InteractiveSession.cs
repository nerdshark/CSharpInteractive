using System;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.ProgressMonitoring;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Components.Extensions;

namespace CSharpInteractive
{
	public interface IInteractiveSession
	{
		event Action<string> TextReceived;
		event Action PromptReady;
		event Action Exited;
		void StartReceiving ();
		void SendCommand (string line);
		void Interrupt ();
	}

	public abstract class ProcessInteractiveSession : IInteractiveSession
	{
		public event Action<string> TextReceived = delegate {};
		public event Action PromptReady = delegate {};
		public event Action Exited = delegate {};

		Process process;

		public abstract string GetFileName ();
		public virtual IEnumerable<string> GetArguments ()
		{
			yield break;
		}

		public void StartReceiving ()
		{
			var si = new ProcessStartInfo {
				FileName = GetFileName (),
				Arguments = string.Join (" ", GetArguments ().Select (x => "\"" + x + "\"")),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
			};

			process = new Process ();

			process.StartInfo = si;

			process.EnableRaisingEvents = true;

			process.Exited += (sender, e) => {
				process = null;
				Exited ();
			};

			process.OutputDataReceived += (sender, e) => {
				TextReceived (e.Data);
				PromptReady ();
			};

			process.ErrorDataReceived += (sender, e) => {
				TextReceived (e.Data);
				PromptReady ();
			};

			process.Start ();

			process.BeginOutputReadLine ();
			process.BeginErrorReadLine ();
		}

		public void SendCommand (string line)
		{
			if (process == null)
				return;

			process.StandardInput.WriteLine (line);
		}

		public void Interrupt ()
		{
			if (process == null)
				return;

			process.Kill ();
		}
	}

	public class CSharpInteractiveSession : ProcessInteractiveSession
	{
		public override string GetFileName ()
		{
			LoggingService.LogDebug ("In CSharpInteractiveSession::GetFileName");
			string csharpReplPath = "";
			IEnumerable<string> toolsPaths = null;
			var targetRuntime = IdeApp.Workspace.ActiveRuntime;
			var targetFramework = IdeApp.Services.ProjectService.DefaultTargetFramework;
			if (targetRuntime == null)
				LoggingService.LogDebug ("\ttargetRuntime is null");
			if (targetFramework == null)
				LoggingService.LogDebug ("\ttargetFramework is null");
			if (targetRuntime != null && targetFramework != null) toolsPaths = targetRuntime.GetToolsPaths (targetFramework);
			if (toolsPaths == null)
				return "toolsPaths is null";
			if (!toolsPaths.Any ()) throw new Exception("no toolpaths found");
			foreach (var toolPath in toolsPaths) {
				var possiblePath = Path.Combine (toolPath, "csharp");
				if (File.Exists (possiblePath))
					return possiblePath;

			}
			var paths = "";
			foreach (var toolPath in toolsPaths)
				paths += "\t" + toolPath + Environment.NewLine;
			LoggingService.LogDebug ("\tPaths searched:\n" + paths);
			if (String.IsNullOrEmpty (csharpReplPath)) csharpReplPath = "<not found>";
			LoggingService.LogDebug ("\tcsharpReplPath: " + csharpReplPath);
			return csharpReplPath;

			/*
			if (IdeApp.Workspace.IsOpen)
			{

				//var targetFramework = IdeApp.Services.ProjectService.DefaultTargetFramework;

			}
			else {

			}

			//var currentRuntime = Runtime.SystemAssemblyService.CurrentRuntime;
			//var myRuntime = MonoDevelop.Projects.cu
			//currentRuntime.GetToolPath (MonoDevelop.Core.sys);
			return "/Library/Frameworks/Mono.framework/Versions/Current/bin/csharp";
			*/
		}
	}	
}
