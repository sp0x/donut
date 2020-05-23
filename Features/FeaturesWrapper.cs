﻿using System.Collections.Generic;
using Donut.Interfaces;


namespace Donut.Features
{
    /// <summary>
    /// Wraps a document and it's features.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FeaturesWrapper<T> : IDocumentFeatures<T>
    {
        /// <summary>
        /// The features of the document.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Features { get; set; }

        /// <summary>
        /// The document to which the features are related
        /// </summary>
        public T Document { get; set; }
        public FeaturesWrapper() { }
        protected FeaturesWrapper(T doc, IEnumerable<KeyValuePair<string, object>> features)
        {
            this.Document = doc;
            this.Features = features;
        }
    }
}