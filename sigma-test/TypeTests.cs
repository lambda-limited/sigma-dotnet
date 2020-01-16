using NUnit.Framework;
using Sigma;
using System;
using System.Collections.Generic;
using System.Text;

namespace SigmaTest
{
    class TypeTests
    {
        [Test]
        public void TestListCoercion()
        {
            List<object> input = new List<object>() { "abc" };
            object result = Types.CoerceType(input, typeof(List<string>));
            if (result is List<string> output)
            {
                Assert.AreEqual(1, output.Count);
                Assert.AreEqual("abc", output[0]);
            }
            else
            {
                Assert.Fail();
            }
            // coerce mixed list to List<string> (should fail)
            try
            {
                input.Add(4);
                result = Types.CoerceType(input, typeof(List<string>));
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("unable to coerce"));
            }

            // coerce mixed list to List<string> (should fall through)
            result = Types.CoerceType(input, typeof(List<object>));
        }

        [Test]
        public void TestMapCoercion()
        {
            Dictionary<object, object> input = new Dictionary<object, object>();
            input.Add(1, "abc");
            object result = Types.CoerceType(input, typeof(Dictionary<int,string>));
            if (result is Dictionary<int, string> output)
            {
                Assert.AreEqual(1, output.Count);
                Assert.AreEqual("abc", output[1]);
            }
            else
            {
                Assert.Fail();
            }
            // coerce mixed list to List<string> (should fail)
            try
            {
                input.Add("2", "def");
                result = Types.CoerceType(input, typeof(Dictionary<int, string>));
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("unable to coerce"));
            }

            // coerce mixed list to List<string> (should fall through)
            result = Types.CoerceType(input, typeof(Dictionary<object, object>));
        }
    }
}
