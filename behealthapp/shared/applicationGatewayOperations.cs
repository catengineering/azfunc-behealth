using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Compute.Fluent;
namespace behealthapp.shared
{
    class applicationGatewayOperations
    {

        /// <summary>
        /// Checks the application gateway back end health metrics and deletes App Payload Failed nodes
        /// </summary>
        /// <returns><c>true</c>, if we deleted nodes, <c>false</c> otherwise.</returns>
        /// <param name="appGw">App gateway</param>
        /// <param name="scaleSet">Scale set.</param>
        /// <param name="minHealthyServers">Minimum healthy servers.</param>
        /// <param name="log">Log.</param>
        public static bool CheckApplicationGatewayBEHealthAndDeleteBadNodes(ApplicationGatewayBackendHealthInner appGw, IVirtualMachineScaleSet scaleSet, int minHealthyServers, ILogger log)
        {
            try
            {
                log.LogInformation("Enumerating Application Gateway Backend Servers");
                var healthy = new List<ApplicationGatewayBackendHealthServer>();
                var unhealthy = new List<ApplicationGatewayBackendHealthServer>();
                foreach (var server in appGw.BackendAddressPools[0].BackendHttpSettingsCollection[0].Servers)
                {
                    if (server.Health.Value == "Healthy")
                    {
                        healthy.Add(server);
                    }
                    else
                    {
                        unhealthy.Add(server);
                    }
                }

                List<string> appGwBadIps = new List<string>();

                // If we have unhealthy nodes, then delete them
                if (unhealthy.Count > 0)
                {
                    log.LogInformation("App Payload Failed node count = {0}, removing nodes", unhealthy.Count);

                    if (healthy.Count <= 3)
                    {
                        var nodeCount = healthy.Count() + unhealthy.Count() + 3;
                        log.LogInformation("Healthy Node Count <=3, mandatory scale event to Current Healthly Count + UnHealthy Count +  3 nodes. ScaleTarget Set to {0}", nodeCount);
                        vmScaleSetOperations.ScaleToTargetSize(scaleSet, nodeCount, 10, 100, false, false, log);
                    }

                    return vmScaleSetOperations.RemoveVMSSInstancesByIP(scaleSet, unhealthy.Select(s => s.Address).ToList(), log);
                }


                return false;
            }
            catch (Exception e)
            {
                log.LogInformation("Error Message: " + e.Message);
                log.LogInformation("HResult: " + e.HResult);
                log.LogInformation("InnerException:" + e.InnerException);
                return false;
            }
        }
    }
}
