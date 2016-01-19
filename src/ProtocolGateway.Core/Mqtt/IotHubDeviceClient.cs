// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.Mqtt
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.ProtocolGateway.Mqtt.Auth;

    public class IotHubDeviceClient : IDeviceClient
    {
        readonly DeviceClient deviceClient;

        public IotHubDeviceClient(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient;
        }

        public static async Task<IDeviceClient> CreateFromConnectionStringAsync(string connectionString)
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(connectionString);
            await client.OpenAsync();
            return new IotHubDeviceClient(client);
        }

        public static DeviceClientFactoryFunc PreparePoolFactory(string baseConnectionString, string poolId, int connectionPoolSize)
        {
            IotHubConnectionStringBuilder csb = IotHubConnectionStringBuilder.Create(baseConnectionString);
            string[] connectionIds = Enumerable.Range(1, connectionPoolSize).Select(index => poolId + index).ToArray();
            int connectionIndex = 0;
            DeviceClientFactoryFunc deviceClientFactory = deviceCredentials =>
            {
                if (++connectionIndex >= connectionPoolSize)
                {
                    connectionIndex = 0;
                }
                //csb.GroupName = connectionIds[connectionIndex]; // todo: uncommment once explicit control over connection pooling is available
                var identity = (IoTHubIdentity)deviceCredentials.Identity;
                csb.AuthenticationMethod = DeriveAuthenticationMethod(csb.AuthenticationMethod, identity.DeviceId, deviceCredentials.Properties);
                csb.HostName = identity.IoTHubHostName;
                string connectionString = csb.ToString();
                return CreateFromConnectionStringAsync(connectionString);
            };
            return deviceClientFactory;
        }

        public Task SendAsync(Message message)
        {
            return this.deviceClient.SendEventAsync(message);
        }

        public Task<Message> ReceiveAsync()
        {
            return this.deviceClient.ReceiveAsync(TimeSpan.MaxValue);
        }

        public Task AbandonAsync(string lockToken)
        {
            return this.deviceClient.AbandonAsync(lockToken);
        }

        public Task CompleteAsync(string lockToken)
        {
            return this.deviceClient.CompleteAsync(lockToken);
        }

        public Task RejectAsync(string lockToken)
        {
            return this.deviceClient.RejectAsync(lockToken);
        }

        public Task DisposeAsync()
        {
            return this.deviceClient.CloseAsync();
        }

        internal static IAuthenticationMethod DeriveAuthenticationMethod(IAuthenticationMethod currentAuthenticationMethod, string deviceId, AuthenticationProperties authenticationProperties)
        {
            switch (authenticationProperties.Scope)
            {
                case AuthenticationScope.None:
                    var policyKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithSharedAccessPolicyKey;
                    if (policyKeyAuth != null)
                    {
                        return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, policyKeyAuth.PolicyName, policyKeyAuth.Key);
                    }
                    var deviceKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithRegistrySymmetricKey;
                    if (deviceKeyAuth != null)
                    {
                        return new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKeyAuth.DeviceId);
                    }
                    var deviceTokenAuth = currentAuthenticationMethod as DeviceAuthenticationWithToken;
                    if (deviceTokenAuth != null)
                    {
                        return new DeviceAuthenticationWithToken(deviceId, deviceTokenAuth.Token);
                    }
                    throw new InvalidOperationException("");
                case AuthenticationScope.SasToken:
                    return new DeviceAuthenticationWithToken(deviceId, authenticationProperties.Secret);
                case AuthenticationScope.DeviceKey:
                    return new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, authenticationProperties.Secret);
                case AuthenticationScope.HubKey:
                    return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, authenticationProperties.PolicyName, authenticationProperties.Secret);
                default:
                    throw new InvalidOperationException("Unexpected AuthenticationScope value: " + authenticationProperties.Scope);
            }
        }
    }
}