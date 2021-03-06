﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlpineSkiHouseCQRS.Infrastructure
{
    public interface IQueryHandler<IQuery>
    {
        Task<IQuery> Handle(IQuery parameters);
    }
}
