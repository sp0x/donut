﻿using System;
using Donut.Interfaces;

namespace Donut.Caching
{
    public class CacheBacking : Attribute
    {
        public CacheType Type { get; set; }
        public CacheBacking(CacheType type) { Type = type; }
    }
}