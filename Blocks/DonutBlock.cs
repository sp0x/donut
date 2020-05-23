﻿using System.Threading.Tasks.Dataflow;
using Donut.Features;
using Donut.Interfaces;

namespace Donut.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class DonutBlock<T> : IDonutBlock<T>
        where T : class, IIntegratedDocument
    {
        /// <summary>
        /// The flow block that's used as the root of the dataflow.
        /// </summary>
        public BaseFlowBlock<T, T> FlowBlock { get; private set; }
        /// <summary>
        /// The feature propagator block.
        /// </summary>
        public IPropagatorBlock<T, FeaturesWrapper<T>> FeaturePropagator { get; private set; }

        public DonutBlock(BaseFlowBlock<T, T> flowblock,
            IPropagatorBlock<T, FeaturesWrapper<T>> featureblock)
        {
            FlowBlock = flowblock;
            FeaturePropagator = featureblock;
        }
    }
}