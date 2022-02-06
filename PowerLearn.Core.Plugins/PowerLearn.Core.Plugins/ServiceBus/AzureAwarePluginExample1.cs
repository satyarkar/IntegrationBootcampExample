namespace PowerLearn.Core.Plugins.ServiceBus
{
    using Microsoft.Xrm.Sdk;
    using System;

    public class AzureAwarePluginExample1 :IPlugin
    {
        private Guid _serviceEndPointId;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AzureAwarePluginExample1(string config)
        {
            if (string.IsNullOrEmpty(config) || !Guid.TryParse(config, out _serviceEndPointId))
            {
                throw new InvalidPluginExecutionException("Service enpoint ID should be passed as config");
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("STRAT of AzureAwarePluginExample1.Execute().");
            IServiceEndpointNotificationService notificationService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
            if (notificationService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve service bus service.");
            }

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    PostMessageToServiceBus(context, tracingService, notificationService);
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

            tracingService.Trace("END of AzureAwarePluginExample1.Execute().");
        }

        private void PostMessageToServiceBus(IPluginExecutionContext context, ITracingService tracingService, IServiceEndpointNotificationService notificationService)
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

            tracingService.Trace("Posting the execution context.");
            string response = notificationService.Execute(new EntityReference("serviceendpoint", _serviceEndPointId), context);
            if (!string.IsNullOrEmpty(response))
            {
                tracingService.Trace("Response = {0}", response);
            }
            tracingService.Trace("Done.");
        }
    }
}
