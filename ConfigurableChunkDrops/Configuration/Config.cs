﻿namespace ConfigurableChunkDrops.Configuration
{
    using SMCLib.Json;
    using System.Collections.Generic;

    public class Config: ConfigFile
    {
        public Config(string fileName = "config", string subfolder = null) : base(fileName, subfolder)
        {
        }

        public Dictionary<TechType, Dictionary<TechType, float>> Breakables = new();
    }
}
