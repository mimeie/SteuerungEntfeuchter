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

            IOBrokerJSONGet jsonResultKellerHum = clusterConn.GetIOBrokerValue(KellerHumObject);
            IOBrokerJSONGet jsonResultEntfeuchter = clusterConn.GetIOBrokerValue(EntfeuchterIstObject);

            if (jsonResultKellerHum == null)
            {
                return;
            }
            if (jsonResultEntfeuchter == null)
            {
                return;
            }

            KellerSensor.LastChange = jsonResultKellerHum.LastChange;
            KellerSensor.Feuchtigkeit = jsonResultKellerHum.valInt.Value;
            
            Entfeuchter.Status = jsonResultEntfeuchter.valBool.Value;

            Console.WriteLine("feuchtigkeit wert / limit: " + KellerSensor.Feuchtigkeit.ToString() + " - " + KellerSensor.LimitHigh.ToString());
            Console.WriteLine("aktuelle Zeit / UTC Zeit: " + DateTime.Now.ToString() + " - " + DateTime.UtcNow.ToString());
            Console.WriteLine("limit high time: " + KellerSensor.LimitHighTime.ToString());
            //feuchtigkeit überprüfen
            if (KellerSensor.Feuchtigkeit > KellerSensor.LimitHigh )
            {
                //&& KellerSensor.LimitHighTime == DateTime.MinValue
                Console.WriteLine("feuchtigkeit zu hoch: " + KellerSensor.Feuchtigkeit.ToString());
                              
                if (KellerSensor.LimitHighTime < DateTime.Now && KellerSensor.LimitHighTime != DateTime.MinValue)
                {
                    //Entfeuchter einschalten
                    Console.WriteLine("Entfeuchter einschalten");
                    clusterConn.SetIOBrokerValue(EntfeuchterZielObject, true);
                }
                else
                {
                    if (KellerSensor.LimitHighTime == DateTime.MinValue)
                    { 
                        KellerSensor.LimitHighTime = DateTime.Now.AddHours(KellerSensor.LimitHighDelayHours);
                    }
                    Console.WriteLine("Entfeuchter nocht nicht einschalten, wegen Zeitlimit: " + KellerSensor.LimitHighTime.AddHours(KellerSensor.LimitHighDelayHours).ToString());
                }
            }
            else
            {
                KellerSensor.LimitHighTime = DateTime.MinValue;
                if (Entfeuchter.Status == true)
                {
                    Console.WriteLine("Entfeuchter ausschalten");
                    clusterConn.SetIOBrokerValue(EntfeuchterZielObject, false);
                }
                else
                {
                    Console.WriteLine("Entfeuchter ist schon aus");
                }

                
            }

        }
    }
}
