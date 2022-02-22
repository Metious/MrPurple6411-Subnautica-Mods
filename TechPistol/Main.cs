﻿using BepInEx;

#if !EDITOR
namespace TechPistol
{
    using HarmonyLib;
    using SMCLib.Handlers;
    using System.IO;
    using System.Reflection;
    using Configuration;
    using Module;
    using UnityEngine;

    public class Main:BaseUnityPlugin
    {
        private const string bundlePath = 
#if SN1
            "Assets/TechPistol";
#elif BZ
            "Assets/TechPistolBZ";
#endif
        private static Assembly assembly = Assembly.GetExecutingAssembly();
        private static string modPath = Path.GetDirectoryName(assembly.Location);
        internal static AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(modPath, bundlePath));
        internal static Config Config { get; } = OptionsPanelHandler.RegisterModOptions<Config>();
        internal static PistolFragmentPrefab PistolFragment { get; } = new();
        internal static PistolPrefab Pistol { get; } = new();

        public void  Awake()
        {
            PistolFragment.Patch();
            Pistol.Patch();

            Harmony.CreateAndPatchAll(assembly, $"MrPurple6411_{assembly.GetName().Name}");
        }
    }
}
#endif