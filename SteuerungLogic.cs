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
        //für die state machine
        private static readonly object SyncLock = new object();
        private readonly List<StatesTransition> _transitions;


        //aufruf: http://steuerungentfeuchter.prod.j1/triggerresponse
        private static volatile SteuerungLogic _instance;
        private static object _syncRoot = new object();
        private JobManager _jobManager;

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

            //StateMachine init
            CurrentState = State.Aus;
            _transitions = new List<StatesTransition>();

            _transitions.Add(new StatesTransition(State.Aus, Signal.GotoWaitForEntfeuchten, GotoStateWaitForEntfeuchten, State.WaitForEntfeuchten));

            _transitions.Add(new StatesTransition(State.WaitForEntfeuchten, Signal.GotoAus, GotoStateAus, State.Aus));
            _transitions.Add(new StatesTransition(State.WaitForEntfeuchten, Signal.GotoEntfeuchten, GotoStateEntfeuchten, State.Entfeuchten));

            _transitions.Add(new StatesTransition(State.Entfeuchten, Signal.GotoAus, GotoStateAus, State.Aus));
            _transitions.Add(new StatesTransition(State.Entfeuchten, Signal.GotoWaitForEntfeuchten, GotoStateWaitForEntfeuchten, State.WaitForEntfeuchten));
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


        public void ExecuteAction(Signal signal)
        {
            lock (SyncLock)
            {
                foreach (var transition in _transitions)
                {
                    if (transition.GetHashCode() == (CurrentState.ToString().GetHashCode() ^ signal.ToString().GetHashCode()))
                    {
                        CurrentState = transition.TargetState;
                        if (transition.TransitionDelegateMethod != null)
                        {
                            transition.TransitionDelegateMethod();
                        }

                        Console.WriteLine(String.Format("ChangeOfState - Signal: {0}, StartState: {1}, TargetState {2}.", transition.Signal, transition.StartState, transition.TargetState));
                        return;
                    }
                }

                Console.WriteLine(String.Format("WrongTransition - Signal: {0}, CurrentState: {1}.", signal, CurrentState));
            }
        }

        /// <summary>
        /// Gets the state of the current.
        /// </summary>
        public State CurrentState { get; private set; }


        /// <summary>
        /// Gets a value indicating whether [first contactiong done].
        /// </summary>
        public bool FirstContactiongDone { get; private set; }

        public void Start()
        {
            Console.WriteLine("Steuerung starten");
            clusterConn = new IOBrokerClusterConnector();
            KellerSensor = new SensorFeuchtigkeit(62,1,59);
           

            Entfeuchter = new Schalter();

       
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
                clusterConn.SetIOBrokerValue(EntfeuchterZielObject, false);
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
                clusterConn.SetIOBrokerValue(EntfeuchterZielObject, true);
            }
            else
            {
                Console.WriteLine("wert noch nicht über zeitlimit");
                ExecuteAction(Signal.GotoWaitForEntfeuchten);
            }
            //Entfeuchter einschalten
          
            
        }

        


        //public void Update()
        //{
        //    Console.WriteLine("Neue Daten getriggert");
        //    //Daten updaten

        //    IOBrokerJSONGet jsonResultKellerHum = clusterConn.GetIOBrokerValue(KellerHumObject);
        //    IOBrokerJSONGet jsonResultEntfeuchter = clusterConn.GetIOBrokerValue(EntfeuchterIstObject);

        //    if (jsonResultKellerHum == null)
        //    {
        //        return;
        //    }
        //    if (jsonResultEntfeuchter == null)
        //    {
        //        return;
        //    }

        //    KellerSensor.Feuchtigkeit = jsonResultKellerHum.valInt.Value;
        //    Entfeuchter.Status = jsonResultEntfeuchter.valBool.Value;

        //    Console.WriteLine("feuchtigkeit wert / limit: " + KellerSensor.Feuchtigkeit.ToString() + " - " + KellerSensor.LimitHigh.ToString());
        //    Console.WriteLine("aktuelle Zeit / UTC Zeit: " + DateTime.Now.ToString() + " - " + DateTime.UtcNow.ToString());
        //    Console.WriteLine("limit high time: " + KellerSensor.LimitHighTime.ToString());
        //    //feuchtigkeit überprüfen
        //    if (KellerSensor.Feuchtigkeit > KellerSensor.LimitHigh)
        //    {
        //        //&& KellerSensor.LimitHighTime == DateTime.MinValue
        //        Console.WriteLine("feuchtigkeit zu hoch: " + KellerSensor.Feuchtigkeit.ToString());

        //        if (KellerSensor.LimitHighTime < DateTime.Now && KellerSensor.LimitHighTime != DateTime.MinValue)
        //        {
        //            //Entfeuchter einschalten
        //            Console.WriteLine("Entfeuchter einschalten");
        //            clusterConn.SetIOBrokerValue(EntfeuchterZielObject, true);
        //        }
        //        else
        //        {
        //            if (KellerSensor.LimitHighTime == DateTime.MinValue)
        //            {
        //                KellerSensor.LimitHighTime = DateTime.Now.AddHours(KellerSensor.LimitHighDelayHours);
        //            }
        //            Console.WriteLine("Entfeuchter nocht nicht einschalten, wegen Zeitlimit: " + KellerSensor.LimitHighTime.AddHours(KellerSensor.LimitHighDelayHours).ToString());
        //        }
        //    }
        //    else
        //    {
        //        KellerSensor.LimitHighTime = DateTime.MinValue;
        //        if (Entfeuchter.Status == true)
        //        {
        //            Console.WriteLine("Entfeuchter ausschalten");
        //            clusterConn.SetIOBrokerValue(EntfeuchterZielObject, false);
        //        }
        //        else
        //        {
        //            Console.WriteLine("Entfeuchter ist schon aus");
        //        }


        //    }

        //}


        public void Update()
        {
            Console.WriteLine("Neue Daten getriggert");
            //Daten updaten

            IOBrokerJSONGet jsonResultKellerHum = clusterConn.GetIOBrokerValue(KellerHumObject);
            IOBrokerJSONGet jsonResultEntfeuchter = clusterConn.GetIOBrokerValue(EntfeuchterIstObject);

            if (jsonResultKellerHum == null)
            {
                Console.WriteLine("keine Daten jsonResultKellerHum");
                return;
            }
            if (jsonResultEntfeuchter == null)
            {
                Console.WriteLine("keine Daten jsonResultEntfeuchter");
                return;
            }
           
            KellerSensor.Feuchtigkeit = jsonResultKellerHum.valInt.Value;            
            Entfeuchter.Status = jsonResultEntfeuchter.valBool.Value;

            Console.WriteLine("feuchtigkeit wert / limit: " + KellerSensor.Feuchtigkeit.ToString() + " - " + KellerSensor.LimitHigh.ToString());
            Console.WriteLine("aktuelle Zeit / UTC Zeit: " + DateTime.Now.ToString() + " - " + DateTime.UtcNow.ToString());
            Console.WriteLine("limit high time: " + KellerSensor.LimitHighTime.ToString());

            if (Entfeuchter.Status == true && CurrentState != State.Entfeuchten)
            {
                Console.WriteLine("status entfeuchter (laufend) und state machine stimmen nicht.");
                ExecuteAction(Signal.GotoAus);
            }

            Console.WriteLine("Daten holen abgeschlossen");

            //feuchtigkeit überprüfen
            if (KellerSensor.Feuchtigkeit > KellerSensor.LimitHigh )
            {  
                if (CurrentState == State.Aus)
                { 
                    ExecuteAction(Signal.GotoWaitForEntfeuchten);
                }
                else if (CurrentState == State.WaitForEntfeuchten)
                {
                    ExecuteAction(Signal.GotoEntfeuchten);
                }
            }
            else
            {
                ExecuteAction(Signal.GotoAus);     
            }

        }
    }
}
