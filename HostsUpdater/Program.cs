using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

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

        private static string HostsFolderPath 
        {
            get
            {
                var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return Path.Combine(systemPath, @"drivers\etc");
            }            
        }

        #endregion

        static void Main(string[] args)
        {
            // Download the latest Hosts file


            // Stop the Blocksite Service
            // Stop the DNS cache

            //var hostsFile = new FileInfo(HostsFolderPath);
            //hostsFile.IsReadOnly = false;

            // Restart the Blocksite service
            // Restart the DNS cache


            logger.Trace(LogHelper.BuildMethodEntryTrace());

            try
            {
                // Download the latest Hosts files
                DownloadHostsData();
                // Stop the Blocksite Service
                // Make the hosts and hosts.bak files editable
                // Copy the updated URLs to the hosts and hosts.bak files
                // Make the hosts and hosts.bak files read only
                // Restart the Blocksite service
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
        
        private static void DownloadHostsData()
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());
            
            var datestamp = DateTime.Now.ToString("ddMMyyyy");
            var hostsDownloadFile = new FileInfo(Path.Combine(HostsFolderPath, string.Format("HostsDownload-{0}.txt", datestamp)));                       
            WebClient webClient = new WebClient();
            webClient.DownloadFile(AppScope.Configuration.StevenBlacksHostsFileUrl, hostsDownloadFile.FullName);
            
            var ampsDownloadFile = new FileInfo(Path.Combine(HostsFolderPath, string.Format("AmpsDownload-{0}.txt", datestamp)));
            webClient.DownloadFile(AppScope.Configuration.AmpHostsFileUrl, ampsDownloadFile.FullName);

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

 
        #endregion Private Methods
    }
}