﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kudu.Contracts.Settings;
using Kudu.Core;
using Kudu.Core.Deployment;
using Kudu.FunctionalTests.Infrastructure;
using Kudu.TestHarness;
using Xunit;
using Xunit.Sdk;

namespace Kudu.FunctionalTests
{
    public class GitDeploymentTests
    {
        // ASP.NET apps

        [Fact]
        public void PushAndDeployAspNetAppOrchard()
        {
            PushAndDeployApps("Orchard", "master", "Welcome to Orchard", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployAspNetAppProjectWithNoSolution()
        {
            PushAndDeployApps("ProjectWithNoSolution", "master", "Project without solution", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployAspNetAppHiddenFoldersAndFiles()
        {
            PushAndDeployApps("HiddenFoldersAndFiles", "master", "Hello World", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployWebApiApp()
        {
            PushAndDeployApps("Dev11_Net45_Mvc4_WebAPI", "master", "HelloWorld", HttpStatusCode.OK, "", resourcePath: "api/values", httpMethod: "POST", jsonPayload: "\"HelloWorld\"");
        }

        [Fact]
        public void PushAndDeployAspNetAppWebSiteInSolution()
        {
            PushAndDeployApps("WebSiteInSolution", "master", "SomeDummyLibrary.Class1", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployAspNetAppWebSiteInSolutionWithDeploymentFile()
        {
            PushAndDeployApps("WebSiteInSolution", "UseDeploymentFile", "SomeDummyLibrary.Class1", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployAspNetAppKuduGlob()
        {
            PushAndDeployApps("kuduglob", "master", "ASP.NET MVC", HttpStatusCode.OK, "酷度");
        }

        [Fact]
        public void PushAndDeployAspNetAppAppWithPostBuildEvent()
        {
            PushAndDeployApps("AppWithPostBuildEvent", "master", "Hello Kudu", HttpStatusCode.OK, "Deployment successful");
        }

        // Node apps

        [Fact]
        public void PushAndDeployNodeAppExpress()
        {
            // Ensure node is installed.
            Assert.Contains("nodejs", System.Environment.GetEnvironmentVariable("Path"), StringComparison.OrdinalIgnoreCase);

            PushAndDeployApps("Express-Template", "master", "Modify this template to jump-start your Node.JS Express Web Pages application", HttpStatusCode.OK, "");
        }

        [Fact]
        public void PushAndDeployHtml5WithAppJs()
        {
            PushAndDeployApps("Html5Test", "master", "html5", HttpStatusCode.OK, String.Empty);
        }

        //Entity Framework 4.5 MVC Project with SQL Compact DB (.sdf file)
        //and Metadata Artifact Processing set to 'Embed in Assembly'
        [Fact]
        public void PushAndDeployEFMVC45AppSqlCompactMAPEIA()
        {
            PushAndDeployApps("MvcApplicationEFSqlCompact", "master", "Reggae", HttpStatusCode.OK, "");
        }

        // Other apps

        [Fact]
        public void CustomDeploymentScriptShouldHaveDeploymentSetting()
        {
            // use a fresh guid so its impossible to accidently see the right output just by chance.
            var guidtext = Guid.NewGuid().ToString();
            var unicodeText = "酷度酷度";
            var normalVar = "TESTED_VAR";
            var normalVarText = "Settings Were Set Properly" + guidtext;
            var kuduSetVar = "KUDU_SYNC_CMD";
            var kuduSetVarText = "Fake Kudu Sync " + guidtext;
            var expectedLogFeedback = "Using custom deployment setting for {0} custom value is '{1}'.".FormatCurrentCulture(kuduSetVar, kuduSetVarText);

            string randomTestName = "CustomDeploymentScriptShouldHaveDeploymentSetting";
            ApplicationManager.Run(randomTestName, appManager =>
            {
                appManager.SettingsManager.SetValue(normalVar, normalVarText).Wait();
                appManager.SettingsManager.SetValue(kuduSetVar, kuduSetVarText).Wait();

                // Act
                using (TestRepository testRepository = Git.Clone("CustomDeploymentSettingsTest"))
                {
                    appManager.GitDeploy(testRepository.PhysicalPath, "master");
                }
                var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                // Assert
                Assert.Equal(1, results.Count);
                Assert.Equal(DeployStatus.Success, results[0].Status);

                // Also validate custom script output supports unicode
                string[] expectedStrings = {
                    unicodeText,
                    normalVar + "=" + normalVarText,
                    kuduSetVar + "=" + kuduSetVarText,
                    expectedLogFeedback };
                KuduAssert.VerifyLogOutput(appManager, results[0].Id, expectedStrings);
            });
        }

        [Fact]
        public async Task CustomGeneratorArgs()
        {
            await ApplicationManager.RunAsync("UpdatedTargetPathShouldChangeDeploymentDestination", async appManager =>
            {
                // Even though it's a WAP, use custom script generator arguments to treat it as a web site,
                // deploying only its content folder
                await appManager.SettingsManager.SetValue(SettingsKeys.ScriptGeneratorArgs, "--basic -p MvcApplication14/content");

                using (TestRepository testRepository = Git.Clone("Mvc3AppWithTestProject"))
                {
                    appManager.GitDeploy(testRepository.PhysicalPath, "master");
                }
                var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                Assert.Equal(1, results.Count);
                Assert.Equal(DeployStatus.Success, results[0].Status);
                KuduAssert.VerifyUrl(appManager.SiteUrl + "themes/base/jquery.ui.accordion.css", ".ui-accordion-header");
            });
        }

        [Fact]
        public void UpdatedTargetPathShouldChangeDeploymentDestination()
        {
            ApplicationManager.Run("UpdatedTargetPathShouldChangeDeploymentDestination", appManager =>
            {
                using (TestRepository testRepository = Git.Clone("TargetPathTest"))
                {
                    appManager.GitDeploy(testRepository.PhysicalPath, "master");
                }
                var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                Assert.Equal(1, results.Count);
                Assert.Equal(DeployStatus.Success, results[0].Status);
                KuduAssert.VerifyUrl(appManager.SiteUrl + "myTarget/index.html", "Target Path Test");
            });
        }

        [Fact]
        public void PushAndDeployMVCAppWithLatestNuget()
        {
            PushAndDeployApps("MVCAppWithLatestNuget", "master", "MVCAppWithLatestNuget", HttpStatusCode.OK, "Deployment successful");
        }

        [Fact]
        public void PushAndDeployConsoleWorker()
        {
            const string verificationFilePath = "LogFiles/verification.txt";
            const string runScriptPath = "Site/wwwroot/bin/run.cmd";
            const string runningVerification = "Running...";
            const string stoppedVerification = "Missing run.cmd file.";
            const string expectedVerificationFileContent = "Verified!!!";

            ApplicationManager.Run("ConsoleWorker", appManager =>
            {
                ///////// Part 1
                TestTracer.Trace("Starting ConsoleWorker test, deploying the worker");

                using (TestRepository testRepository = Git.Clone("ConsoleWorker"))
                {
                    appManager.GitDeploy(testRepository.PhysicalPath);
                }
                var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                Assert.Equal(1, results.Count);
                Assert.Equal(DeployStatus.Success, results[0].Status);

                KuduAssert.VerifyUrl(appManager.SiteUrl, runningVerification);

                TestTracer.Trace("Waiting for the verification file...");

                bool verificationFileExists = false;
                for (int checkCount = 0; !verificationFileExists && checkCount < 10; checkCount++)
                {
                    Thread.Sleep(500);
                    verificationFileExists = appManager.VfsManager.Exists(verificationFilePath);
                }

                string verificationFileContent = appManager.VfsManager.ReadAllText(verificationFilePath);
                Assert.Equal(expectedVerificationFileContent, verificationFileContent.TrimEnd());

                ///////// Part 2
                TestTracer.Trace("Waiting for worker to start the process again...");

                string[] lines = new string[0];
                for (int checkCount = 0; lines.Length < 2 && checkCount < 60; checkCount++)
                {
                    Thread.Sleep(1000);
                    verificationFileContent = appManager.VfsManager.ReadAllText(verificationFilePath);
                    lines = verificationFileContent.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }

                Assert.Equal(2, lines.Length);
                Assert.Equal(expectedVerificationFileContent, lines[1]);

                ///////// Part 3
                TestTracer.Trace("Verifying worker stopped when run.cmd is missing");

                appManager.VfsManager.Delete(runScriptPath);

                bool workerStopped = false;
                for (int checkCount = 0; !workerStopped && checkCount < 60; checkCount++)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        KuduAssert.VerifyUrl(appManager.SiteUrl, stoppedVerification);
                        workerStopped = true;
                    }
                    catch (AssertException)
                    {
                    }
                }
                KuduAssert.VerifyUrl(appManager.SiteUrl, stoppedVerification);

                ///////// Part 4
                TestTracer.Trace("Verifying worker starts again when run.cmd is back");

                appManager.VfsManager.WriteAllText(runScriptPath, "ConsoleWorker.exe");

                bool workerStarted = false;
                for (int checkCount = 0; !workerStarted && checkCount < 60; checkCount++)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        KuduAssert.VerifyUrl(appManager.SiteUrl, runningVerification);
                        workerStarted = true;
                    }
                    catch (AssertException)
                    {
                    }
                }
                KuduAssert.VerifyUrl(appManager.SiteUrl, runningVerification);
            });
        }

        [Fact]
        public void PushAndDeployMVCAppWithNuGetAutoRestore()
        {
            PushAndDeployApps("MvcApplicationWithNuGetAutoRestore", "master", "MvcApplicationWithNuGetAutoRestore", HttpStatusCode.OK, "Deployment successful");
        }

        //Common code
        internal static void PushAndDeployApps(string repoCloneUrl, string defaultBranchName,
                                              string verificationText, HttpStatusCode expectedResponseCode, string verificationLogText,
                                              string resourcePath = "", string httpMethod = "GET", string jsonPayload = "", bool deleteSCM = false)
        {
            using (new LatencyLogger("PushAndDeployApps - " + repoCloneUrl))
            {
                Uri uri;
                if (!Uri.TryCreate(repoCloneUrl, UriKind.Absolute, out uri))
                {
                    uri = null;
                }

                string randomTestName = uri != null ? Path.GetFileNameWithoutExtension(repoCloneUrl) : repoCloneUrl;
                ApplicationManager.Run(randomTestName, appManager =>
                {
                    // Act
                    using (TestRepository testRepository = Git.Clone(randomTestName, uri != null ? repoCloneUrl : null))
                    {
                        using (new LatencyLogger("GitDeploy"))
                        {
                            appManager.GitDeploy(testRepository.PhysicalPath, defaultBranchName);
                        }
                    }
                    var results = appManager.DeploymentManager.GetResultsAsync().Result.ToList();

                    // Assert
                    Assert.Equal(1, results.Count);
                    Assert.Equal(DeployStatus.Success, results[0].Status);
                    var url = new Uri(new Uri(appManager.SiteUrl), resourcePath);
                    if (!String.IsNullOrEmpty(verificationText))
                    {
                        KuduAssert.VerifyUrl(url.ToString(), verificationText, expectedResponseCode, httpMethod, jsonPayload);
                    }
                    if (!String.IsNullOrEmpty(verificationLogText))
                    {
                        KuduAssert.VerifyLogOutput(appManager, results[0].Id, verificationLogText.Trim());
                    }
                    if (deleteSCM)
                    {
                        using (new LatencyLogger("SCMAndWebDelete"))
                        {
                            appManager.RepositoryManager.Delete(deleteWebRoot: false, ignoreErrors: false).Wait();
                        }
                    }
                });
            }
        }
    }
}
