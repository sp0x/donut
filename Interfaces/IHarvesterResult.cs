namespace Donut.Interfaces
{
    public interface IHarvesterResult
    {
        int ProcessedEntries { get; }
        int ProcessedShards { get; }
    }
}