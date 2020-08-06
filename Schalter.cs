using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteuerungEntfeuchter
{
    public class Schalter : Objekt
    {
        public bool Status { get; set; }
        public bool ZielStatus { get; set; }
    }
}
