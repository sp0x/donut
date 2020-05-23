﻿using System.Collections.Generic;
using Donut.Lex.Data;
using Donut.Interfaces;

namespace Donut.Lex.Generators
{
    public class AggregateJobTree
    {
        private IDonutScript _script;
        public List<AggregateStage> Stages { get; set; }
        public string Name { get; set; }

        public AggregateJobTree(IDonutScript script)
        {
            _script = script;
            Stages = new List<AggregateStage>();
        }

        public AggregateStage AddGroup(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public AggregateStage AddFunction(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public AggregateStage AddProjection(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public void Clear()
        {
            Stages.Clear();
        }

        public AggregateJobTree Clone()
        {
            var jtree = new AggregateJobTree(_script);
            foreach (var stage in Stages)
            {
                jtree.Stages.Add(stage.Clone());
            }
            return jtree;
        }
    }

    public enum AggregateStageType { Group, Project, Match }
}