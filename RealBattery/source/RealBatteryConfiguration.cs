using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealBattery
{
    public struct BatteryConfig
    {
        public BatteryTypes batteryType;
        public bool isPopulated;
        public float PowerDensity;          // kW/t
        public float EnergyDensity;         // kWh/t
        public float ChargeEfficiency;      // %
        public float ChargeRatio;            // %
        public float HighEClevel;           // %
        public float LowEClevel;            // %
        public float ThermalLosses;         // %
        public float CoreTempGoal;
        public float ECbufferRatio;

        public FloatCurve ChargeEfficiencyCurve;
        public FloatCurve TemperatureCurve; // Efficiency
    }

    public enum BatteryTypes
    {
        //CFG_default = 1, // former "Lead_Acid"
        CFG_lead_acid = 1, // former "Lead_Acid",
        CFG_lead_acid_singleUse = 2, // former "Lead_Acid",
        CFG_li_ion = 3 //former "Li_Ion";
    }

    class RealBatteryConfiguration
    {
        //not needed since enum
        //public const string CFG_default = CFG_lead_acid;
        //public const string CFG_lead_acid = "Lead_Acid";
        //public const string CFG_li_ion = "Li_Ion";

        // Amount of Ec per storedCharge; 3600 EC = 1SC = 3600kWs = 1kWh
        public const double EC2SCratio = 3600;
               

        public static string getBatteryTypesFriendlyName(BatteryTypes theType)
        {
            switch (theType)
            {
                case BatteryTypes.CFG_lead_acid: return "Lead Acid";
                case BatteryTypes.CFG_lead_acid_singleUse: return "Single use Lead Acid";
                case BatteryTypes.CFG_li_ion: return "Li Ion";
                default: return "unknown";
            }
        }

        private static SortedList<BatteryTypes, BatteryConfig> Configurations = new SortedList<BatteryTypes, BatteryConfig>();

        private static void populateConfig()
        {
            if (Configurations.Count != 0)
            {
                // Already populated
                return;
            }

            Debug.Log("RealBattery: Populating configuration");

            BatteryConfig tempCfg;

            //-------Lead Acid Config START
            tempCfg = new BatteryConfig();
            tempCfg.batteryType = BatteryTypes.CFG_lead_acid;
            tempCfg.PowerDensity = 400f;
            tempCfg.EnergyDensity = 20f;
            tempCfg.ChargeEfficiency = 0.9f;
            tempCfg.ChargeRatio = 0.1f;
            tempCfg.HighEClevel = 0.9f;
            tempCfg.LowEClevel = 0.1f;
            tempCfg.ThermalLosses = 0.1f;
            tempCfg.CoreTempGoal = 320f; //273 + 47°C
            tempCfg.ECbufferRatio = 1.0f;

            tempCfg.ChargeEfficiencyCurve = new FloatCurve();
            tempCfg.ChargeEfficiencyCurve.Add(0.0f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.5f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.8f, 0.8f);
            tempCfg.ChargeEfficiencyCurve.Add(1.0f, 0.5f);

            tempCfg.TemperatureCurve = new FloatCurve();
            tempCfg.TemperatureCurve.Add(0, 0.5f);
            tempCfg.TemperatureCurve.Add(200, 0.5f);
            tempCfg.TemperatureCurve.Add(250, 0.5f);
            tempCfg.TemperatureCurve.Add(300, 1.0f);
            tempCfg.TemperatureCurve.Add(350, 1.0f);
            tempCfg.TemperatureCurve.Add(400, 0.3f);
            tempCfg.TemperatureCurve.Add(500, 0.0f);

            Configurations.Add(tempCfg.batteryType, tempCfg);
            //-------Lead Acid Config END


            //-------Lead Acid Config START
            tempCfg = new BatteryConfig();
            tempCfg.batteryType = BatteryTypes.CFG_lead_acid_singleUse;
            tempCfg.PowerDensity = 400f;
            tempCfg.EnergyDensity = 20f;
            tempCfg.ChargeEfficiency = 0.9f;
            tempCfg.ChargeRatio = 0.0f;
            tempCfg.HighEClevel = 0.9f;
            tempCfg.LowEClevel = 0.1f;
            tempCfg.ThermalLosses = 0.1f;
            tempCfg.CoreTempGoal = 320f; //273 + 47°C
            tempCfg.ECbufferRatio = 1.0f;

            tempCfg.ChargeEfficiencyCurve = new FloatCurve();
            tempCfg.ChargeEfficiencyCurve.Add(0.0f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.5f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.8f, 0.8f);
            tempCfg.ChargeEfficiencyCurve.Add(1.0f, 0.5f);

            tempCfg.TemperatureCurve = new FloatCurve();
            tempCfg.TemperatureCurve.Add(0, 0.5f);
            tempCfg.TemperatureCurve.Add(200, 0.5f);
            tempCfg.TemperatureCurve.Add(250, 0.5f);
            tempCfg.TemperatureCurve.Add(300, 1.0f);
            tempCfg.TemperatureCurve.Add(350, 1.0f);
            tempCfg.TemperatureCurve.Add(400, 0.3f);
            tempCfg.TemperatureCurve.Add(500, 0.0f);

            Configurations.Add(tempCfg.batteryType, tempCfg);
            //-------Lead Acid Config END

            //-------Li Ion Config START
            tempCfg = new BatteryConfig();
            tempCfg.batteryType = BatteryTypes.CFG_li_ion;
            tempCfg.PowerDensity = 1800f;
            tempCfg.EnergyDensity = 180f;
            tempCfg.ChargeEfficiency = 0.95f;
            tempCfg.ChargeRatio = 1f;
            tempCfg.HighEClevel = 0.9f;
            tempCfg.LowEClevel = 0.1f;
            tempCfg.ThermalLosses = 0.1f;
            tempCfg.CoreTempGoal = 340f; //273 + 67°C; 80°C thermal fuse, 140°C Meltdown
            tempCfg.ECbufferRatio = 1.0f;

            tempCfg.ChargeEfficiencyCurve = new FloatCurve();
            tempCfg.ChargeEfficiencyCurve.Add(0.0f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.5f, 1.0f);
            tempCfg.ChargeEfficiencyCurve.Add(0.9f, 0.8f);
            tempCfg.ChargeEfficiencyCurve.Add(1.0f, 0.7f);

            tempCfg.TemperatureCurve = new FloatCurve();
            tempCfg.TemperatureCurve.Add(0, 0.5f);
            tempCfg.TemperatureCurve.Add(200, 0.5f);
            tempCfg.TemperatureCurve.Add(250, 0.5f);
            tempCfg.TemperatureCurve.Add(280, 1.0f);
            tempCfg.TemperatureCurve.Add(340, 1.0f);
            tempCfg.TemperatureCurve.Add(360, 0.7f);
            tempCfg.TemperatureCurve.Add(410, 0.0f);

            Configurations.Add(tempCfg.batteryType, tempCfg);
            //-------Li Ion Config END
        }

        public static BatteryConfig getConfig(BatteryTypes config)
        {
            populateConfig();

            BatteryConfig retCfg = new BatteryConfig();
            
            if (!Configurations.ContainsKey(config))
            {
                retCfg.isPopulated = false;
                Debug.Log("RealBatter: Warning, config is not populated!");
            }
            else
            {
                retCfg = Configurations[config];
                retCfg.isPopulated = true;
            }

            return retCfg;
        }

        public static BatteryTypes getNextConfigKey(BatteryTypes config)
        {
            int nextIdx = Configurations.IndexOfKey(config) + 1;
            if (nextIdx >= Configurations.Count)
            {
                nextIdx = 0;
            }

            return Configurations.Keys[nextIdx];
        }


    }       
}
