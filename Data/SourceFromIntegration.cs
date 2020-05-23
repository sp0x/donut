using System;

namespace Donut.Data
{
    public class SourceFromIntegration : Attribute
    {
        public string IntegrationName { get; set; }
        public SourceFromIntegration(string integerationName)
        {
            IntegrationName = integerationName;
        }
    }
}