﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut.Blocks;
using Donut.Features;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Batching;
using Netlyt.Interfaces.Data;

namespace Donut
{


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDonut"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class DonutRunner<TDonut, TContext, TData> : IDonutRunner<TDonut, TData>
        where TContext: DonutContext
        where TDonut : Donutfile<TContext, TData>
        where TData : class, IIntegratedDocument
    {
        private Harvester<TData> _harvester;
        private IMongoCollection<BsonDocument> _featuresCollection;
        private IPropagatorBlock<TData, FeaturesWrapper<TData>> _featuresBlock;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="harvester"></param>
        /// <param name="db"></param>
        /// <param name="featuresCollection"></param>
        public DonutRunner(
            Harvester<TData> harvester,
            IDatabaseConfiguration db,
            string featuresCollection)
        {
            if (string.IsNullOrEmpty(featuresCollection))
            {
                throw new ArgumentNullException(nameof(featuresCollection));
            }
            _harvester = harvester;
            //var mlist = new MongoList(db, featuresCollection);
            _featuresCollection = MongoHelper.GetCollection(db.Value);
        }

        public async Task<IHarvesterResult> Run(IDonutfile donut, IFeatureGenerator<TData> featureGenerator) 
            => await Run(donut as TDonut, featureGenerator);

        /// <summary>
        /// Runs the donut.
        /// </summary>
        /// <param name="donut"></param>
        /// <param name="getFeatureGenerator"></param>
        /// <returns></returns>
        public async Task<IHarvesterResult> Run(TDonut donut, IFeatureGenerator<TData> getFeatureGenerator)
        {
            var integration = donut.Context.Integration;
            //Create our destination block
            var donutBlock = donut.CreateDataflowBlock(getFeatureGenerator);
            var dataProcessingBlock = donutBlock.FlowBlock;
            _featuresBlock = donutBlock.FeaturePropagator;

            var insertCreator = new TransformBlock<FeaturesWrapper<TData>, BsonDocument>((x) =>
            { 
                var rawFeatures = new BsonDocument();
                var featuresDocument = new IntegratedDocument(rawFeatures);
                //add some cleanup, or feature document definition, because right now the original document is used
                //either clean  it up or create a new one with just the features.
                //if (doc.Document.Value.Contains("events")) doc.Document.Value.Remove("events");
                //if (doc.Document.Value.Contains("browsing_statistics")) doc.Document.Value.Remove("browsing_statistics");
                foreach (var featurePair in x.Features)
                {
                    var name = featurePair.Key;
                    if (string.IsNullOrEmpty(name)) continue;
                    var featureval = featurePair.Value;
                    rawFeatures.Set(name, BsonValue.Create(featureval));
                } 
                featuresDocument.IntegrationId = integration.Id;
                featuresDocument.APIId = integration.APIKey.Id;
                x.Features = null;
                return rawFeatures;
            });
            var insertBatcher = new MongoInsertBatch<BsonDocument>(_featuresCollection, 3000);
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            //Insert our features
            _featuresBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            //After all data is processed, extract the features
            dataProcessingBlock.ContinueWith(() =>
            {
                var extractionTask = RunFeatureExtraction(donut);
                Task.WaitAll(extractionTask);
            }); 
            _harvester.SetDestination(dataProcessingBlock);
            
            var harvesterRun = await _harvester.Run(); 
            //If we have to repeat it, handle this..
            return harvesterRun;
        }

        private async Task RunFeatureExtraction(TDonut donut)
        {
            //Don`t accept any more data
            donut.Complete();
            try
            {
                //Prepare anything that we need to do, like running mongodb aggregate pipelines
                await donut.PrepareExtraction();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Donut error while preparing extraction: " + ex.Message);
            }
            if (donut.ReplayInputOnFeatures && !donut.SkipFeatureExtraction)
            {
                var featuresFlow = new TransformFlowBlock<TData, FeaturesWrapper<TData>>
                    (_featuresBlock);
                _harvester.Reset();
                _harvester.SetDestination(featuresFlow);
                var featuresResult = await _harvester.Run(); 
            }
            else
            {
                if (!donut.SkipFeatureExtraction)
                {
                    await donut.CompleteExtraction();
                }
            }

            try
            {
                await donut.OnFinished();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Donut error while finishing: " + ex.Message);
            }
        }

    }
}