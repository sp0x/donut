using Donut.Interfaces;

namespace Donut.Data
{
    public class HarvesterResult : IHarvesterResult
    {
        public int ProcessedEntries { get; private set; }
        public int ProcessedShards { get; private set; }

        public HarvesterResult(int shards, int elements)
        {
            ProcessedEntries = elements;
            ProcessedShards = shards;
        }
    }
}