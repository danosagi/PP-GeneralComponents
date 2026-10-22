using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeneralComponents.NotificationTemplates.Tests
{
    [TestClass]
    public class CallerInputValuesParserTests
    {
        [TestMethod]
        public void Parse_ReturnsValuesMatchedWithoutRegardToCase()
        {
            var callerInputValues = CallerInputValuesParser.Parse("{\"Reason\":\"Late submission\",\"Note\":null}", "Test.Code");

            Assert.AreEqual("Late submission", callerInputValues["reason"]);
            Assert.IsNull(callerInputValues["Note"]);
        }

        [TestMethod]
        public void Parse_TreatsBlankInputAsNoValues()
        {
            Assert.AreEqual(0, CallerInputValuesParser.Parse("  ", "Test.Code").Count);
            Assert.AreEqual(0, CallerInputValuesParser.Parse(null, "Test.Code").Count);
        }

        [TestMethod]
        public void Parse_RejectsInvalidJson()
        {
            AssertInputValuesInvalid("{\"Reason\":");
        }

        [TestMethod]
        public void Parse_RejectsArrays()
        {
            AssertInputValuesInvalid("[\"Late submission\"]");
        }

        [TestMethod]
        public void Parse_RejectsNamesThatDifferOnlyByCase()
        {
            AssertInputValuesInvalid("{\"Reason\":\"One\",\"reason\":\"Two\"}");
        }

        private static void AssertInputValuesInvalid(string inputValuesJson)
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(() => CallerInputValuesParser.Parse(inputValuesJson, "Test.Code"));
            Assert.AreEqual(NotificationTemplateErrorCodes.InputValuesInvalid, failure.ErrorCode);
        }
    }
}
