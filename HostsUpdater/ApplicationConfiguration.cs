using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.Utilities.Configuration;

namespace HostsUpdater
{
    public class ApplicationConfiguration : AppConfiguration
    {
        #region Public Properties

        public string ApplicationTitle { get; set; }
        public string StevenBlacksHostsFileUrl { get; set; }
        public string AmpHostsFileUrl { get; set; }
        public int MaxAgeOfTempFilesInDays { get; set; }

        #endregion

        #region Constructor

        public ApplicationConfiguration()
        {
        }

        #endregion        
    }
}