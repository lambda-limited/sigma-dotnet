using NUnit.Framework;
using System;
using System.Text;
using Sigma;
using System.IO;
using NodaTime;
using System.Collections;
using System.Collections.Generic;

namespace SigmaTest

{
    class ReaderTests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void TestConstants()
        {
            Console.Out.WriteLine("Constant tests");
            Assert.AreEqual(true, read("&t"));
            Assert.AreEqual(false, read("&f"));
            Assert.IsNull(read("&n"));
            ReadErr("&x", "invalid constant");
        }

        [Test]
        public void TestBytes()
        {
            Console.Out.WriteLine("byte[] tests");
            byte[] bytes = new byte[512];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)(i & 0xFF);
            }
            Object result = WriteRead(bytes);
            Assert.IsTrue(result is byte[]);
            Assert.AreEqual(bytes, result);
        }

        [Test]
        public void TestBase64()
        {
            Console.Out.WriteLine("Base 64 tests");
            Object o = read("*VGhlIHF1aWNrIGJyb3duIGZveA");
            Assert.IsTrue(o is byte[]);
            Assert.AreEqual("The quick brown fox", Encoding.UTF8.GetString((byte[])o));
            o = read("*");
            Assert.IsTrue(o is byte[]);
            Assert.IsTrue(((byte[])o).Length == 0);
        }

        [Test]
        public void TestDates()
        {
            Console.Out.WriteLine("Date Tests");
            ReadWrite("@2019-01-01", typeof(LocalDate));
            ReadWrite("@12:31:47.7654", typeof(LocalTime));
            ReadWrite("@12:00:00.123456789", typeof(LocalTime));
            ReadErr("@12:00:00.1234567891", "invalid fraction");
            ReadWrite("@12:31:47+11:00", typeof(OffsetTime));
            ReadWrite("@12:31:47-11:00", typeof(OffsetTime));
            ReadWrite("@12:31:47Z", typeof(OffsetTime));
            ReadWrite("@2019-01-01T12:31:47.7654", typeof(LocalDateTime));
            ReadWrite("@2019-01-01T12:31:47.7654-01:00", typeof(OffsetDateTime));
            ReadWrite("@2019-01-01T12:31:47Z", typeof(OffsetDateTime));
            ReadWrite("@2019-01-01T12:31:47.7654+11:00[Australia/Hobart]", typeof(ZonedDateTime));
            ReadErr("@2019-01-01T12:31:47.7654+11:00[Australia/Bogansville]", "invalid time zone");
        }

        [Test]
        public void TestList()
        {
            Console.Out.WriteLine("List Tests");
            Assert.AreEqual("[]", ReadWrite("[]", typeof(ICollection)));
            Assert.AreEqual("[]", ReadWrite(" [ ] ", typeof(ICollection)));
            Assert.AreEqual("[1,2,3,\"test\"]", ReadWrite("[1, 2 ,3, \"test\"]", typeof(ICollection)));
        }

        [Test]
        public void TestMap()
        {
            Console.Out.WriteLine("Map Tests");
            Assert.AreEqual("{}", ReadWrite("{}", typeof(IDictionary)));
            Assert.AreEqual("{}", ReadWrite(" { } ", typeof(IDictionary)));
            Assert.AreEqual("{\"a\"=6}", ReadWrite("{\"a\"= 6}", typeof(IDictionary)));
            Assert.AreEqual("{\"a\"=6,\"b\"=&f,\"c\"=@2019-01-01}", ReadWrite("{\"a\"=6, \"b\"=&f , \"c\"=@2019-01-01}", typeof(IDictionary)));
            Assert.AreEqual("{9=9}", ReadWrite("{9=9}", typeof(IDictionary)));
        }

        [Test]
        public void TestNumbers()
        {
            Console.Out.WriteLine("Number Tests");
            Assert.AreEqual(0, read("0"));
            Assert.AreEqual(123, read("123"));
            Assert.AreEqual(1.2, read("1.2"));
            Assert.AreEqual("-0.000000000000000123", ReadWrite("-0.000000000000000123", typeof(decimal)));
            Assert.AreEqual("-0.000000000000000123", ReadWrite("-1.23e-16", typeof(decimal)));
            Assert.AreEqual("12300000000000000", ReadWrite("+1.23E+16", typeof(decimal)));
            ReadErr("+1.23E+234", "invalid number");
            ReadErr("+1.23e+234", "invalid number");
        }



        private Object read(string s)
        {
            Reader r = new Reader(s);
            return r.Read();
        }

        private void ReadErr(string s, string msgFragment)
        {
            Reader r = new Reader(s);
            try
            {
                r.Read();
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("expected error message containing '{0}' but got '{1}'", msgFragment, ex.Message);
                Assert.IsTrue(ex.Message.Contains(msgFragment));
            }
        }


        // read the data and then write it back out again
        // this requires the writer tests to have passed
        private string ReadWrite(string r, Type type)
        {
            Object o = read(r);
            Assert.IsTrue(type.IsAssignableFrom(o.GetType()));
            String w = Write(o);
            return w;
        }

        // write the data and then read it back in again.
        // this requires the writer tests to have passed    
        private Object WriteRead(Object value)
        {
            MemoryStream outStream = new MemoryStream();
            Writer w = new Writer(outStream);
            w.Write(value);
            MemoryStream inStream = new MemoryStream(outStream.ToArray());
            Reader r = new Reader(inStream);
            return r.Read();
        }

        private string Write(Object value)
        {
            MemoryStream stream = new MemoryStream();
            Writer w = new Writer(stream);
            w.Write(value);
            string result = Encoding.UTF8.GetString(stream.ToArray());
            return result;
        }


    }
}
