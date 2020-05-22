﻿using System;
using System.Collections.Generic;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Generators;
using Netlyt.Interfaces;

namespace Donut.Lex.Generation
{
    public static class Extensions
    {
        private static Dictionary<Type, Func<object,CodeGenerator>> _generators;
        static Extensions()
        {
            _generators = new Dictionary<Type, Func<object, CodeGenerator>>();
            _generators.Add(typeof(MapReduceExpression), (x) => new MapReduceMapGenerator());
            _generators.Add(typeof(MapAggregateExpression), (x) => new MapReduceAggregateGenerator());
            _generators.Add(typeof(DonutScript), (x) => new DonutScriptCodeGenerator((DonutScript)x));
        }

        /// <summary>
        /// Gets the appropriate code generator that can generate code form the expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static CodeGenerator GetCodeGenerator(this IExpression expression)
        {
            var expType = expression.GetType();
            Func<object, CodeGenerator> generatorFunc;
            if (_generators.TryGetValue(expType, out generatorFunc))
            {
                var generator = generatorFunc(expression);
                return generator;
            } 
            else
            {
                throw new Exception("No generator for this expression!");
            }
        }
    }
}
