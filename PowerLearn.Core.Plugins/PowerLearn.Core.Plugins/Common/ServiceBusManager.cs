namespace PowerLearn.Core.Plugins.Common
{
    using Microsoft.Xrm.Sdk;
    using PowerLearn.Core.Plugins.Model;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;

    public class ServiceBusManager
    {
        IOrganizationService _organizationService;
        ITracingService _tracingService;

        private const string RETRIEVE_ENVIRONMENT_VARIABLE_SECRETVALUE_ACTION_NAME = "RetrieveEnvironmentVariableSecretValue";
        private const string ENVIRONMENT_VARIABLE_INPUTPARAM_NAME = "EnvironmentVariableName";
        private const string ENV_VARIABLE_NAME = "pl_AccountQueueSASKeySecret";
        private const string ENVIRONMENT_VARIABLE_SECRETVALUE = "EnvironmentVariableSecretValue";

        public ServiceBusManager(IOrganizationService organizationService, ITracingService tracingService)
        {
            _organizationService = organizationService;
            _tracingService = tracingService;
        }

        public async void PostMessageToServiceBus(string accountName, string website)
        {
            _tracingService.Trace("PostMessageToServiceBus() exection start.");

            SharedAccessConnection sharedAccessConnection = this.RetrieveSharedAccessConnection();
            TimeSpan ts = new TimeSpan(0, 0, 90);
            // "https://xxx.servicebus.windows.net/entitypath/messages";
            var serviceBusEntityPathURL = $"https://{sharedAccessConnection.Endpoint.Substring(5)}{sharedAccessConnection.EntityPath}/messages";
            string sasToken = this.GetSASToken(sharedAccessConnection.Endpoint, sharedAccessConnection.SharedAccessKeyName, sharedAccessConnection.SharedAccessKey, ts);
            var json = this.ComposeJsonMessage(accountName, website);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", sasToken);
            var response = await client.PostAsync(serviceBusEntityPathURL, content);

            _tracingService.Trace("PostMessageToServiceBus() execution end.");
        }

        private SharedAccessConnection RetrieveSharedAccessConnection()
        {
            SharedAccessConnection sharedAccessConnection = null;
            _tracingService.Trace("RetrieveSharedAccessConnection() execution start.");

            // Execute Action
            OrganizationRequest req = new OrganizationRequest(RETRIEVE_ENVIRONMENT_VARIABLE_SECRETVALUE_ACTION_NAME);
            req[ENVIRONMENT_VARIABLE_INPUTPARAM_NAME] = ENV_VARIABLE_NAME;
            OrganizationResponse response = _organizationService.Execute(req);
            string connectionString = string.Empty;
            if (response.Results.Count == 0 || !response.Results.Keys.Contains(ENVIRONMENT_VARIABLE_SECRETVALUE))
            {
                _tracingService.Trace("Couldn't retrieve the environment variable secret value.");
                return null;
            }
            response.Results.TryGetValue(ENVIRONMENT_VARIABLE_SECRETVALUE, out connectionString);
            _tracingService.Trace("Retrieve secret value is successful.");
            string[] connStringArray = connectionString.Split(';');
            sharedAccessConnection = new SharedAccessConnection();

            foreach (var item in connStringArray)
            {
                string[] configArray = item.Split('=');
                switch (configArray[0])
                {
                    case "Endpoint":
                        sharedAccessConnection.Endpoint = configArray[1];
                        break;
                    case "SharedAccessKeyName":
                        sharedAccessConnection.SharedAccessKeyName = configArray[1];
                        break;
                    case "SharedAccessKey":
                        sharedAccessConnection.SharedAccessKey = item.Substring(16);
                        break;
                    case "EntityPath":
                        sharedAccessConnection.EntityPath = configArray[1];
                        break;
                    default:
                        break;
                }
            }

            _tracingService.Trace("RetrieveSharedAccessConnection() execution end.");

            return sharedAccessConnection;
        }

        private string GetSASToken(string resourceUri, string keyName, string key, TimeSpan ttl)
        {
            var expiry = GetExpiry(ttl);
            string stringToSign = Uri.EscapeDataString(resourceUri).ToLowerInvariant() + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                Uri.EscapeDataString(resourceUri).ToLowerInvariant(),
                Uri.EscapeDataString(signature), expiry, keyName);
            return sasToken;
        }

        private string GetExpiry(TimeSpan ttl)
        {
            var expirySinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1) + ttl;
            return Convert.ToString((int)expirySinceEpoch.TotalSeconds);
        }

        private string ComposeJsonMessage(string accountName, string website)
        {
            Account account = new Account
            {
                Name = accountName,
                Website = website
            };
            return SerializeToJsonString(account);
        }

        private string SerializeToJsonString(object objectToSerialize)
        {
            using (var ms = new MemoryStream())
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(objectToSerialize.GetType());
                ser.WriteObject(ms, objectToSerialize);
                byte[] json = ms.ToArray();
                ms.Close();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }
        }
    }
}
