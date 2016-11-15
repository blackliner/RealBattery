using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealBattery
{
    public struct BatteryConfig
    {
        public bool isPopulated;
        public float PowerDensity;          // kW/t
        public float EnergyDensity;         // kWh/t
        public float ChargeEfficiency;      // %
        public float ChargeRate;            // %
        public float HighEClevel;           // %
        public float LowEClevel;            // %
        public float ThermalLosses;         // %

        public FloatCurve ChargeEfficiencyCurve;
        public FloatCurve TemperatureCurve; // Efficiency
    }

    class RealBatteryConfiguration
    {
        public const string CFG_default = CFG_lead_acid;
        public const string CFG_lead_acid = "Lead_Acid";

        private static Dictionary<string, BatteryConfig> Configurations = new Dictionary<string, BatteryConfig>();

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
            tempCfg.PowerDensity = 200f;
            tempCfg.EnergyDensity = 20f;
            tempCfg.ChargeEfficiency = 0.9f;
            tempCfg.ChargeRate = 0.1f;
            tempCfg.HighEClevel = 0.95f;
            tempCfg.LowEClevel = 0.9f;
            tempCfg.ThermalLosses = 100f;

            tempCfg.ChargeEfficiencyCurve = new FloatCurve();
            tempCfg.ChargeEfficiencyCurve.Add(0, 1, 0, 0);
            tempCfg.ChargeEfficiencyCurve.Add(0.5f, 1, 0, -0.6666666f);
            tempCfg.ChargeEfficiencyCurve.Add(0.8f, 0.8f, -0.6666666f, -1.5f);
            tempCfg.ChargeEfficiencyCurve.Add(1, 0.5f, -1.5f, -1.5f);

            tempCfg.TemperatureCurve = new FloatCurve();
            tempCfg.TemperatureCurve.Add(200, 0.1f);
            tempCfg.TemperatureCurve.Add(250, 0.2f);
            tempCfg.TemperatureCurve.Add(300, 1.0f);
            tempCfg.TemperatureCurve.Add(350, 1.0f);
            tempCfg.TemperatureCurve.Add(400, 0.7f);
            tempCfg.TemperatureCurve.Add(500, 0.0f);

            Configurations.Add(CFG_lead_acid, tempCfg);
            //-------Lead Acid Config END
        }

        public static BatteryConfig getConfig(string configName)
        {
            populateConfig();

            BatteryConfig retCfg = new BatteryConfig();
            
            if (configName == null || !Configurations.ContainsKey(configName))
            {

                retCfg.isPopulated = false;
            }
            else
            {
                retCfg = Configurations[configName];
                retCfg.isPopulated = true;
            }

            return retCfg;
        }

		
    }       
}
