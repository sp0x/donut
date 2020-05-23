using System.Collections.Generic;

namespace Donut.Interfaces
{
    public interface IFeaturesWrapper
    {
        IEnumerable<KeyValuePair<string, object>> Features { get; set; }
    }
    public interface IFeaturesWrapper<T> : IFeaturesWrapper
    {
        T Document { get; set; }
    }
}