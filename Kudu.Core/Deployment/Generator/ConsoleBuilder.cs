using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kudu.Contracts.Settings;
using Kudu.Core.Infrastructure;

namespace Kudu.Core.Deployment.Generator
{
    public class ConsoleBuilder : GeneratorSiteBuilder
    {
        private readonly string _projectPath;
        private readonly string _solutionPath;

        public ConsoleBuilder(IEnvironment environment, IDeploymentSettingsManager settings, IBuildPropertyProvider propertyProvider, string sourcePath, string projectPath, string solutionPath)
            : base(environment, settings, propertyProvider, sourcePath)
        {
            _projectPath = projectPath;
            _solutionPath = solutionPath;
        }

        protected override string ScriptGeneratorCommandArguments
        {
            get
            {
                StringBuilder commandArguments = new StringBuilder();
                commandArguments.AppendFormat("--console \"{0}\"", _projectPath);

                if (!String.IsNullOrEmpty(_solutionPath))
                {
                    commandArguments.AppendFormat(" --solutionFile \"{0}\"", _solutionPath);
                }
                else
                {
                    commandArguments.AppendFormat(" --no-solution", _solutionPath);
                }

                return commandArguments.ToString();
            }
        }

        public override string ProjectType
        {
            get { return "CONSOLE WORKER"; }
        }

        public override async Task Build(DeploymentContext context)
        {
            string destinationPath = context.BuildTempPath;
            string consoleWorkerPath = Path.Combine(Environment.ScriptPath, "ConsoleWorker");

            CopyFile(consoleWorkerPath, destinationPath, "global.asax");
            CopyFile(consoleWorkerPath, destinationPath, "web.config");

            string scriptDirectoryPath = Path.Combine(destinationPath, "bin");
            string scriptFilePath = Path.Combine(scriptDirectoryPath, "run.cmd");
            string scriptContent = "@echo off\necho Running {0}\n{0}\n".FormatInvariant(VsHelper.GetProjectExecutableName(_projectPath));

            OperationManager.Attempt(() =>
            {
                FileSystemHelpers.EnsureDirectory(scriptDirectoryPath);
                File.WriteAllText(scriptFilePath, scriptContent);
            });

            await base.Build(context);
        }

        private void CopyFile(string sourcePath, string destinationPath, string fileName)
        {
            sourcePath = Path.Combine(sourcePath, fileName + ".template");
            destinationPath = Path.Combine(destinationPath, fileName);
            OperationManager.Attempt(() => File.Copy(sourcePath, destinationPath, overwrite: true));
        }
    }
}
