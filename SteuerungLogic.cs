using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Net;

using Newtonsoft.Json;
using JusiJSONHelper;
//Update-Package

namespace SteuerungEntfeuchter
{
    public class SteuerungLogic
    {
        //        http://localhost:60502/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity
        //        http://iobrokerdatacollector.prod-system.192.168.2.114.xip.io/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity
        private static string IOBrokerDataCollectorAddress = "http://iobrokerdatacollector.prod-system.192.168.2.114.xip.io/api/iobroker/";
        private static string KellerHumObject = "zwave2.0.Node_003.Multilevel_Sensor.humidity";

        public SensorFeuchtigkeit KellerSensor;

        public SteuerungLogic()
         {
            KellerSensor =  new SensorFeuchtigkeit();
            IOBrokerJSONGet jsonResult = GetIOBrokerValue(KellerHumObject);
            KellerSensor.LastChange = jsonResult.LastChange;
            KellerSensor.Feuchtigkeit = jsonResult.valInt;
        }
      

       private IOBrokerJSONGet GetIOBrokerValue(string objectName)
        {
            using (WebClient wc = new WebClient())
            {
                IOBrokerJSONGet ioJson = new IOBrokerJSONGet();

                string downString = IOBrokerDataCollectorAddress + KellerHumObject;
                Console.WriteLine("Download String '{0}'", downString);

                var json = wc.DownloadString(downString);
                ioJson = JsonConvert.DeserializeObject<IOBrokerJSONGet>(json);
                return ioJson;
            }
        }
       

        public void Run()
        {
            //feuchtigkeit überprüfen
            if (KellerSensor.Feuchtigkeit > 57)
            {

            }
            else
            {

            }

        }
    }
}
