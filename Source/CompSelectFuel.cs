﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HugsLib;
using HugsLib.Settings;
using static BurnItForFuel.ModBaseBurnItForFuel;

namespace BurnItForFuel
{
    [StaticConstructorOnStartup]
    public class CompSelectFuel : ThingComp, IStoreSettingsParent
    {
        public StorageSettings fuelSettings;

        public CompProperties_SelectFuel Props
        {
            get
            {
                return (CompProperties_SelectFuel)props;
            }
        }
        public bool StorageTabVisible
        {
            get
            {
                return MultipleFuelSet();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in StorageSettingsClipboard.CopyPasteGizmosFor(fuelSettings))
            {
                yield return g;
            }
            yield break;
        }

        public StorageSettings GetParentStoreSettings()
        {
            StorageSettings settings = new StorageSettings();
            ModSettingsPack pack = HugsLibController.SettingsManager.GetModSettings("JPT_BurnItForFuel");
            ThingFilter fuelSettings = pack.GetHandle<FuelSettingsHandle>("FuelSettings").Value.masterFuelSettings;
            settings.filter = fuelSettings;
            return settings;
        }

        public StorageSettings GetStoreSettings()
        {
            return fuelSettings;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            fuelSettings = new StorageSettings(this);
            if (BaseFuelSettings(parent) != null)
            {
                if ((!StorageSettingsIncludeBaseFuel() || IsVehicle()) && !parent.def.GetCompProperties<CompProperties_Refuelable>().atomicFueling)
                {
                    if (!StorageSettingsIncludeBaseFuel()) Log.Message("[BurnItForFuel] " + BaseFuelSettings(parent).ToString() + " was missing from the " + parent + " storage settings, so those were overriden. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    if (IsVehicle()) Log.Message("[BurnItForFuel] " + parent + " looks like its a vehicle, so we're preventing fuel mixing to protect your engines. Add <atomicFueling>true</atomicFueling> to its CompProperties_Refuelable to prevent this.");
                    GetParentStoreSettings().filter.SetAllowAll(BaseFuelSettings(parent));
                }
                foreach (ThingDef thingDef in  GetParentStoreSettings().filter.AllowedThingDefs)
                {
                    fuelSettings.filter.SetAllow(thingDef, true);
                }
            }
            if (SafeToMixFuels())
            {
                if (parent.TryGetComp<CompRefuelable>() != null)
                {
                    parent.TryGetComp<CompRefuelable>().Props.atomicFueling = true;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<StorageSettings>(ref fuelSettings, "fuelSettings");
            if (fuelSettings == null)
            {
                SetUpStorageSettings();
            }
        }

        public void SetUpStorageSettings()
        {
            if (GetParentStoreSettings() != null)
            {
                fuelSettings = new StorageSettings(this);
                fuelSettings.CopyFrom(GetParentStoreSettings());
            }
        }

        public bool StorageSettingsIncludeBaseFuel() //e.g: Dubs Hygiene Burning Pit doesn't. 
        {
            bool flag = false;
            foreach (ThingDef thingDef in BaseFuelSettings(parent).AllowedThingDefs)
            {
                if (GetParentStoreSettings().AllowedToAccept(thingDef))
                {
                    if (!flag) { flag = true; }
                }
            }
            return flag;
        }

        private static ThingFilter BaseFuelSettings(ThingWithComps T)
        {
            if (T.def.comps != null)
            {
                for (int i = 0; i < T.def.comps.Count; i++)
                {
                    if (T.def.comps[i].compClass == typeof(CompRefuelable))
                    {
                        CompProperties_Refuelable comp = (CompProperties_Refuelable)T.def.comps[i];
                        return comp.fuelFilter;
                    }
                }
            }
            return null;
        }
        private bool IsVehicle()
        {
            CompProperties_Refuelable props = parent.TryGetComp<CompRefuelable>().Props;
            return props.targetFuelLevelConfigurable && props.consumeFuelOnlyWhenUsed;
        }

        private bool MultipleFuelSet()
        {
            ICollection<ThingDef> filter = GetParentStoreSettings().filter.AllowedThingDefs as ICollection<ThingDef>;
            return filter.Count() > 1;
        }

        private bool SafeToMixFuels()
        {
            bool flag = false;
            if (MultipleFuelSet() && parent.def.passability != Traversability.Impassable && !parent.def.building.canPlaceOverWall && !IsVehicle()) flag = true;
            return flag;
        }
    }
}