namespace Increased_Resource_Spawns.Configuration;

using Nautilus.Json;
using Nautilus.Options.Attributes;
using System.Collections.Generic;

[Menu("Increased Resource Spawn Settings")]
public class SMLConfig: ConfigFile
{
    [Slider("Resource Multiplier", 1, 10, DefaultValue = 1, Step = 1, Format = "{0:F0}")]
    public int ResourceMultiplier = 1;

    public List<string> Blacklist = new() 
    { 
        TechType.CrashHome.AsString(),
#if SUBNAUTICA
        TechType.SpikePlant.AsString() 
#endif
    };

    public List<string> WhiteList = new();
}