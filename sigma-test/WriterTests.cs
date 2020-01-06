using NUnit.Framework;
using System;
using Sigma;
using System.IO;
using System.Text;
using NodaTime;

namespace SigmaTest
{
    public class Tests
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
        public void TestDates()
        {
            Console.Out.WriteLine("Date tests");
            Assert.AreEqual("@2019-08-21", Write(new LocalDate(2019, 8, 21)));
            Assert.AreEqual("@10:23:56.123456789", Write(LocalTime.FromHourMinuteSecondNanosecond(10, 23, 56, 123456789)));
            Assert.AreEqual("@10:23:56.123Z", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.Zero)));
            Assert.AreEqual("@10:23:56.123+11:00", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.FromHours(11))));
            Assert.AreEqual("@10:23:56.123-11:00", Write(new OffsetTime(LocalTime.FromHourMinuteSecondMillisecondTick(10, 23, 56, 123, 0), Offset.FromHours(-11))));
            Assert.AreEqual("@2019-08-21T10:11:12.123", Write(new LocalDateTime(2019, 08, 21, 10, 11, 12, 123)));
            Assert.AreEqual("@2019-08-21T10:11:12.123+11:30", Write(new OffsetDateTime(new LocalDateTime(2019, 08, 21, 10, 11, 12, 123), Offset.FromHoursAndMinutes(11,30))));
            Assert.AreEqual("@2020-01-21T10:11:12-11:30", Write(new OffsetDateTime(new LocalDateTime(2020, 01, 21, 10, 11, 12), Offset.FromHoursAndMinutes(-11, -30)))); // note unexpected behavior
            Assert.AreEqual("@2019-08-21T10:11:12Z", Write(new OffsetDateTime(new LocalDateTime(2019, 08, 21, 10, 11, 12), Offset.Zero)));
            Assert.AreEqual("@2019-12-21T10:11:12.123+11:00[Australia/Hobart]", Write(
                new ZonedDateTime(
                    new LocalDateTime(2019, 12, 21, 10, 11, 12, 123),
                    DateTimeZoneProviders.Tzdb.GetZoneOrNull("Australia/Hobart"),
                    Offset.FromHours(11))));
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
                Assert.IsTrue(ex.Message.Contains(msgFragment));
            }
        }
    }
}