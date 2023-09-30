﻿namespace GravTrapStorage.Configuration;

using Nautilus.Json;
using Nautilus.Options.Attributes;
using System.Collections.Generic;

[Menu("Grav Trap Storage")]
public class SMLConfig:ConfigFile
{
    [Slider("Storage Width", 1, 8, DefaultValue = 4, Step = 1f), OnChange(nameof(ApplyOptions))]
    public int Width = 4;
    
    [Slider("Storage Height", 1, 10, DefaultValue = 4, Step = 1f), OnChange(nameof(ApplyOptions))]
    public int Height = 4;

    [Slider("Distance to pickup objects", 3, 30, DefaultValue = 3)]
    public int Distance = 3;
    
    [Slider("Transfer to storage targeting Distance ", 3, 200, DefaultValue = 3)]
    public int TransferDistance = 3;
    
    private void ApplyOptions()
    {
        foreach (StorageContainer container in Patches.GravspherePatches.StorageContainers.Values)
        {
            container.Resize(Width, Height);
        }
    }
}