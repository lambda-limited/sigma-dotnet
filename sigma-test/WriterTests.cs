using NUnit.Framework;
using System;
using Sigma;
using System.IO;
using System.Text;
using NodaTime;
using System.Collections.Generic;

namespace SigmaTest
{
    public class WriterTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestBoolean()
        {
            Console.Out.WriteLine("Boolean tests");
            Assert.AreEqual("&f", Write(false));
            Assert.AreEqual("&t", Write(true));
        }

        [Test]
        public void TestBase64()
        {
            Console.Out.WriteLine("Base64 tests");
            Assert.AreEqual("*", Write(Encoding.ASCII.GetBytes(""), false));
            Assert.AreEqual("*VGhlIHF1aWNrIGJyb3duIGZveA", Write(Encoding.ASCII.GetBytes("The quick brown fox"), false));
        }

        [Test]
        public void TestBinary()
        {
            Console.Out.WriteLine("Base64 tests");
            Assert.AreEqual("|3|abc", Write(new byte[] { 0x61, 0x62, 0x63 }));
        }

        [Test]
        public void TestDates()
        {
            Console.Out.WriteLine("Date tests");
            Assert.AreEqual("@2019-08-21", Write(new LocalDate(2019, 8, 21)));
            Assert.AreEqual("@10:23:56.123456789", Write(LocalTime.FromHourMinuteSecondNanosecond(10, 23, 56, 123456789)));
            Assert.AreEqual("@00:00:00", Write(LocalTime.FromHourMinuteSecondNanosecond(0, 0, 0, 0)));
            Assert.AreEqual("@10:23:56.123Z", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.Zero)));
            Assert.AreEqual("@10:23:56.123+11:00", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.FromHours(11))));
            Assert.AreEqual("@10:23:56.123-11:00", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.FromHours(-11))));
            Assert.AreEqual("@2019-08-21T10:11:12.123", Write(new LocalDateTime(2019, 08, 21, 10, 11, 12, 123)));
            Assert.AreEqual("@2019-08-21T00:00:00", Write(new LocalDateTime(2019, 08, 21, 0, 0, 0, 0)));
            Assert.AreEqual("@2019-08-21T10:11:12.123+11:30", Write(new OffsetDateTime(new LocalDateTime(2019, 08, 21, 10, 11, 12, 123), Offset.FromHoursAndMinutes(11, 30))));
            Assert.AreEqual("@2019-08-21T00:00:00+11:30", Write(new OffsetDateTime(new LocalDateTime(2019, 08, 21, 0, 0, 0, 0), Offset.FromHoursAndMinutes(11, 30))));
            Assert.AreEqual("@2020-01-21T10:11:12-11:30", Write(new OffsetDateTime(new LocalDateTime(2020, 01, 21, 10, 11, 12), Offset.FromHoursAndMinutes(-11, -30)))); // note unexpected behavior
            Assert.AreEqual("@2019-08-21T10:11:12Z", Write(new OffsetDateTime(new LocalDateTime(2019, 08, 21, 10, 11, 12), Offset.Zero)));
            Assert.AreEqual("@2019-12-21T10:11:12.123+11:00[Australia/Hobart]", Write(
                new ZonedDateTime(
                    new LocalDateTime(2019, 12, 21, 10, 11, 12, 123),
                    DateTimeZoneProviders.Tzdb.GetZoneOrNull("Australia/Hobart"),
                    Offset.FromHours(11))));
            // native .NET types
            Assert.AreEqual("@2019-08-21T10:11:12.123+11:30", Write(new DateTimeOffset(2019, 08, 21, 10, 11, 12, 123, new TimeSpan(11, 30, 0))));
            Assert.AreEqual("@2019-08-21T10:11:12.123-11:30", Write(new DateTimeOffset(2019, 08, 21, 10, 11, 12, 123, new TimeSpan(-11, -30, 0))));
            Assert.AreEqual("@2019-08-21T10:11:12Z", Write(new DateTimeOffset(2019, 08, 21, 10, 11, 12, TimeSpan.Zero)));
            Assert.AreEqual("@2019-08-21T10:11:12.123", Write(new DateTime(2019, 08, 21, 10, 11, 12, 123)));
        }

        [Test]
        public void TestList()
        {
            Console.Out.WriteLine("List tests");
            List<object> list = new List<object>();
            Assert.AreEqual("[]", Write(list));
            list.Add(123);
            Assert.AreEqual("[123]", Write(list));
            list.Add("test");
            Assert.AreEqual("[123,\"test\"]", Write(list));
            list.Add(new List<object>());
            Assert.AreEqual("[123,\"test\",[]]", Write(list));
        }

        [Test]
        public void TestMap()
        {
            Console.Out.WriteLine("Map tests");
            Dictionary<string, object> map = new Dictionary<string, object>();
            Assert.AreEqual("{}", Write(map));
            map.Add("a", 123);
            Assert.AreEqual("{\"a\"=123}", Write(map));
            map.Add("b", "test");
            Assert.AreEqual("{\"a\"=123,\"b\"=\"test\"}", Write(map));
            map.Add("c d", false);
            Assert.AreEqual("{\"a\"=123,\"b\"=\"test\",\"c d\"=&f}", Write(map));
            map.Add("e=f", null);
            Assert.AreEqual("{\"a\"=123,\"b\"=\"test\",\"c d\"=&f,\"e=f\"=&n}", Write(map));
            Dictionary<object, int> hmap = new Dictionary<object, int>();
            hmap.Add(1, 2);
            hmap.Add("3", 4);
            Assert.AreEqual("{1=2,\"3\"=4}", Write(hmap));
        }


        [Test]
        public void TestNull()
        {
            Console.Out.WriteLine("null test");
            Assert.AreEqual("&n", Write(null));
        }


        [Test]
        public void TestNumbers()
        {
            //    Console.Out.WriteLine("Number tests");
            Assert.AreEqual("0", Write(0));
            Assert.AreEqual("3", Write((byte)3));
            Assert.AreEqual("4", Write((short)4));
            Assert.AreEqual("123456789123", Write(123456789123L));
            Assert.AreEqual("1.234", Write(1.234f));
            Assert.AreEqual("1.234", Write(1.234));
            Assert.AreEqual("123468273648723676000000", Write(1.23468273648723676e23m));
            Assert.AreEqual("1.234682736487236765867", Write(1.234682736487236765867m));
        }

        [Test]
        public void TestObject()
        {
            Console.Out.WriteLine("Object tests");
            Types.UnregisterAll();
            TestModel m = new TestModel();
            WriteErr(m, "No object type registered for class");
            Types.Register(typeof(TestModel), "model");
            string result = Write(m);
            Console.Out.WriteLine(result);
            Assert.AreEqual("model{B=53,Bl=&t,Bytes=|6|abcdef,I=54,L=55,Ld=@2019-03-21,Ldt=@2019-08-22T10:11:12.123,"
                    + "List=[\"abc\"],LocalTime=@10:11:12,Map={\"xyx\"=99,9=9},OffsetTime=@10:11:12+11:20,"
                    + "S=56,Str=\"string\",Tree={2=4,3=9},ZonedDateTime=@2020-01-01T00:00:00+11:00[Australia/Hobart]}",
                    result);

        }
        //
        //model{B=53,Bl=&t,Bytes=|6|abcdef,I=54,L=55,Ld=@2019-03-21,Ldt=@2019-08-22T10:11:12.123,List=["abc"],LocalTime=@10:11:12,Map={"xyx"=99,9=9},OffsetTime=@10:11:12+11:20,S=56,Str="string",Tree={2=4,3=9},ZonedDateTime=@2019-12-21T10:11:12.123+11:00[Australia/Hobart]}

        [Test]
        public void TestString()
        {
            Console.Out.WriteLine("String tests");
            Assert.AreEqual("\"\"", Write(""));
            Assert.AreEqual("\"test\"", Write("test"));
            Assert.AreEqual("\"a\\\"b\"", Write("a\"b"));
            Assert.AreEqual("\"\\\\\\\"\"", Write("\\\""));
            Assert.AreEqual("\"\\r\\n\\t\"", Write("\r\n\t"));
            Assert.AreEqual("\"\\u0000\"", Write("\u0000"));
            Assert.AreEqual("\"\\u0001\"", Write("\u0001"));
            Assert.AreEqual("\"a\"", Write("\u0061"));
            Console.Out.WriteLine("Test UTF-8 encodings");
            TestUtf8CodePoints(0x7f, 0x7FF);
            TestUtf8CodePoints(0x800, 0x9FF);
            TestUtf8CodePoints(0xE000, 0xE0FF);
            TestUtf8CodePoints(0x10000, 0x100FF);
        }



        private void TestUtf8CodePoints(int start, int end)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i <= end; ++i)
            {
                sb.Append(Char.ConvertFromUtf32(i));
            }
            String test = "\"" + sb.ToString() + "\"";
            MemoryStream stream = new MemoryStream(0x80);
            Writer w = new Writer(stream);
            w.Write(sb.ToString());
            String result = Encoding.UTF8.GetString(stream.ToArray());
            Assert.AreEqual(test, result);
        }

        private string Write(object value, bool allowBytes)
        {
            MemoryStream stream = new MemoryStream();
            Writer instance = new Writer(stream);
            instance.Write(value, allowBytes);
            String result = Encoding.UTF8.GetString(stream.ToArray());
            return result;
        }

        private string Write(object value)
        {
            return Write(value, true);
        }

        private void WriteErr(object value, string msgFragment)
        {
            try
            {
                Write(value);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains(msgFragment))
                {
                    Console.Out.WriteLine("Expected '" + msgFragment + "'but got '" + ex.Message + "'");
                    Assert.Fail();
                }
            }
        }
    }
}