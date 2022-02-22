﻿namespace PowerOrder.Configuration
{
    using SMCLib.Utility;
    using SMCLib.Options;
    using System;
    using System.Linq;

    internal class Options: ModOptions
    {
        private readonly Config config = Main.config;

        public Options() : base("PowerOrder")
        {
            try
            {
                config.Load();
            }
            catch(Exception e)
            {
                Main.logSource.LogError($"Failed to load Config file. Generating fresh file.\n {e}");
            }

            config.Order = config.Order.OrderBy(p => p.Key).ThenBy(p => p.Value).ToDictionary(t => t.Key, t => t.Value);
            config.Save();

            ChoiceChanged += Options_ChoiceChanged;
        }


        private void Options_ChoiceChanged(object sender, ChoiceChangedEventArgs e)
        {
            if(!e.Id.Contains("PowerOrder_"))
            {
                return;
            }

            var key = int.Parse(e.Id.Replace("PowerOrder_", ""));

            config.Order.TryGetValue(key, out var oldValue);

            var otherKey = config.Order.First((x) => x.Value == e.Value).Key;
            config.Order[otherKey] = oldValue;
            config.Order[key] = e.Value;
            config.Save();

            try
            {
                var currentTab = Main.optionsPanel.currentTab;
                Main.optionsPanel.RemoveTabs();
                Main.optionsPanel.AddTabs();
                Main.optionsPanel.SetVisibleTab(currentTab);
            }
            catch(Exception er)
            {
                Main.logSource.LogError(er);
                ErrorMessage.AddError(er.Message);
            }
        }

        public override void BuildModOptions()
        {
            var choices = config.Order.Values.ToArray();
            foreach(var key in config.Order.Keys)
            {
                AddChoiceOption($"PowerOrder_{key}", key.ToString(), choices, key - 1);
            }
        }
    }
}
