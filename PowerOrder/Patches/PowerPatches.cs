﻿namespace PowerOrder.Patches
{
    using HarmonyLib;
    using SMCLib.Utility;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    [HarmonyPatch(typeof(PowerRelay), nameof(PowerRelay.AddInboundPower))]
    internal class PowerRelay_AddInboundPower
    {
        [HarmonyPrefix]
        private static void Prefix(PowerRelay __instance, IPowerInterface powerInterface)
        {
            try
            {
                if(__instance?.inboundPowerSources == null || __instance.inboundPowerSources.Contains(powerInterface))
                    return;
                Main.logSource.LogDebug($"{Regex.Replace(__instance.gameObject.name, @"\(.*?\)", "")} AddInboundPower: {Regex.Replace(powerInterface.GetType().Name, @"\(.*?\)", "")}");
                Main.config.doSort = true;
            }
            catch(Exception e)
            {
                Main.logSource.LogError(e);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(PowerRelay __instance)
        {
            try
            {
                if(!Main.config.doSort)
                    return;
                var info = __instance.inboundPowerSources;
                var test = new List<IPowerInterface>(info);
                test.Sort((i, i2) =>
                {
                    var p = (UnityEngine.MonoBehaviour)i;
                    var p2 = (UnityEngine.MonoBehaviour)i2;
                    var pn = GetOrderNumber(p.gameObject.name);
                    var p2n = GetOrderNumber(p2.gameObject.name);
                    return Math.Sign(pn - p2n);
                });
                __instance.inboundPowerSources = test;
                Main.config.doSort = false;
            }
            catch(Exception e)
            {
                Main.logSource.LogError( e);
            }
        }
        private static int GetOrderNumber(string name)
        {
            for(var x = 0; x < Main.config.Order.Count; x++)
            {
                var kvp = Main.config.Order.ElementAt(x);
                if(name.ToLower().Contains(kvp.Value.ToLower()))
                {
                    var order = kvp.Key;
                    if(order > Main.config.Order.Count || order < 1)
                    {
                        Main.logSource.LogError(kvp.Key + " has an invalid order number.  Please fix this and try again.  (Must be within 1-" + Main.config.Order.Count + ")");
                        ErrorMessage.AddMessage(kvp.Key + " has an invalid order number.  Please fix this and try again.  (Must be within 1-" + Main.config.Order.Count + ")");
                        throw new Exception();
                    }
                    return order;
                }
            }
            name = Regex.Replace(name, @"\(.*?\)", "");
            Main.logSource.LogInfo("New power source found: " + name);
            Main.config.Order.Add(Main.config.Order.Count + 1, name);
            Main.config.Save();
            return Main.config.Order.Count + 1;
        }
    }
}
