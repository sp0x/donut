using System.Threading.Tasks.Dataflow;
using Donut.Features;
using Donut.Interfaces;

namespace Donut.Blocks
{
    public interface IDonutBlock<T>
        where T : IIntegratedDocument
    {
        IPropagatorBlock<T, FeaturesWrapper<T>> FeaturePropagator { get; }
        BaseFlowBlock<T, T> FlowBlock { get; }
    }
}