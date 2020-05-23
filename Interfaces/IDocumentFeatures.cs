namespace Donut.Interfaces
{
    public interface IDocumentFeatures<T> : IFeaturesWrapper
    {
        T Document { get; set; }
    }
}