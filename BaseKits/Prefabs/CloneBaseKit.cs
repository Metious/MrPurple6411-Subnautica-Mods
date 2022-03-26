﻿namespace BaseKits.Prefabs
{
    using SMLHelper.V2.Assets;
    using SMLHelper.V2.Handlers;
    using System.Collections;
    using System.Collections.Generic;
#if SN1
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#else
    using SMLHelper.V2.Crafting;
#endif
    using UnityEngine;

    internal class CloneBaseKit : Craftable
    {
        private static readonly List<TechType> UnlockRequired = new()
        {
            TechType.BaseBulkhead, TechType.BaseRoom, TechType.BaseMapRoom, TechType.BaseMoonpool,
            TechType.BaseBioReactor, TechType.BaseNuclearReactor, TechType.BaseObservatory, TechType.BaseUpgradeConsole,
            TechType.BaseFiltrationMachine, TechType.BaseWaterPark
        };

        private GameObject processedPrefab;
        private readonly TechType TypeToClone;
        private readonly TechGroup group;
        private readonly TechCategory category;

        internal CloneBaseKit(TechType typeToClone) : base($"Kit_{typeToClone}",
            $"{Language.main.Get(typeToClone)} Kit", "Super Compressed Base in a Kit")
        {
            TypeToClone = typeToClone;

            if (CraftData.GetBuilderIndex(typeToClone, out var originalGroup, out var originalCategory, out _))
            {
                string originalCategoryString =
                    Language.main.Get(uGUI_BlueprintsTab.techCategoryStrings.Get(originalCategory));

                group = TechGroupHandler.Main.AddTechGroup($"{originalGroup}_Kits", $"{originalGroup} - Kits");
                category = TechCategoryHandler.Main.AddTechCategory($"{originalCategory}_Kits",
                    $"{originalCategoryString} - Kits");
                if (!TechCategoryHandler.Main.TryRegisterTechCategoryToTechGroup(group, category))
                {
                    QModManager.Utility.Logger.Log(QModManager.Utility.Logger.Level.Error,
                        $"Failed to Register {category} to {group}");
                }
            }

            OnFinishedPatching += () =>
            {
                CraftDataHandler.SetBackgroundType(this.TechType, CraftData.BackgroundType.PlantAir);
            };
        }

        public override TechType RequiredForUnlock =>
            UnlockRequired.Contains(TypeToClone) ? TypeToClone : TechType.None;

        public override TechGroup GroupForPDA => group;

        public override TechCategory CategoryForPDA => category;

        public override float CraftingTime => 10f;

        public override GameObject GetGameObject()
        {
            GameObject go = null;
            if (processedPrefab != null)
            {
                go = Object.Instantiate(processedPrefab, default, default, true);
                return go;
            }

            var prefab = Utils.CreateGenericLoot(TechType);

            if (prefab != null)
            {
                processedPrefab = prefab;
                processedPrefab.SetActive(false);
                go = Object.Instantiate(processedPrefab, default, default, true);
            }

            return go;
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            GameObject go = null;
            if (processedPrefab != null)
            {
                go = Object.Instantiate(processedPrefab, default, default, true);
                gameObject.Set(go);
                yield break;
            }

            var prefab = Utils.CreateGenericLoot(TechType);
            if (prefab != null)
            {
                processedPrefab = prefab;
                processedPrefab.SetActive(false);
                go = Object.Instantiate(processedPrefab, default, default, true);
            }

            gameObject.Set(go);
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return CraftDataHandler.GetTechData(TypeToClone);
        }

        protected override Sprite GetItemSprite()
        {
            return SpriteManager.Get(TypeToClone);
        }
    }
}