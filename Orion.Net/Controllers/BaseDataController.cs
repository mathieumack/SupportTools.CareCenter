﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orion.Net.Core.Interfaces;
using Orion.Net.Core.Results;

namespace Orion.Net.Controllers
{
    public class BaseDataController<T> : Controller where T : ClientScriptResult, new()
    {
        // Temporary cache management for tests only.
        // TODO : replace with a  distributed cache management system
        public static Dictionary<Guid, object> CacheManager = new Dictionary<Guid, object>();

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public T Get(Guid id)
        {
            if (CacheManager.ContainsKey(id))
            {
                var result = CacheManager[id];
                CacheManager.Remove(id);
                return result as T;
            }

            return new T();
        }
    }
}
