using System.Collections.Generic;

namespace Donut.Interfaces.Cloud
{
    public interface ICloudNodeNotification
    {
        string Token { get; }
        Dictionary<string, string> Headers { get; }
    }
}