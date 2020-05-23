using System.Collections.Generic;


namespace Donut.Integration
{
    public interface IIntegrationSet
    {
        IIntegration Definition { get; }
        IInputSource Source { get; set; }

        IEnumerable<IntegratedDocument> AsEnumerable();
        bool Equals(object obj);
        int GetHashCode();
        void Reset();
        IntegratedDocument Wrap(dynamic data);
    }
}