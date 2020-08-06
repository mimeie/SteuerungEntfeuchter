﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteuerungEntfeuchter
{
    public class SensorFeuchtigkeit: Objekt
    {
        public int Feuchtigkeit { get; set; }
        public int Abschaltlevel { get; set; }
        public int LimitHigh { get; set; }
        public int LimitHighDelayHours { get; set; }
        public DateTime LimitHighTime { get; set; }

        public SensorFeuchtigkeit(int _limitHigh, int _limitHighDelayHours, int _abschaltlevel)
        {
            LimitHigh = _limitHigh;
            LimitHighDelayHours= _limitHighDelayHours;
            Abschaltlevel = _abschaltlevel;
        }

    }

}
