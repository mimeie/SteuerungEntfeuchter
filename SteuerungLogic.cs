using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Net;

using Newtonsoft.Json;
using JusiBase;
//Update-Package

namespace SteuerungEntfeuchter
{
    public sealed class SteuerungLogic
    {
        private static volatile SteuerungLogic _instance;
        private static object _syncRoot = new object();

        //        http://localhost:60502/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity
        //        http://iobrokerdatacollector.prod-system.192.168.2.114.xip.io/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity
        //private static string IOBrokerDataCollectorAddress = "http://iobrokerdatacollector.prod-system.192.168.2.114.xip.io/api/iobroker/";
        private static string KellerHumObject = "zwave2.0.Node_003.Multilevel_Sensor.humidity";

        private static string EntfeuchterIstObject = "zwave2.0.Node_031.Binary_Switch.currentValue";
        private static string EntfeuchterZielObject = "zwave2.0.Node_031.Binary_Switch.targetValue";

        IOBrokerClusterConnector clusterConn;

        public SensorFeuchtigkeit KellerSensor;
        public Schalter Entfeuchter;

        private SteuerungLogic()
         {
            
        }

        public static SteuerungLogic Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new SteuerungLogic();
                        }
                    }
                }

                return _instance;
            }
        }

        public void Start()
        {
            clusterConn = new IOBrokerClusterConnector();
            KellerSensor = new SensorFeuchtigkeit(57,1,55);
            Entfeuchter = new Schalter();
            
        }

        public void Stop()
        {
        }


       

        public void Update()
        {
            Console.WriteLine("Neue Daten getriggert");
            //Daten updaten

            IOBrokerJSONGet jsonResult = clusterConn.GetIOBrokerValue(KellerHumObject);
            KellerSensor.LastChange = jsonResult.LastChange;
            KellerSensor.Feuchtigkeit = jsonResult.valInt.Value;
            

            jsonResult = clusterConn.GetIOBrokerValue(EntfeuchterIstObject);
            Entfeuchter.Status = jsonResult.valBool.Value;

            //feuchtigkeit überprüfen
            if (KellerSensor.Feuchtigkeit > KellerSensor.LimitHigh && KellerSensor.LimitHighTime == DateTime.MinValue)
            {
                Console.WriteLine("feuchtigkeit zu hoch und über zeitlimit");
                KellerSensor.LimitHighTime = DateTime.Now;               
                if (KellerSensor.LimitHighTime.AddHours(KellerSensor.LimitHighDelayHours) > DateTime.Now)
                {
                    //Entfeuchter einschalten
                    Console.WriteLine("Entfeuchter einschalten");
                    clusterConn.SetIOBrokerValue(EntfeuchterZielObject, true);
                }
            }
            else
            {
                KellerSensor.LimitHighTime = DateTime.MinValue;
                clusterConn.SetIOBrokerValue(EntfeuchterZielObject, false);
                Console.WriteLine("Entfeuchter ausschalten");
            }

        }
    }
}
