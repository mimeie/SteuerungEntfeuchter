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
        public StateMachineLogic StateMachine;
        
        //aufruf: http://steuerungentfeuchter.prod.j1/triggerresponse
        private static volatile SteuerungLogic _instance;
        private static object _syncRoot = new object();
        private JobManager _jobManager;

        //        http://localhost:60502/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity
        //        http://iobrokerdatacollector.prod-system.192.168.2.114.xip.io/api/iobroker/zwave2.0.Node_003.Multilevel_Sensor.humidity

               


        public SensorFeuchtigkeit KellerSensor;
        public Schalter Entfeuchter;

        

        private SteuerungLogic()
        {
            StateMachine = new StateMachineLogic();

            //StateMachine init
            StateMachine.CurrentState = State.Aus;


            StateMachine._transitions.Add(new StatesTransition(State.Aus, Signal.GotoWaitForEntfeuchten, GotoStateWaitForEntfeuchten, State.WaitForEntfeuchten));

            StateMachine._transitions.Add(new StatesTransition(State.WaitForEntfeuchten, Signal.GotoAus, GotoStateAus, State.Aus));
            StateMachine._transitions.Add(new StatesTransition(State.WaitForEntfeuchten, Signal.GotoEntfeuchten, GotoStateEntfeuchten, State.Entfeuchten));

            StateMachine._transitions.Add(new StatesTransition(State.Entfeuchten, Signal.GotoAus, GotoStateAus, State.Aus));
            StateMachine._transitions.Add(new StatesTransition(State.Entfeuchten, Signal.GotoWaitForEntfeuchten, GotoStateWaitForEntfeuchten, State.WaitForEntfeuchten));

           

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
            Console.WriteLine("Steuerung starten");
            
            KellerSensor = new SensorFeuchtigkeit("zwave2.0.Node_003.Multilevel_Sensor.humidity",59,1,57);
           

            Entfeuchter = new Schalter("zwave2.0.Node_031.Binary_Switch.currentValue", "zwave2.0.Node_031.Binary_Switch.targetValue");

       
            Console.WriteLine("JobManager initialisieren");
            _jobManager = new JobManager();
            _jobManager.Initialize();
            Console.WriteLine("JobManager wurde initialisiert");
            Console.WriteLine("Steuerung gestartet");

        }

        public void Stop()
        {
        }

       
        private void GotoStateAus()
        {
            Console.WriteLine("Executed: GotoStateAus");
            KellerSensor.LimitHighTime = DateTime.MinValue;
            if (Entfeuchter.Status == true)
            {
                Entfeuchter.ZielStatus=true;                
            }
            else
            {
                Console.WriteLine("Entfeuchter ist schon aus");
            }            
        }
        private void GotoStateWaitForEntfeuchten()
        {
            Console.WriteLine("Executed: GotoStateWaitForEntfeuchten");

            if (KellerSensor.LimitHighTime == DateTime.MinValue)
            {
                KellerSensor.LimitHighTime = DateTime.Now.AddHours(KellerSensor.LimitHighDelayHours);
            }
            Console.WriteLine("Entfeuchter nocht nicht einschalten, wegen Zeitlimit: " + KellerSensor.LimitHighTime.AddHours(KellerSensor.LimitHighDelayHours).ToString());
        }

        private void GotoStateEntfeuchten()
        {
            Console.WriteLine("Executed: GotoStateEntfeuchten");
            Console.WriteLine("feuchtigkeit zu hoch (versuche zu entfeuchten): " + KellerSensor.Feuchtigkeit.ToString());

            if (KellerSensor.LimitHighTime < DateTime.Now && KellerSensor.LimitHighTime != DateTime.MinValue)
            {
                Console.WriteLine("Entfeuchter einschalten");
                Entfeuchter.ZielStatus=true;                
            }
            else
            {
                Console.WriteLine("wert noch nicht über zeitlimit");
                StateMachine.ExecuteAction(Signal.GotoWaitForEntfeuchten);
            }
            //Entfeuchter einschalten
          
            
        }

        

        public void Update()
        {
            Console.WriteLine("Neue Daten getriggert");
            //Daten updaten
                      
            KellerSensor.Update();
            Entfeuchter.Update();

            Console.WriteLine("feuchtigkeit wert / limit: " + KellerSensor.Feuchtigkeit.ToString() + " - " + KellerSensor.LimitHigh.ToString());
            Console.WriteLine("aktuelle Zeit / UTC Zeit: " + DateTime.Now.ToString() + " - " + DateTime.UtcNow.ToString());
            Console.WriteLine("limit high time: " + KellerSensor.LimitHighTime.ToString());

            if (Entfeuchter.Status == true && StateMachine.CurrentState != State.Entfeuchten)
            {
                Console.WriteLine("status entfeuchter (laufend) und state machine stimmen nicht.");
                StateMachine.ExecuteAction(Signal.GotoAus);
                Entfeuchter.ZielStatus=false; //muss leider manuell gemacht werden da keine transition dafür
            }

            Console.WriteLine("Daten holen abgeschlossen");

            //feuchtigkeit überprüfen
            if (KellerSensor.Feuchtigkeit > KellerSensor.LimitHigh )
            {  
                if (StateMachine.CurrentState == State.Aus)
                {
                    StateMachine.ExecuteAction(Signal.GotoWaitForEntfeuchten);
                }
                else if (StateMachine.CurrentState == State.WaitForEntfeuchten)
                {
                    StateMachine.ExecuteAction(Signal.GotoEntfeuchten);
                }
            }
            else
            {
                StateMachine.ExecuteAction(Signal.GotoAus);     
            }

        }
    }
}
