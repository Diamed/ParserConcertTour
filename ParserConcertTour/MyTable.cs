using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserConcertTour
{
    class MyTable
    {
        public string EventDate { get; set; }
        public Place EventPlace { get; set; }
        public string EventName { get; set; }

        public MyTable(string eventDate, string eventPlaceName, string eventPlaceAddress, string eventName)
        {
            EventDate = eventDate;
            EventPlace = new Place(eventPlaceName, eventPlaceAddress);
            EventName = eventName;
        }

        public MyTable(string eventDate, Place eventPlace, string eventName)
        {
            EventDate = eventDate;
            EventPlace = eventPlace;
            EventName = eventName;
        }

        public MyTable(string eventName)
        {
            EventName = eventName;
        }

        public MyTable()
        {

        }
    }

    struct Place
    {
        public string Name { get; set; }
        public string Address { get; set; }

        public Place(string name, string address)
        {
            Name = name;
            Address = address;
        }
    }
}
