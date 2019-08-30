using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Westwind.Utilities.Configuration;

namespace HostsUpdater
{
    public class AppScope
    {
        public static ApplicationConfiguration Configuration { get; set; }

        // static constructor ensures this code runs only once 
        // the first time any static property is accessed
        static AppScope()
        {
            /// Load the properties from the Config store
            Configuration = new ApplicationConfiguration();
            Configuration.Initialize();
        }
    }
}