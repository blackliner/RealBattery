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

        // for load balancing
        [KSPField(isPersistant = false)]
        public double SC_SOC = 1;

        [KSPField(isPersistant = false)]
        public bool LoadMaster = true; // true = load master; false = load slave

        [KSPField(isPersistant = false)]
        public int loadMasterCallMode = 0; // 0 = not called; 1 = discharge; 2 = charge;

        // for slowing down the charge/discharge status
        private const double statusLowPassTau = 2;

        // shows current charge (= positive value, means this part is CONSUMING xx EC/s) and discharge (= negative value, means this part is GENERATING xx EC/s) power
        [KSPField(isPersistant = false)]
        public double lastECpower = 0;


        //------GUI

        // Battery cgarge Status string
        [KSPField(isPersistant = false, guiActive = false, guiName = "Dynamic Level")]
        public string EC_dynamicLevelStatus;

        // Battery cgarge Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string BatteryChargeStatus;

        // Battery temp Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temp")]
        public string BatteryTempStatus;

        // Battery tech string for Editor
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Tech")]
        public string BatteryTech;

        // discharge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Discharge Rate", guiUnits = "EC/s")]
        public string DischargeInfoEditor;

        // charge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Charge Rate", guiUnits = "EC/s")]
        public string ChargeInfoEditor;

        // efficiency string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Efficiency", guiUnits = "%")]
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

        private bool hasCoreHeat;

        public override void OnAwake()
        {
            Debug.Log("RealBattery: INF OnAwake");

            loadConfig();

            EC_id = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
            SC_id = PartResourceLibrary.Instance.GetDefinition("StoredCharge").id;

            base.OnAwake();
        }

        public override void OnLoad(ConfigNode node)
        {
            Debug.Log("RealBattery: INF OnLoad");

            loadConfig();

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

            BatteryTech = RealBatteryConfiguration.getBatteryTypesFriendlyName((BatteryTypes)BatteryType);

            PowerDensity = batCfg.PowerDensity;         
            EnergyDensity = batCfg.EnergyDensity;       
            ChargeEfficiency = batCfg.ChargeEfficiency; 
            ChargeRatio = batCfg.ChargeRatio;             
            // take cfg for now
            // HighEClevel = batCfg.HighEClevel;           
            // LowEClevel = batCfg.LowEClevel;
            // ThermalLosses = batCfg.ThermalLosses;
            ChargeEfficiencyCurve = batCfg.ChargeEfficiencyCurve;
            TemperatureCurve = batCfg.TemperatureCurve;

            //finish loading this batteries config

            double DischargeRate = part.mass * PowerDensity; //kW

            hasCoreHeat = false;

            foreach (var module in part.Modules)
            {
                if (module.GetType() == typeof(ModuleCoreHeat))
                {
                    coreHeatModule = (ModuleCoreHeat)module;
                    Debug.Log("RealBattery: INF ModuleCoreHeat FOUND!!!");

                    hasCoreHeat = true;
                }
            }

            if (coreHeatModule != null)
            {
                

                coreHeatModule.CoreTempGoal = batCfg.CoreTempGoal;
                coreHeatModule.MaxCoolant = 2 * DischargeRate * ThermalLosses;
            }


            DischargeInfoEditor = String.Format("{0:F2}", DischargeRate);
            ChargeInfoEditor = String.Format("{0:F2}", DischargeRate * ChargeRatio);
            EfficiencyInfoEditor =String.Format("{0:F0}", 100*ChargeEfficiency);

            PartResource StoredCharge = part.Resources.Get("StoredCharge");
            if (StoredCharge != null)
            {
                StoredCharge.maxAmount = part.mass * EnergyDensity;
                StoredCharge.amount = SC_SOC * part.mass * EnergyDensity;


                // setup ECbufferRatio
                PartResource ElectricCharge = part.Resources.Get("ElectricCharge");
                if (ElectricCharge == null)
                {
                    ElectricCharge = part.Resources.Add("ElectricCharge", 10, 10, true, true, true, true, PartResource.FlowMode.Both);
                }

                ElectricCharge.maxAmount = DischargeRate;
                ElectricCharge.amount = DischargeRate;



                UIPartActionWindow[] partWins = FindObjectsOfType<UIPartActionWindow>();
                foreach (UIPartActionWindow partWin in partWins)
                {
                    partWin.displayDirty = true;
                }
            }
            
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("RealBattery: INF OnStart");

            BatteryChargeStatus = "Initializing";
            BatteryTempStatus = "-";

            readAllRealBatteryModules();

            GameEvents.onVesselChange.Add(readAllRealBatteryModules);
            GameEvents.onVesselStandardModification.Add(readAllRealBatteryModules);

            base.OnStart(state);
        }

        public override string GetInfo()
        {
            Debug.Log("RealBattery: INF GetInfo");

            loadConfig();

            double DischargeRate = part.mass * PowerDensity;            

            return "Type: " + RealBatteryConfiguration.getBatteryTypesFriendlyName((BatteryTypes)BatteryType) + "\n"
                 + String.Format("Discharge Rate: {0:F2} EC/s", DischargeRate) + "\n"
                 + String.Format("Charge Rate: {0:F2} EC/s", DischargeRate * ChargeRatio) + "\n"
                 + String.Format("Efficiency: {0:#%}", ChargeEfficiency);
        }

        private static List<RealBattery> rbList;
        public void readAllRealBatteryModules(Vessel gamEventVessel=null)
        {
            if (vessel == null)
            {
                //nothing to do
                return;
            }

            if (part != null)
            {
                part.force_activate();
            }
            else
            {
                Debug.Log("RealBattery: ERR readAllRealBatteryModules with part == null");
            }
            

            // get a fresh list
            if (rbList == null)
            {
                rbList = new List<RealBattery>();
            }
            else
            {
                rbList.Clear();
            }

            foreach (var parts in vessel.Parts)
            {
                foreach (var module in parts.Modules)
                    {
                    if (module is RealBattery)
                    {
                        rbList.Add(module as RealBattery);
                    }
                }                
            }

        }

        private static double EC_dynamicLevel = 0.5; // % dynamical moving charge/discharge threshold, valid for all batteries (on this vessel?)
        private const double EC_dynamicStep = 0.01; // %, for moving 
        private const double EC_dynamicDelta = 0.05; // % difference between min and max value
        public override void OnFixedUpdate()
        {
            // load balancing part
            if (LoadMaster) //i am a loadmaster
            {
                rbList.ForEach(rb => rb.LoadMaster = false);    // make them all slaves
                LoadMaster = true;                             // ..and i am the master
            }

            if (loadMasterCallMode == 0) // KSP called my OnFixedUpdate, reset LoadMaster
            {
                if (LoadMaster == false) //if i am a slave
                {
                    LoadMaster = true; // try to become a master in the next run
                    return; //only obey the load master
                }
                else if (LoadMaster) // i am the master, and i call everyone to work in a sorted fashion
                {
                    // sort the list by SC_SOC for discharging and run discharge
                    rbList = rbList.OrderByDescending(rb => rb.SC_SOC).ToList();

                    rbList.ForEach(rb => rb.loadMasterCallMode = 1); //set everyone to discharge
                    rbList.ForEach(rb => rb.OnFixedUpdate());

                    //now reverse cowgirl for charging
                    rbList.Reverse();

                    rbList.ForEach(rb => rb.loadMasterCallMode = 2); //set everyone to charge
                    rbList.ForEach(rb => rb.OnFixedUpdate());
                    return; // hard work is done
                }                
            }

            // apparently, a loadmaster called me for doing my job. LETS GO!!!

            


            // normal battery part

            double EC_amount, EC_maxAmount, EC_delta, EC_delta_avail, EC_delta_missing;
            double SC_delta, EC_thermal;
            double EC_power;

            // maximum discharge rate EC/s or kW
            double DischargeRate = part.mass * PowerDensity;

            //thermal stuff
            double currentTemp = hasCoreHeat ? coreHeatModule.CoreTemperature : 0;
            double maxTemp = hasCoreHeat ? coreHeatModule.CoreTempGoal : 0;
            double thermalEff = Math.Min(1, TemperatureCurve.Evaluate((float)currentTemp));
            
            if(hasCoreHeat)
            {
                BatteryTempStatus = String.Format("{0:F1} K / {1:F1} K", currentTemp, maxTemp);
            }
            

            // increase this batteries buffer for warp
            //PartResource EC_buffer = part.Resources.Get("ElectricCharge");
            //EC_buffer.amount = EC_buffer.amount / lastWarpRate * TimeWarp.CurrentRate;
            //EC_buffer.maxAmount = EC_buffer.maxAmount / lastWarpRate * TimeWarp.CurrentRate;
            //lastWarpRate = TimeWarp.CurrentRate;

            // get vessel wide EC status (missing or available)
            part.GetConnectedResourceTotals(EC_id, out EC_amount, out EC_maxAmount);
            if (EC_maxAmount > 0)
            {
                double minVal = (LowEClevel + EC_dynamicDelta / 2);
                double maxVal = (HighEClevel - EC_dynamicDelta / 2);
                EC_dynamicLevel = EC_dynamicLevel < minVal ? minVal : EC_dynamicLevel > maxVal ? maxVal : EC_dynamicLevel;
                //EC_dynamicLevel = EC_dynamicLevel > (HighEClevel - EC_dynamicDelta / 2) ? (HighEClevel - EC_dynamicDelta / 2) : EC_dynamicLevel;

                EC_delta_avail = EC_amount - EC_maxAmount * (EC_dynamicLevel + EC_dynamicDelta / 2);  //amount of available EC for charging: 980 - 1000 * 0.95 = 30EC to spare
                EC_delta_missing = EC_maxAmount * (EC_dynamicLevel - EC_dynamicDelta / 2) - EC_amount;  //amount of missing EC for discharging: 1000 * 0.9 - 500 = 400EC missing
            }
            else
            {
                EC_delta_avail = 0;
                EC_delta_missing = 0;
            }

            // get part EC status
            //double part_EC_amount = part.Resources["ElectricCharge"].amount;
            //double part_EC_maxAmount = part.Resources["ElectricCharge"].maxAmount;

            if (loadMasterCallMode == 2 && EC_delta_avail > 0 && SC_SOC < 1) // Charge battery
            {
                double SOC_ChargeEfficiency = Math.Min(1,ChargeEfficiencyCurve.Evaluate((float)SC_SOC));

                EC_delta = TimeWarp.fixedDeltaTime * thermalEff * DischargeRate * ChargeRatio * SOC_ChargeEfficiency;  // EC_delta = 0.1s * 10EC/s = 1EC
                if (EC_delta > EC_delta_avail) EC_dynamicLevel -= EC_dynamicStep; // if i am limited by the available EC, change the dynamic
                EC_delta = part.RequestResource(EC_id, Math.Min(EC_delta, EC_delta_avail));

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;

                EC_thermal = ThermalLosses * EC_power * EC_power / DischargeRate * TimeWarp.fixedDeltaTime;
                //part.AddThermalFlux(EC_thermal);
                if (hasCoreHeat)
                {
                    coreHeatModule.AddEnergyToCore(50 * EC_thermal); 
                }


                SC_delta = -EC_delta / RealBatteryConfiguration.EC2SCratio * ChargeEfficiency;          // SC_delta = -1EC / 10EC/SC * 0.9 = -0.09SC
                SC_delta = part.RequestResource(SC_id, SC_delta);                              

                //BatteryChargeStatus = String.Format("Charging {0:F2} EC/s", EC_power);
            }
            else if (loadMasterCallMode == 1 && EC_delta_missing > 0 && SC_SOC > 0)  // Discharge battery
            {
                SC_delta = TimeWarp.fixedDeltaTime * thermalEff * DischargeRate / RealBatteryConfiguration.EC2SCratio;      // SC_delta = 0.1s * 1SC/s = 0.1SC
                if (SC_delta > EC_delta_missing / RealBatteryConfiguration.EC2SCratio) EC_dynamicLevel += EC_dynamicStep; // if i am limited by the missing EC, change the dynamic
                SC_delta = part.RequestResource(SC_id, Math.Min(SC_delta, EC_delta_missing / RealBatteryConfiguration.EC2SCratio));                

                EC_delta = -SC_delta * RealBatteryConfiguration.EC2SCratio;         // EC_delta = -0.1SC * 10EC/SC = 1EC
                EC_delta = part.RequestResource(EC_id, EC_delta);

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;

                EC_thermal = ThermalLosses * EC_power * EC_power / DischargeRate * TimeWarp.fixedDeltaTime;
                //part.AddThermalFlux(EC_thermal);
                if (hasCoreHeat)
                {
                    coreHeatModule.AddEnergyToCore(50 * EC_thermal); 
                }

                //BatteryChargeStatus = String.Format("Discharging {0:F1} EC/s", EC_power);
            }
            else
            {
                EC_power = 0;
                //BatteryChargeStatus = String.Format("idle; {0:F1} % EC", EC_amount / EC_maxAmount * 100);
            }

            

            //update SOC field for usage in other modules (load balancing)
            SC_SOC = part.Resources["StoredCharge"].amount / part.Resources["StoredCharge"].maxAmount;


            // GUI
            double statusLowPassTauRatio = TimeWarp.fixedDeltaTime / (statusLowPassTau + TimeWarp.fixedDeltaTime);

            lastECpower = lastECpower * (1 - statusLowPassTauRatio) + 2 * EC_power * statusLowPassTauRatio; // twice, because function is called twice from the master, so there is always a +0 run

            if (lastECpower < -0.001)
            {
                BatteryChargeStatus = String.Format("Discharging {0:F1} EC/s", lastECpower);
            }
            else if (lastECpower > 0.001)
            {
                BatteryChargeStatus = String.Format("Charging {0:F1} EC/s", lastECpower);
            }
            else
            {
                BatteryChargeStatus = String.Format("idle; {0:F1} % EC", EC_amount / EC_maxAmount * 100);
            }

            EC_dynamicLevelStatus = String.Format("{0:F1} % EC", EC_dynamicLevel * 100); 


            loadMasterCallMode = 0; // reset for next run
        }


    }
}
