using System;

namespace SteuerungEntfeuchter
{
    public class TriggerResponse
    {
        public int ReturnCode { get; set; }

        public DateTime Date { get; set; }
       
        public string Host { get; set; }
    }
}
