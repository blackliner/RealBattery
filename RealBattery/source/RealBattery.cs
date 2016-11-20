using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace RealBattery
{
    public class RealBattery : PartModule
    {
        // defines the battery characteristics, e.g. Lead_Acid
        [KSPField(isPersistant = true)]
        public double BatteryType;

        // max possible discharge rate, kW/t = EC/s per t
        [KSPField(isPersistant = false)]
        public float PowerDensity;

        // energy density in kWh/t = SC/t
        [KSPField(isPersistant = false)]
        public float EnergyDensity;

        // Only charge if total EC is higher than this, eg 0.95
        [KSPField(isPersistant = false)]
        public float HighEClevel;

        // Only discharge if total EC is lower than this, eg 0.9
        [KSPField(isPersistant = false)]
        public float LowEClevel;

        // how much of the discarche rate is usable for charging
        [KSPField(isPersistant = false)]
        public float ChargeRatio;

        // charge efficiency, eg 0.90 for a 90% efficiency during charging
        [KSPField(isPersistant = false)]
        public float ChargeEfficiency;

        // thermal losses, how much of the transfered kW goes to heat
        [KSPField(isPersistant = false)]
        public float ThermalLosses;

        // chargin efficiency based on SOC, eg. to slow down charging on a full battery
        [KSPField(isPersistant = false)]
        public FloatCurve ChargeEfficiencyCurve = new FloatCurve();

        // chargin efficiency based temperature, eg. to slow down charging on a hot battery
        [KSPField(isPersistant = false)]
        public FloatCurve TemperatureCurve = new FloatCurve();

        // Battery cgarge Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string BatteryChargeStatus;

        // Battery temp Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temp")]
        public string BatteryTempStatus;

        // Battery tech string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Tech")]
        public string BatteryTech;

        // discharge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Discharge Rate", guiUnits = "EC/s")]
        public string DischargeInfoEditor;

        // charge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Charge Rate", guiUnits = "EC/s")]
        public string ChargeInfoEditor;

        // efficiency string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Efficiency", guiUnits = "%")]
        public string EfficiencyInfoEditor;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next tech", active = true)]
        public void NextTech()
        {
            BatteryType = (int)RealBatteryConfiguration.getNextConfigKey((BatteryTypes)BatteryType);
            
            loadConfig();

            return;
        }


        private ModuleCoreHeat coreHeatModule;

        private static int EC_id, SC_id;
        
        public override void OnLoad(ConfigNode node)
        {
            loadConfig();

            foreach (var module in part.Modules)
            {
                if (module.GetType() == typeof(ModuleCoreHeat))
                {
                    coreHeatModule = (ModuleCoreHeat)module;
                    Debug.Log("RealBattery: ModuleCoreHeat FOUND!!!");
                }
            }
            

            EC_id = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;            
            SC_id = PartResourceLibrary.Instance.GetDefinition("StoredCharge").id;

            base.OnLoad(node);
        }

        private void loadConfig()
        {
            // handle config
            BatteryConfig batCfg = RealBatteryConfiguration.getConfig((BatteryTypes)BatteryType);
            if (!batCfg.isPopulated)
            {
                batCfg = RealBatteryConfiguration.getConfig(BatteryTypes.CFG_lead_acid);
                if (!batCfg.isPopulated)
                {
                    throw new Exception("No default battery config for RealBattery!");
                }
            }

            BatteryTech = RealBatteryConfiguration.getBatteryTypesFriendlyName(batCfg.batteryType);

            PowerDensity = batCfg.PowerDensity;         
            EnergyDensity = batCfg.EnergyDensity;       
            ChargeEfficiency = batCfg.ChargeEfficiency; 
            ChargeRatio = batCfg.ChargeRatio;             
            HighEClevel = batCfg.HighEClevel;           
            LowEClevel = batCfg.LowEClevel;
            ThermalLosses = batCfg.ThermalLosses;
            ChargeEfficiencyCurve = batCfg.ChargeEfficiencyCurve;
            TemperatureCurve = batCfg.TemperatureCurve;

            if (coreHeatModule != null)
            {
                coreHeatModule.CoreTempGoal = batCfg.CoreTempGoal;
            }
            //finish loading this batteries config

            double DischargeRate = part.mass * PowerDensity;

            DischargeInfoEditor = String.Format("{0:F2}", DischargeRate);
            ChargeInfoEditor = String.Format("{0:F2}", DischargeRate * ChargeRatio);
            EfficiencyInfoEditor =String.Format("{0:F0}", 100*ChargeEfficiency);

            PartResource StoredCharge = part.Resources.Get("StoredCharge");
            if (StoredCharge != null)
            {
                StoredCharge.maxAmount = part.mass * EnergyDensity;
                StoredCharge.amount = part.mass * EnergyDensity;

                UIPartActionWindow[] partWins = FindObjectsOfType<UIPartActionWindow>();
                foreach (UIPartActionWindow partWin in partWins)
                {
                    partWin.displayDirty = true;
                }
            }
            
        }

        public override void OnStart(StartState state)
        {
            BatteryChargeStatus = "Initializing";
            BatteryTempStatus = "-1 K / -1 K";

            part.force_activate();

            base.OnStart(state);
        }

        public override string GetInfo()
        {
            //part.mass = (float)(StoredCharge.maxAmount / EnergyDensity);

            double DischargeRate = part.mass * PowerDensity;

            return String.Format("Discharge Rate: {0:F2} EC/s", DischargeRate) + "\n"
                 + String.Format("Charge Rate: {0:F2} EC/s", DischargeRate * ChargeRatio) + "\n"
                 + String.Format("Efficiency: {0:#%}", ChargeEfficiency);
        }
        

        public override void OnFixedUpdate()
        {
            //Debug.Log("Bettery: OnFixedUpdate");
            double EC_amount, EC_maxAmount, EC_delta, EC_delta_avail, EC_delta_missing;
            double SC_SOC, SC_delta, EC_thermal;
            double EC_power;

            // maximum discharge rate EC/s or kW
            double DischargeRate = part.mass * PowerDensity;

            //thermal stuff
            double currentTemp = coreHeatModule.CoreTemperature;
            double maxTemp = coreHeatModule.CoreTempGoal;
            double thermalEff = Math.Min(1, TemperatureCurve.Evaluate((float)currentTemp));
            
            BatteryTempStatus = String.Format("{0:F1} K / {1:F1} K", currentTemp, maxTemp);


            //ChargingStatus = "OnFixedUpdate";                                  

            part.GetConnectedResourceTotals(EC_id, out EC_amount, out EC_maxAmount);

            if (EC_maxAmount > 0)
            {
                EC_delta_avail = EC_amount - EC_maxAmount * HighEClevel;  //amount of available EC for charging: 980 - 1000 * 0.95 = 30EC to spare
                EC_delta_missing = EC_maxAmount * LowEClevel - EC_amount;  //amount of missing EC for discharging: 1000 * 0.9 - 500 = 400EC missing
            }
            else
            {
                EC_delta_avail = 0;
                EC_delta_missing = 0;
            }

            if (part.Resources["StoredCharge"].maxAmount > 0)
                SC_SOC = part.Resources["StoredCharge"].amount / part.Resources["StoredCharge"].maxAmount;
            else
                SC_SOC = 0;

            if (EC_delta_avail > 0 && SC_SOC < 1) // Charge internal Bettery
            {
                double SOC_ChargeEfficiency = Math.Min(1,ChargeEfficiencyCurve.Evaluate((float)SC_SOC));

                EC_delta = TimeWarp.fixedDeltaTime * thermalEff * DischargeRate * ChargeRatio * SOC_ChargeEfficiency;  // EC_delta = 0.1s * 10EC/s = 1EC
                EC_delta = part.RequestResource(EC_id, Math.Min(EC_delta, EC_delta_avail));

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;

                EC_thermal = ThermalLosses * EC_power * EC_power / DischargeRate * TimeWarp.fixedDeltaTime;
                //part.AddThermalFlux(EC_thermal);
                coreHeatModule.AddEnergyToCore(50 * EC_thermal);


                SC_delta = -EC_delta / RealBatteryConfiguration.EC2SCratio * ChargeEfficiency;          // SC_delta = -1EC / 10EC/SC * 0.9 = -0.09SC
                SC_delta = part.RequestResource(SC_id, SC_delta);                              

                BatteryChargeStatus = String.Format("Charging {0:F2} EC/s", EC_delta/ TimeWarp.fixedDeltaTime);
            }
            else if (EC_delta_missing > 0 && SC_SOC > 0)  // Discharge internal Bettery
            {
                SC_delta = TimeWarp.fixedDeltaTime * thermalEff * DischargeRate / RealBatteryConfiguration.EC2SCratio;      // SC_delta = 0.1s * 1SC/s = 0.1SC
                SC_delta = part.RequestResource(SC_id, Math.Min(SC_delta, EC_delta_missing / RealBatteryConfiguration.EC2SCratio));                

                EC_delta = -SC_delta * RealBatteryConfiguration.EC2SCratio;         // EC_delta = -0.1SC * 10EC/SC = 1EC
                EC_delta = part.RequestResource(EC_id, EC_delta);

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;

                EC_thermal = ThermalLosses * EC_power * EC_power / DischargeRate * TimeWarp.fixedDeltaTime;
                //part.AddThermalFlux(EC_thermal);
                coreHeatModule.AddEnergyToCore(50 * EC_thermal);

                BatteryChargeStatus = String.Format("Discharging {0:F1} EC/s", EC_delta / TimeWarp.fixedDeltaTime);
            }
            else
            {
                BatteryChargeStatus = String.Format("idle; {0:F1} % EC", EC_amount / EC_maxAmount * 100);
            }


            

        }


    }
}
