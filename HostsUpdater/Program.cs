using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ServiceProcess;

using NLog;

using HostsUpdater.Utilities;

namespace HostsUpdater
{
    class Program
    {
        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Private Properties

        private static string ApplicationTitle
        {
            get { return AppScope.Configuration.ApplicationTitle; }
        }

        private static string BlocksiteServiceName
        {
            get { return "BlocksiteService"; }
        }

        private static string HostsFolderPath 
        {
            get
            {
                var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return Path.Combine(systemPath, @"drivers\etc");
            }            
        }

        private static string HostsDownloadFilePath
        {
            get
            {
                var datestamp = DateTime.Now.ToString("ddMMyyyy");
                return Path.Combine(HostsFolderPath, string.Format("HostsDownload-{0}.txt", datestamp));
            }          
        }


        private static string AmpsDownloadFilePath
        {
            get
            {
                var datestamp = DateTime.Now.ToString("ddMMyyyy");
                return Path.Combine(HostsFolderPath, string.Format("AmpsDownload-{0}.txt", datestamp));
            }
        }

        #endregion

        static void Main(string[] args)
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            try
            {
                DownloadHostsData();
                StopService(BlocksiteServiceName); // Stop the DNS cache?
                RebuildHostsFile();
                FlushDns();
                StartService(BlocksiteServiceName);
                CleanupTemporaryFiles();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Service failed when preventing changes to HOSTS file.");
            }

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

        /// <summary>
        /// Method to trap unhandled exceptions.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error((Exception)e.ExceptionObject,
                        string.Format("An unhandled exception has occurred in the {0}.", ApplicationTitle));
            Environment.Exit(1);
        }

        #region Private Methods
        
        private static void CleanupTemporaryFiles()
        {
            var currentFolder = new DirectoryInfo(HostsFolderPath);
            var files = new List<FileInfo>();
            files.AddRange(currentFolder.GetFiles());
            var maxAgeOfTempFilesInDays = AppScope.Configuration.MaxAgeOfTempFilesInDays;

            foreach (var file in files)
            {
                if ((file.Name.StartsWith("UpdatedHosts-") && file.Extension == ".txt") ||
                    (file.Name.StartsWith("hosts-") && file.Extension == ".bak" &&
                        file.CreationTime < DateTime.Now.AddDays(-maxAgeOfTempFilesInDays)) ||
                    (file.Name.StartsWith("HostsDownload-") && file.Extension == ".txt" &&
                        file.CreationTime < DateTime.Now.AddHours(-maxAgeOfTempFilesInDays)) ||
                    (file.Name.StartsWith("AmpsDownload-") && file.Extension == ".txt" &&
                        file.CreationTime < DateTime.Now.AddHours(-maxAgeOfTempFilesInDays)))
                {
                    logger.Info(string.Format("Deleting file {0}", file.FullName));
                    file.IsReadOnly = false;
                    file.Delete();
                }
            }
        }

        private static void DownloadHostsData()
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());
            
            var hostsDownloadFile = new FileInfo(HostsDownloadFilePath);                       
            WebClient webClient = new WebClient();
            webClient.DownloadFile(AppScope.Configuration.StevenBlacksHostsFileUrl, hostsDownloadFile.FullName);
            
            var ampsDownloadFile = new FileInfo(AmpsDownloadFilePath);
            webClient.DownloadFile(AppScope.Configuration.AmpHostsFileUrl, ampsDownloadFile.FullName);

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }
        
        private static void StopService(string serviceName)
        {
            var service  = new ServiceController();
            service.ServiceName = serviceName;

            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                logger.Info(string.Format("Successfully stoppped service '{0}'.", serviceName));
            }
        }

        private static void StartService(string serviceName)
        {
            var service = new ServiceController();
            service.ServiceName = serviceName;

            if ((service.Status != ServiceControllerStatus.Running) &&
                (service.Status != ServiceControllerStatus.StartPending))
            {
                service.Start();
                logger.Info(string.Format("Successfully started service '{0}'.", serviceName));
            }
        }

        private static void RebuildHostsFile()
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            var hostsTemplateFile = new FileInfo(Path.Combine(HostsFolderPath, "HostsTemplate.txt"));

            if (!hostsTemplateFile.Exists)
            {
                logger.Warn("Unable to find the hosts template file at '{0}'.", hostsTemplateFile);
                return;
            }

            var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            var updatedHostsFilePath = Path.Combine(HostsFolderPath, string.Format("UpdatedHosts-{0}.txt", timestamp));
            hostsTemplateFile.CopyTo(updatedHostsFilePath);

            AppendFileContents(HostsDownloadFilePath, updatedHostsFilePath);
            AppendFileContents(AmpsDownloadFilePath, updatedHostsFilePath);
            
            WhitelistDomains(updatedHostsFilePath);
            
            var backupFilePath = Path.Combine(HostsFolderPath, "hosts.bak");
            OverwriteFile(updatedHostsFilePath, backupFilePath);

            var hostsFilePath = Path.Combine(HostsFolderPath, "hosts");
            OverwriteFile(updatedHostsFilePath, hostsFilePath);

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

        private static void OverwriteFile(string sourceFilePath, string targetFilePath)
        {
            var targetFile = new FileInfo(targetFilePath);
            targetFile.IsReadOnly = false;
            File.WriteAllText(targetFilePath, File.ReadAllText(sourceFilePath));
            logger.Info(string.Format("Successfully overwrite file '{0}'.", targetFilePath));
            targetFile.IsReadOnly = true;
        }

        private static void AppendFileContents(string sourceFilePath, string targetFilePath)
        {
            var sourceFile = new FileInfo(sourceFilePath);
            if (sourceFile.Exists)
            {
                using (Stream sourceFileStream = File.OpenRead(sourceFile.FullName))
                {
                    using (Stream targetFileStream = new FileStream(targetFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        sourceFileStream.CopyTo(targetFileStream);
                    }
                }

                var message = string.Format("Successfully added contents of file '{0}' to '{1}'.", sourceFilePath, targetFilePath);
                logger.Info(message);
            }
        }

        private static void WhitelistDomains(string updatedHostsFilePath)
        {
            var whitelistFile = new FileInfo(Path.Combine(HostsFolderPath, "Whitelist.txt"));

            if (whitelistFile.Exists)
            {
                var domains = File.ReadLines(whitelistFile.FullName);
                domains = domains.Where(d => !d.StartsWith("#"));

                foreach (var domain in domains)
                {
                    var lines = File.ReadLines(updatedHostsFilePath);
                    var updatedContent = new List<string>();

                    foreach (var line in lines)
                    {
                        if (line.Contains(domain) && (!line.StartsWith("#")))
                        {
                            updatedContent.Add("# " + line);
                        }
                        else
                        {
                            updatedContent.Add(line);
                        }
                    }

                    File.WriteAllLines(updatedHostsFilePath, updatedContent);
                }
            }
        }
        
        private static void FlushDns()
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "ipconfig.exe";
            process.StartInfo.Arguments = "/flushdns";
            process.Start();
            process.WaitForExit();
            //string output = process.StandardOutput.ReadToEnd();
            //return output;

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }


        #endregion Private Methods
    }
}