using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using behealthapp.shared;

namespace behealthapp
{
    public static class behealth
    {
        [FunctionName("BEHealth")]
        public static void Run([TimerTrigger("*/45 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            string clientID = Utils.GetEnvVariableOrDefault("clientID");
            string clientSecret = Utils.GetEnvVariableOrDefault("clientSecret");
            string tenantID = Utils.GetEnvVariableOrDefault("tenantID", "a8175357-a762-478b-b724-6c2bd3f3f45e");
            string location = Utils.GetEnvVariableOrDefault("location");
            string subscriptionID = Utils.GetEnvVariableOrDefault("subscriptionID");
            string resourcegroupname = Utils.GetEnvVariableOrDefault("_resourceGroupName","newexp-demo");
            string appGwName = Utils.GetEnvVariableOrDefault("_appGwName", "newexp-appgw");
            string scaleSetName = Utils.GetEnvVariableOrDefault("_scaleSetName", "newexpvmssdemo");
            int minHealthyServers = Utils.GetEnvVariableOrDefault("_minHealthyServers", 3);
    

            try
            {
                log.LogInformation("Creating Azure Client for BE Health Function");
                var azEnvironment = AzureEnvironment.AzureGlobalCloud;
                var azClient = azureClient.CreateAzureClient(clientID, clientSecret, tenantID, azEnvironment, subscriptionID);
                var scaleSet = azClient.VirtualMachineScaleSets.GetByResourceGroup(resourcegroupname, scaleSetName);
                var appGw = azClient.ApplicationGateways.GetByResourceGroup(resourcegroupname, appGwName);
                var appGwBEHealth = azClient.ApplicationGateways.Inner.BackendHealthAsync(resourcegroupname, appGwName).Result;
                log.LogInformation("Checking Application Gateway BE Healthy ");
                applicationGatewayOperations.CheckApplicationGatewayBEHealthAndDeleteBadNodes(appGwBEHealth, scaleSet, minHealthyServers, log);

               
            }
            catch (Exception e)
            {
                log.LogInformation("Error Message: " + e.Message);
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
