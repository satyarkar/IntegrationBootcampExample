namespace PowerLearn.Core.Plugins.ServiceBus
{
    using Microsoft.Xrm.Sdk;
    using PowerLearn.Core.Plugins.Common;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;

    public class AzureAwarePluginExample2 : IPlugin
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AzureAwarePluginExample2()
        {

        }

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = serviceFactory.CreateOrganizationService(Guid.Empty);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("STRAT of AzureAwarePluginExample2.Execute().");

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    string websiteurl = string.Empty;
                    Entity targetEntity = (context.InputParameters["Target"] as Entity).ToEntity<Entity>();
                    websiteurl = targetEntity.GetAttributeValue<string>("websiteurl");
                    if (string.IsNullOrEmpty(websiteurl))
                    {
                        if (string.IsNullOrEmpty(websiteurl) && context.PreEntityImages.Contains("accountImage") && context.PreEntityImages["accountImage"] is Entity)
                        {
                            Entity preEntityImage = (context.PreEntityImages["accountImage"] as Entity).ToEntity<Entity>();
                            websiteurl = preEntityImage.GetAttributeValue<string>("websiteurl");
                        }
                        else
                        {
                            tracingService.Trace("Failed to retreive PreEntityImages.");
                        }
                    }

                    if (string.IsNullOrEmpty(websiteurl))
                    {
                        tracingService.Trace("No website url for account.");
                        return;
                    }

                    new ServiceBusManager(organizationService, tracingService).PostMessageToServiceBus(targetEntity.GetAttributeValue<string>("name"), websiteurl);
                }
                else
                {
                    tracingService.Trace("Failed to retreive target entity.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("Exception {0}", ex.ToString());
                throw;
            }

            tracingService.Trace("END of AzureAwarePluginExample2.Execute().");
        }
    }
}
