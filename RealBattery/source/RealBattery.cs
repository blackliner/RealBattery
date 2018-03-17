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
        public int BatteryType;

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

        // chargin efficiency based on SOC, eg. to slow down charging on a full battery
        [KSPField(isPersistant = false)]
        public FloatCurve ChargeEfficiencyCurve = new FloatCurve();

        // for load balancing
        [KSPField(isPersistant = false)]
        public double SC_SOC = 1;
        
        // shows current charge (= positive value, means this part is CONSUMING xx EC/s) and discharges (= negative value, means this part is GENERATING xx EC/s) power
        [KSPField(isPersistant = false)]
        public double lastECpower = 0;


        //------GUI

        // Battery charge Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string BatteryChargeStatus;

        // Battery tech string for Editor
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Tech")]
        public string BatteryTech;

        // discharge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Discharge Rate", guiUnits = "EC/s")]
        public string DischargeInfoEditor;

        // charge string for Editor
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Charge Rate", guiUnits = "EC/s")]
        public string ChargeInfoEditor;

        // disabled until "simple" techs available
        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Next tech", active = true)]
        public void NextTech()
        {
            BatteryType = (int)RealBatteryConfiguration.getNextConfigKey((BatteryTypes)BatteryType);
            
            LoadConfig();

            return;
        }
        
        private void LoadConfig(ConfigNode node = null)
        {
            // handle config
            BatteryConfig batCfg = RealBatteryConfiguration.GetConfig((BatteryTypes)BatteryType);
            if (!batCfg.isPopulated)
            {
                batCfg = RealBatteryConfiguration.GetConfig(BatteryTypes.CFG_lead_acid);
                if (!batCfg.isPopulated)
                {
                    throw new Exception("No default battery config for RealBattery!");
                }
            }

            BatteryTech = RealBatteryConfiguration.getBatteryTypesFriendlyName((BatteryTypes)BatteryType);

            PowerDensity = batCfg.PowerDensity;         
            EnergyDensity = batCfg.EnergyDensity;
            ChargeEfficiencyCurve = batCfg.ChargeEfficiencyCurve;

            //finish loading this batteries config

            double DischargeRate = part.mass * PowerDensity; //kW

            
            DischargeInfoEditor = String.Format("{0:F2}", DischargeRate);
            ChargeInfoEditor = String.Format("{0:F2}", DischargeRate * ChargeEfficiencyCurve.Evaluate(0f));

            PartResource StoredCharge = part.Resources.Get("StoredCharge");
            if (StoredCharge == null)
            {
                StoredCharge = part.Resources.Add("StoredCharge", 10, 10, true, true, true, true, PartResource.FlowMode.None);
            }

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

        public override void OnStart(StartState state)
        {
            Debug.Log("RealBattery: INF OnStart");

            BatteryChargeStatus = "Initializing";

            LoadConfig();

            base.OnStart(state);
        }

        public override string GetInfo()
        {
            RBlog("RealBattery: INF GetInfo");

            LoadConfig();

            double DischargeRate = part.mass * PowerDensity;            

            return "Type: " + RealBatteryConfiguration.getBatteryTypesFriendlyName((BatteryTypes)BatteryType) + "\n"
                 + String.Format("Discharge Rate: {0:F2} EC/s", DischargeRate) + "\n"
                 + String.Format("Charge Rate: {0:F2} EC/s", DischargeRate * ChargeEfficiencyCurve.Evaluate(0f));
        }

        /*private static List<RealBattery> rbList;
        public void ReadAllRealBatteryModules(Vessel gameEventVessel=null)
        {
            RBlog("RealBattery: INF ReadAllRealBatteryModules");

            if (vessel == null)
            {
                //nothing to do
                RBlog("RealBattery: INF ReadAllRealBatteryModules nothing to do");
                return;
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

            RBlog("RealBattery: INF ReadAllRealBatteryModules populating rbList");

            foreach (var parts in vessel.Parts)
            {
                RBlog("RealBattery: INF ReadAllRealBatteryModules part count = " + vessel.Parts.Count);

                foreach (var module in parts.Modules)
                {
                    RBlog("RealBattery: INF ReadAllRealBatteryModules module count = " + parts.Modules.Count);

                    if (module is RealBattery)
                    {
                        rbList.Add(module as RealBattery);
                    }
                }                
            }

            RBlog("RealBattery: INF rblist entries: " + rbList.Count);

        }*/

        // update context menu
        private double GUI_power = 0;
        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            RBlog("RealBattery: INF OnUpdate");

            // for slowing down the charge/discharge status
            double statusLowPassTauRatio = 0.01;

            GUI_power = GUI_power + statusLowPassTauRatio * (lastECpower - GUI_power);

            // GUI
            if (GUI_power < -0.001)
            {
                BatteryChargeStatus = String.Format("Discharging {0:F1} EC/s", GUI_power);
            }
            else if (GUI_power > 0.001)
            {
                BatteryChargeStatus = String.Format("Charging {0:F1} EC/s", GUI_power);
            }
            else
            {
                part.GetConnectedResourceTotals(PartResourceLibrary.ElectricityHashcode, out double EC_amount, out double EC_maxAmount);
                BatteryChargeStatus = String.Format("idle; {0:F1} % EC", EC_amount / EC_maxAmount * 100);
            }

        }

        private bool doLogDebugStuff = false;
        private void RBlog(string message)
        {
            if (doLogDebugStuff) // just for debugging
            Debug.Log(message);
        }       
        
        // positive means sending EC to the battery, ie charging the battery
        // same for return value: positive value means EC was sent to the battery to charge it
        public double XferECtoRealBattery(double amount) 
        {
            // normal battery part

            double EC_delta = 0;
            double SC_delta = 0;
            double EC_power = 0;

            // maximum discharge rate EC/s or kW
            double DischargeRate = part.mass * PowerDensity;
            

            // get part EC status
            //double part_EC_amount = part.Resources["ElectricCharge"].amount;
            //double part_EC_maxAmount = part.Resources["ElectricCharge"].maxAmount;

            if (amount > 0 && SC_SOC < 1) // Charge battery
            {
                double SOC_ChargeEfficiency = ChargeEfficiencyCurve.Evaluate((float)SC_SOC);
                int SC_id = PartResourceLibrary.Instance.GetDefinition("StoredCharge").id;

                EC_delta = TimeWarp.fixedDeltaTime * DischargeRate * SOC_ChargeEfficiency;  // maximum amount of EC the battery can convert to SC, limited by current charge capacity
                
                EC_delta = part.RequestResource(PartResourceLibrary.ElectricityHashcode, Math.Min(EC_delta, amount));

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;

                SC_delta = -EC_delta / RealBatteryConfiguration.EC2SCratio;          // SC_delta = -1EC / 10EC/SC * 0.9 = -0.09SC
                SC_delta = part.RequestResource(SC_id, SC_delta);   //issue: we might "overfill" the battery and should give back some EC
                               

                RBlog("RealBattery: INF charged");
            }
            else if (amount < 0 && SC_SOC > 0)  // Discharge battery
            {
                int SC_id = PartResourceLibrary.Instance.GetDefinition("StoredCharge").id;

                SC_delta = TimeWarp.fixedDeltaTime * DischargeRate / RealBatteryConfiguration.EC2SCratio;      // maximum amount of SC the battery can convert to EC
                
                SC_delta = part.RequestResource(SC_id, Math.Min(SC_delta, -amount / RealBatteryConfiguration.EC2SCratio)); //requesting SC from storage, so SC_delta will be positive

                EC_delta = -SC_delta * RealBatteryConfiguration.EC2SCratio;         // EC_delta = -0.1SC * 10EC/SC = 1EC
                EC_delta = part.RequestResource(PartResourceLibrary.ElectricityHashcode, EC_delta);

                EC_power = EC_delta / TimeWarp.fixedDeltaTime;


                RBlog("RealBattery: INF discharged");
            }
            else
            {
                EC_power = 0;

                RBlog("RealBattery: INF no charge or discharge");
            }

            //update SOC field for usage in other modules (load balancing)
            SC_SOC = part.Resources["StoredCharge"].amount / part.Resources["StoredCharge"].maxAmount;

            lastECpower = EC_power;

            return EC_delta;
        }


    }
}
