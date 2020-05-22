﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Generation;
using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    /// <summary>
    /// A code generator base class, that helps with generating features.
    /// </summary>
    public abstract class FeatureCodeGenerator : CodeGenerator
    {
        protected DonutScript Script { get; set; }

        public FeatureCodeGenerator(DonutScript script)
        {
            this.Script = script;
        }
        
        /// <summary>
        /// Add all features
        /// </summary>
        /// <param name="featureAssignments"></param>
        public void AddAll(IEnumerable<AssignmentExpression> featureAssignments)
        {
            foreach (var f in featureAssignments)
            {
                try
                {
                    Add(f);
                }
                catch (DonutFunctionNotImplementedException ex)
                {
                    Trace.WriteLine(ex.Message);
#if DEBUG
                    Debug.WriteLine(ex.Message);
#endif
                }

            }
        }

        /// <summary>
        /// Add a feature assignment to the aggregate pipeline
        /// </summary>
        /// <param name="feature"></param>
        public abstract void Add(AssignmentExpression feature);

        /// <summary>
        /// Get the name of a feature.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        protected string GetFeatureName(AssignmentExpression feature)
        {
            string fName = feature.Member.ToString();
            IExpression fExpression = feature.Value;
            var featureFType = fExpression.GetType();
            if (featureFType == typeof(NameExpression))
            {
                var member = (fExpression as NameExpression).Member?.ToString();
                //In some cases we might just use the field
                if (string.IsNullOrEmpty(member)) member = fExpression.ToString();
                if (Script.Targets.Any(x=>x.Column.Name==member))
                {
                    fName = member;
                }
            }
            return fName;
        }
    }
}
