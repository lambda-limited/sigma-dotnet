using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace SigmaTest
{
    public class TestModel
    {
        public byte B { get; set; }
        public bool Bl { get; set; }
        public byte[] Bytes { get; set; }
        public int I { get; set; }
        public long L { get; set; }
        public LocalDate Ld { get; set; }
        public LocalDateTime Ldt { get; set; }
        public List<object> List { get; set; }

        public LocalTime LocalTime { get; set; }
        public Dictionary<object,object> Map { get; set; }
        public OffsetTime OffsetTime { get; set; }
        public short S { get; set; }
        public string Str { get; set; }
        public SortedDictionary<object, int> Tree { get; set; }
        public ZonedDateTime ZonedDateTime { get; set; }

        public TestModel()
        {
            B = 53;
            Bl = true;
            Bytes = Encoding.ASCII.GetBytes("abcdef");
            I = 54;
            L = 55;
            Ld = new LocalDate(2019, 3, 21);
            Ldt = new LocalDateTime(2019, 8, 22, 10, 11, 12, 123);
            List = new List<object>();
            LocalTime = new LocalTime(10, 11, 12);
            Map = new Dictionary<object, object>();
            OffsetTime = new OffsetTime(new LocalTime(10, 11, 12), Offset.FromHoursAndMinutes(11, 20));
            S = 56;
            Str = "string";
            Tree = new SortedDictionary<object, int>();
            ZonedDateTime = new ZonedDateTime(
                    new LocalDateTime(2020, 01, 01, 0, 0, 0, 0),
                    DateTimeZoneProviders.Tzdb.GetZoneOrNull("Australia/Hobart"),
                    Offset.FromHours(11));
            List.Add("abc");
            Map.Add("xyx", 99);
            Map.Add(9, 9);
            Tree.Add(3, 9);
            Tree.Add(2, 4);
        }
    }
}
