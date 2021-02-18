using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace HostsUpdater
{    public class Options
    {
        [Option("blockSocial", Required = false, HelpText = "Block social media.")]
        public bool BlockSocialMedia { get; set; }
    }
}