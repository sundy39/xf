using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Client.Components
{
    public static class ApiClientManager
    {
        private static readonly Dictionary<string, ApiClient> ApiClients = new Dictionary<string, ApiClient>();

        public static ApiClient GetApiClient(string baseAddress)
        {
            if (!ApiClients.ContainsKey(baseAddress))
            {
                ApiClients[baseAddress] = new ApiClient(baseAddress);
            }

            return ApiClients[baseAddress];
        }

        public const string BaseAddress_Key = "BaseAddress";

        public static ApiClient GetApiClient()
        {
            string baseAddress = ConfigurationManager.AppSettings[BaseAddress_Key];
            return GetApiClient(baseAddress);
        }


    }
}
