using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace HostsUpdater.Utilities
{
    /// <summary>
    /// Static class to hold helper methods to help with logging
    /// </summary>
    public static class LogHelper
    {
        public static string BuildMethodEntryTrace(Dictionary<string, string> parameters = null, [CallerMemberName] string methodName = "")
        {
            var traceMessage = string.Concat("Entering method ", methodName, "()");

            if (parameters == null || parameters.Count == 0)
            {
                // Empty statement to ignore empty parameters collection
            }
            else if (parameters.Count == 1)
            {
                traceMessage += " with parameter ";
            }
            else
            {
                traceMessage += " with parameters ";
            }

            if (parameters != null)
            {
                List<string> parameterValues = new List<string>();

                foreach (KeyValuePair<string, string> parameter in parameters)
                {
                    parameterValues.Add(string.Format("{0}: '{1}'", parameter.Key, parameter.Value));
                }

                traceMessage += string.Join(", ", parameterValues);
            }

            traceMessage += ".";
            return traceMessage;
        }

        public static string BuildMethodExitTrace(string returnValue = null, [CallerMemberName] string methodName = "")
        {
            var traceMessage = string.Concat("Exiting method ", methodName, "()");

            if (!string.IsNullOrEmpty(returnValue))
            {
                traceMessage += string.Format(" with return value: '{0}'", returnValue);
            }

            traceMessage += ".";
            return traceMessage;
        }
    }
}