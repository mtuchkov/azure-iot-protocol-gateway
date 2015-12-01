// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.Mqtt.Auth
{
    using System;

    public sealed class Identity
    {
        public string IoTHubName { get; private set; }

        public string DeviceId { get; private set; }

        public Identity(string iotHubName, string deviceId)
        {
            this.IoTHubName = iotHubName;
            this.DeviceId = deviceId;
        }

        public override string ToString()
        {
            return this.IoTHubName + "/" + this.DeviceId;
        }

        public static Identity Parse(string username)
        {
            int delimiterPos = username.IndexOf("/", StringComparison.Ordinal);
            string iotHubName = username.Substring(0, delimiterPos).Substring(0, username.IndexOf('.'));
            string deviceId = username.Substring(delimiterPos + 1);

            return new Identity(iotHubName, deviceId);
        }
    }
}