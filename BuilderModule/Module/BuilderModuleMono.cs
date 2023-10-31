namespace BuilderModule.Module;

using System.Collections;
using System.Text;
using UnityEngine;
using UWE;

public class BuilderModuleMono: MonoBehaviour
{
    public int ModuleSlotID;

    public Vehicle vehicle;
    public EnergyMixin energyMixin;
    public EnergyInterface energyInterface;
    public PowerRelay powerRelay;

    public bool isToggle;

    public float powerConsumptionConstruct = 0.5f;
    public float powerConsumptionDeconstruct = 0.5f;
    private static FMODAsset completeSound;
    private int handleInputFrame = -1;
    private string deconstructText;
    private string constructText;
    private string noPowerText;

#if BELOWZERO
	internal SeaTruckUpgrades seaTruck;
	internal SeaTruckLights lights;
	internal Hoverbike hoverbike;
#endif

	public void Awake()
    {
        if(completeSound is null && PrefabDatabase.TryGetPrefabFilename(CraftData.GetClassIdForTechType(TechType.Builder), out var BuilderFilename))
        {
            AddressablesUtility.LoadAsync<GameObject>(BuilderFilename).Completed += (x) =>
            {
                var gameObject1 = x.Result;
                var builderPrefab = gameObject1 != null ? gameObject1.GetComponent<BuilderTool>() : null;
                if (builderPrefab is not null)
                    completeSound = Instantiate(builderPrefab.completeSound, gameObject.transform);
            };
        }
    }

    public void OnToggle(int slotID, bool state)
    {
        var techType = TechType.None;

        if(vehicle != null)
            techType = vehicle.GetSlotBinding(slotID);

        if (!Main.BuilderModules.Contains(techType)) return;
        isToggle = state;
        if(!isToggle)
            OnDisable();
    }

    public void Toggle()
    {
        while (true)
        {
            isToggle = !isToggle;
            if (isToggle)
            {
                if (energyMixin != null && energyMixin.charge > 0f || powerRelay != null && powerRelay.GetPower() > 0f || energyInterface != null && energyInterface.TotalCanProvide(out _) > 0f)
                {
                    ErrorMessage.AddMessage($"BuilderModule Enabled");
                    Player.main.GetPDA().Close();
                    uGUI_BuilderMenu.Show();
                    handleInputFrame = Time.frameCount;
                    return;
                }
                else
                {
                    ErrorMessage.AddMessage($"Insufficient Power");
                    continue;
                }
            }

            ErrorMessage.AddMessage($"BuilderModule Disabled");
            OnDisable();
            break;
        }
    }

    public void OnDisable()
    {
        if(uGUI_BuilderMenu.IsOpen())
            uGUI_BuilderMenu.Hide();
        if(Builder.prefab != null)
            Builder.End();
    }


    protected void Update()
    {
        if(isToggle && !(Player.main.GetVehicle() != null
#if BELOWZERO
            || Player.main.IsPilotingSeatruck() || Player.main.inHovercraft
#endif
            ))
        {
            Toggle();
            return;
        }

#if BELOWZERO
        if(isToggle && lights != null && powerRelay.IsPowered())
        {
            lights.lightsActive = true;
            lights.floodLight.SetActive(true);
        }
#endif



        if(VehicleCheck())
        {
            if(GameInput.GetButtonDown(GameInput.Button.Reload))
            {
                Toggle();
                return;
            }
        }


        UpdateText();
        if(isToggle)
        {
            if(GameInput.GetButtonDown(GameInput.Button.RightHand) && !Player.main.GetPDA().isOpen)
            {
                if(Builder.isPlacing)
                    Builder.End();

                if (!(energyMixin != null && energyMixin.charge > 0f) &&
                    !(powerRelay != null && powerRelay.GetPower() > 0f) &&
                    !(energyInterface != null && energyInterface.TotalCanProvide(out _) > 0f))
                    return;
                
                Player.main.GetPDA().Close();
                uGUI_BuilderMenu.Show();
                handleInputFrame = Time.frameCount;
                return;
            }
            if(Builder.isPlacing)
            {
                if(GameInput.GetButtonDown(GameInput.Button.LeftHand))
                {
                    if(Builder.TryPlace())
                        Builder.End();
                    return;
                }
                else if(handleInputFrame != Time.frameCount && GameInput.GetButtonDown(GameInput.Button.Exit))
                {
                    Builder.End();
                    return;
                }
                FPSInputModule.current.EscapeMenu();
                Builder.Update();
            }
            if(!uGUI_BuilderMenu.IsOpen() && !Builder.isPlacing)
            {
                HandleInput();
            }
        }
    }

    private bool VehicleCheck()
    {
        if(vehicle is not null && vehicle == Player.main.GetVehicle())
        {
            return true;
        }
#if BELOWZERO
        else if(hoverbike is not null && hoverbike == Player.main.GetComponentInParent<Hoverbike>())
        {
            return true;
        }
        else if(seaTruck is not null && seaTruck == Player.main.GetComponentInParent<SeaTruckUpgrades>())
        {
            return true;
        }
#endif
        return false;
    }

    private void HandleInput()
    {
        if(handleInputFrame == Time.frameCount)
            return;

        handleInputFrame = Time.frameCount;

        if(!AvatarInputHandler.main.IsEnabled() || TryDisplayNoPowerTooltip())
            return;

        Targeting.AddToIgnoreList(Player.main.gameObject);
        GameObject Object = null;
        float num = 9000;
        if(vehicle != null)
            Targeting.GetTarget(vehicle.gameObject, 60f, out Object, out num);

        if(Object is null)
            return;

        var buttonHeld = GameInput.GetButtonHeld(GameInput.Button.LeftHand);
        var buttonDown = GameInput.GetButtonDown(GameInput.Button.Deconstruct);
        var buttonHeld2 = GameInput.GetButtonHeld(GameInput.Button.Deconstruct);
        var constructable = Object.GetComponentInParent<Constructable>();
        if(constructable != null && num > constructable.placeMaxDistance * 2)
            constructable = null;

        if(constructable != null)
        {
            OnHover(constructable);
            if(buttonHeld)
            {
                Construct(constructable, true);
                Construct(constructable, true);
                Construct(constructable, true);
            }
            else if(constructable.DeconstructionAllowed(out var text))
            {
                if (!buttonHeld2) return;
                if(!constructable.constructed)
                {
                    Construct(constructable, false);
                    Construct(constructable, false);
                    Construct(constructable, false);
                    return;
                }

                constructable.SetState(false, false);
            }
            else if(buttonDown && !string.IsNullOrEmpty(text))
            {
                ErrorMessage.AddMessage(text);
            }
        }
        else
        {
            var baseDeconstructable = Object.GetComponentInParent<BaseDeconstructable>() ?? Object.GetComponentInParent<BaseExplicitFace>()?.parent;

            if (baseDeconstructable is null) return;
            if(!baseDeconstructable.DeconstructionAllowed(out var text))
            {
                if(buttonDown && !string.IsNullOrEmpty(text))
                    ErrorMessage.AddMessage(text);
                return;
            }

            OnHover(baseDeconstructable);
            if(buttonDown)
                baseDeconstructable.Deconstruct();
        }
    }

    private bool TryDisplayNoPowerTooltip()
    {
        if (!((energyInterface != null ? energyInterface.TotalCanProvide(out _) : 0f) <= 0f) || 
            !((powerRelay != null ? powerRelay.GetPower() : 0f) <= 0f) ||
            !((energyMixin != null ? energyMixin.charge : 0f) <= 0f)) return false;
        
        var main = HandReticle.main;
        main.SetText(HandReticle.TextType.Hand, this.noPowerText, true);
        main.SetIcon(HandReticle.IconType.Default);
        return true;
    }

    private void UpdateText()
    {
        var buttonFormat = LanguageCache.GetButtonFormat("ConstructFormat", GameInput.Button.LeftHand);
        var buttonFormat2 = LanguageCache.GetButtonFormat("DeconstructFormat", GameInput.Button.Deconstruct);
        constructText = Language.main.GetFormat("ConstructDeconstructFormat", buttonFormat, buttonFormat2);
        deconstructText = buttonFormat2;
        noPowerText = Language.main.Get("NoPower");
    }

    private void Construct(Constructable c, bool state)
    {
        if (c != null &&
            !c.constructed &&
            ((energyInterface != null ? energyInterface.TotalCanProvide(out _) : 0f) > 0f ||
             (powerRelay != null ? powerRelay.GetPower() : 0f) > 0f ||
             (energyMixin != null ? energyMixin.charge : 0f) > 0f))
            StartCoroutine(ConstructAsync(c, state));
    }

    private IEnumerator ConstructAsync(Constructable c, bool state)
    {
        if(
#if SUBNAUTICA
			!GameModeUtils.IsCheatActive(GameModeOption.NoEnergy)
#else
			GameModeManager.GetOption<bool>(GameOption.TechnologyRequiresPower)
#endif
			)
        {
            if(powerRelay is null)
                powerRelay = PowerSource.FindRelay(vehicle.gameObject.transform);

            var amount = ((!state) ? powerConsumptionDeconstruct : powerConsumptionConstruct) * Time.deltaTime;
            var consumed = energyInterface != null ? energyInterface.ConsumeEnergy(amount) : 0f;
            var energyMixinConsumed = energyMixin != null && energyMixin.ConsumeEnergy(amount);

            if(energyMixinConsumed || consumed >= amount || (powerRelay != null && powerRelay.ConsumeEnergy(amount, out consumed)))
            {

                if(!energyMixinConsumed && consumed < amount)
                {
                    if(energyInterface is not null)
                        consumed = energyInterface.AddEnergy(consumed);
                    if(powerRelay is not null)
                        powerRelay.AddEnergy(consumed, out _);
                    yield break;
                }
            }
        }

        var wasConstructed = c.constructed;
        bool flag;
        if(state)
        {
            flag = c.Construct();
        }
        else
        {
            var result = new TaskResult<bool>();
            var reason = new TaskResult<string>();
            yield return c.DeconstructAsync(result, reason);
            flag = result.Get();
        }

        if(!flag && state && !wasConstructed)
            FMODUWE.PlayOneShot(completeSound, c.transform.position, 20f);

    }

    private void OnHover(Constructable constructable)
    {
        if(isToggle)
        {
            var main = HandReticle.main;
            if(constructable.constructed)
            {
                main.SetText(HandReticle.TextType.Hand, this.deconstructText, false);
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(constructText);
                foreach(var keyValuePair in constructable.GetRemainingResources())
                {
                    var key = keyValuePair.Key;
                    var text = Language.main.Get(key);
                    var value = keyValuePair.Value;
                    stringBuilder.AppendLine(value > 1
                        ? Language.main.GetFormat("RequireMultipleFormat", text, value)
                        : text);
                }
                main.SetText(HandReticle.TextType.Hand, stringBuilder.ToString(), false);
                main.SetProgress(constructable.amount);
                main.SetIcon(HandReticle.IconType.Progress, 1.5f);
            }
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private void OnHover(BaseDeconstructable deconstructable)
    {
        if (!isToggle) return;
        var main = HandReticle.main;
        main.SetText(HandReticle.TextType.Hand, this.deconstructText, false);
    }

    protected void OnDestroy()
    {
        OnDisable();
        if(vehicle is not null)
            vehicle.onToggle -= OnToggle;
    }
}