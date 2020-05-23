﻿using System;
using System.Collections.Generic;
using Donut.Lex.Expressions;
using MongoDB.Bson;
using Donut.Interfaces;

namespace Donut
{
    public class DonutFunctions
    {
        private static Dictionary<string, IDonutFunction> Functions { get; set; }
        static DonutFunctions()
        {
            Functions = new Dictionary<string, IDonutFunction>();
            Functions["sum"] = new DonutFunction("sum")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$sum", "{0}" } }).ToString()
            };
            Functions["max"] = new DonutFunction("max")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$max", "{0}" } }).ToString()
            };
            Functions["min"] = new DonutFunction("min")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$min", "{0}" } }).ToString()
            };
            Functions["num_unique"] = new DonutFunction("num_unique")
            { IsAggregate = true };
            Functions["std"] = new DonutFunction("std")//Disables because mongo doesn't handle it right
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$stdDevPop", "{0}" } }).ToString()
            };
            Functions["mean"] = new DonutFunction("mean")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$avg", "{0}" } }).ToString()
            };
            Functions["avg"] = new DonutFunction("avg")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                GroupValue = (new BsonDocument { { "$avg", "{0}" } }).ToString()
            };
//            Functions["num_unique"] = new DonutFunction("num_unique")
//            {
//                Type = DonutFunctionType.GroupKey,
//                IsAggregate = true,
//                GroupValue = (new BsonDocument { { "{0}", "{1}"} }).ToString()
//            };
//            Functions["skew"] = new DonutFunction("skew")
//            { IsAggregate = true };
            Functions["day"] = new DonutFunction("day")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfMonth", "{0}" } }).ToString(),
                Eval = (x)=> x.AsDateTime.Day
            };
            Functions["hour"] = new DonutFunction("hour")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$hour", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.Hour
            };
            Functions["month"] = new DonutFunction("month")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$month", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.Month
            };
            Functions["year"] = new DonutFunction("year")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$year", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.Year
            };
            Functions["weekday"] = new DonutFunction("weekday")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfWeek", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.DayOfWeek
            };
            Functions["yearday"] = new DonutFunction("yearday")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfYear", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.DayOfYear
            };
            Functions["dayofyear"] = new DonutFunction("dayOfYear")
            {
                Type = DonutFunctionType.Project,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$dayOfYear", "{0}" } }).ToString(),
                Eval = (x) => x.AsDateTime.DayOfYear
            };
            Functions["mode"] = new DonutFunction("mode")
            { IsAggregate = false };
            Functions["first"] = new DonutFunction("first")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                Projection = (new BsonDocument{ { "$first", "{0}"} }).ToString()
            };
            Functions["last"] = new DonutFunction("last")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = true,
                Projection = (new BsonDocument { { "$last", "{0}" } }).ToString()
            };
            //Custom functions
            Functions["dstime"] = new DsTime("dstime")
            {
                Type = DonutFunctionType.GroupField,
                IsAggregate = false
            };
            Functions["num_unique"] = new NumUnique("num_unique")
            {
                Type = DonutFunctionType.Donut,
                IsAggregate = false
            };

            //Functions["time"] = "(function(timeElem){ return timeElem.getTime() })";
        }

        private static string GetAggtegateBody(BsonDocument filter)
        {
            throw new NotImplementedException();
        }

        private static string GetDayFn()
        {
            return "Utils.GetDay";
        }
        private static string GetWeekdayFn()
        {
            return "Utils.GetWeekDay";
        }
        private static string GetMonthFn()
        {
            return "Utils.GetMonth";
        }
        private static string GetYearFn()
        {
            return "Utils.GetYear";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <param name="expParameters"></param>
        /// <returns></returns>
        public IDonutFunction GetFunction(string function)
        {
            IDonutFunction output = null;
            var lower = function.ToLower();
            if (Functions.ContainsKey(lower))
            {
                output = Functions[lower].Clone();
            }
            else
            {
                throw new Exception($"Unsupported js function: {function}");
            }
            return output;
        }

        public bool IsAggregate(CallExpression callExpression)
        {
            var fKey = callExpression.Name.ToLower();
            if (Functions.ContainsKey(fKey))
            {
                var fn = Functions[fKey];
                return fn.IsAggregate;
            }
            else
            {
                return false;
            }
        }

        public DonutFunctionType GetFunctionType(CallExpression callExpression)
        {
            var fKey = callExpression.Name.ToLower();
            if (Functions.ContainsKey(fKey))
            {
                var fn = Functions[fKey];
                return fn.Type;
            }
            else
            {
                return DonutFunctionType.Standard;
            }
        }
    }
}