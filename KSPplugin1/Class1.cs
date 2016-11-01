using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace KSPplugin1
{
    public class Bettery : PartModule
    {

        // Charge Rate, how much storedCharge is taken per call
        [KSPField(isPersistant = false)]
        public float ChargeRate;

        // Amount of Ec per storedCharge
        [KSPField(isPersistant = false)]
        public float ChargeRatio;

        // Only charge if total EC is higher than this, eg 0.95
        [KSPField(isPersistant = false)]
        public float HighEClevel;

        // Only discharge if total EC is lowe than this, eg 0.9
        [KSPField(isPersistant = false)]
        public float LowEClevel;

        // chargin/discharge efficiency
        [KSPField(isPersistant = false)]
        public float ChargeEfficiency;

        // Bettery Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string ChargingStatus;



        public override void OnStart(StartState state)
        {
            //Debug.Log("Bettery: OnStart");
            ChargingStatus = "Initializing";

            part.force_activate();
        }

        public override void OnUpdate()
        {
            //Debug.Log("Bettery: OnUpdate");
            //ChargingStatus = "OnUpdate";            
        }

        public override string GetInfo()
        {
            //Debug.Log("Bettery: GetInfo");
            return String.Format("Maximum Charge Rate: {0:F2}SC/s", ChargeRate) + "\n" + String.Format("Efficiency: {0:F2}%", ChargeEfficiency * 100);
        }

        public override void OnFixedUpdate()
        {
            //Debug.Log("Bettery: OnFixedUpdate");
            double EC_amount, EC_maxAmount, EC_delta, EC_delta_avail, EC_delta_missing;
            double SC_amount, SC_maxAmount, SC_SOC, SC_delta;

            //ChargingStatus = "OnFixedUpdate";

            int EC_id = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
            int SC_id = PartResourceLibrary.Instance.GetDefinition("StoredCharge").id;

            this.part.GetConnectedResourceTotals(EC_id, out EC_amount, out EC_maxAmount);

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

            //Debug.Log("EC_delta_avail: %f" + EC_delta_avail.ToString());
            //Debug.Log("EC_delta_missing: %f" + EC_delta_missing.ToString());

            this.part.GetConnectedResourceTotals(SC_id, out SC_amount, out SC_maxAmount);

            if (SC_maxAmount > 0)
                SC_SOC = SC_amount / SC_maxAmount;
            else
                SC_SOC = 0;

            if (EC_delta_avail > 0 && SC_SOC < 1) // Charge internal Bettery
            {   
                EC_delta = this.part.RequestResource(EC_id, Math.Min(TimeWarp.fixedDeltaTime * ChargeRate * ChargeRatio, EC_delta_avail));    // EC_delta = 0.1s * 1SC/s * 10EC/SC = 1EC

                SC_delta = this.part.RequestResource(SC_id, -EC_delta / ChargeRatio * ChargeEfficiency);            // SC_delta = -1EC / 10EC/SC * 0.9 = -0.09SC

                ChargingStatus = String.Format("Charging");
            }
            else if (EC_delta_missing > 0 && SC_SOC > 0)  // Discharge internal Bettery
            {
                SC_delta = this.part.RequestResource(SC_id, Math.Min(TimeWarp.fixedDeltaTime * ChargeRate, EC_delta_missing / ChargeRatio));                  // SC_delta = 0.1s * 1SC/s = 0.1SC

                EC_delta = this.part.RequestResource(EC_id, -SC_delta * ChargeRatio);                               // EC_delta = -0.1SC * 10EC/SC = 1EC

                ChargingStatus = String.Format("Discharging");
            }
            else
            {
                ChargingStatus = String.Format("idle");
            }
            
        }
    }
}
