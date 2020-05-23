﻿using Donut.Interfaces;

namespace Donut
{
    public class DonutCodeFeatureDefinition : IDonutCodeFeatureDefinition
    {

        /// <summary>
        /// The script that gathers information from an integration's documents before extracting a feature.
        /// </summary>
        public string PrepareScript { get; set; }

        public string ExtractionScript { get; set; }

        public static DonutCodeFeatureDefinition Empty { get; set; } = new DonutCodeFeatureDefinition()
        {

        };

        public string GetValue()
        {
            return PrepareScript;
        }
    }
}