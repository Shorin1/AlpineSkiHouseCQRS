﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlpineSkiHouseCQRS.Infrastructure
{
    public interface IQuery : IDataModel
    {
        object Result { get; }
        Type ResultType { get; }
    }
}
