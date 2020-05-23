﻿using System;


namespace Donut.Data
{
    public interface ICacheSetSource
    {
        ICacheSet Create(ISetCollection context, Type type);
        IDataSet CreateDataSet(ISetCollection context, Type type);
    }
}