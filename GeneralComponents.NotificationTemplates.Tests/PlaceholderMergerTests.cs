using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeneralComponents.NotificationTemplates.Tests
{
    [TestClass]
    public class PlaceholderMergerTests
    {
        [TestMethod]
        public void FindPlaceholderNames_ReturnsEachNameOnceIgnoringCase()
        {
            var placeholderNames = PlaceholderMerger.FindPlaceholderNames("{{ItemName}} and {{itemname}} for {{RecordId}}");

            CollectionAssert.AreEqual(new[] { "ItemName", "RecordId" }, placeholderNames.ToArray());
        }

        [TestMethod]
        public void Merge_DoesNotRescanInsertedText()
        {
            var mergedText = PlaceholderMerger.Merge("Hello {{Name}}", placeholderName => "{{Name}} $1");

            Assert.AreEqual("Hello {{Name}} $1", mergedText);
        }

        [TestMethod]
        public void EnsureWellFormed_RejectsPlaceholderSplitByFormatting()
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(
                () => PlaceholderMerger.EnsureWellFormed("<p>{{WBS<b>ItemName</b>}}</p>", "Test.Code", "Body"));

            Assert.AreEqual(NotificationTemplateErrorCodes.PlaceholderMalformed, failure.ErrorCode);
            StringAssert.Contains(failure.Message, "Body");
        }

        [TestMethod]
        public void EnsureWellFormed_RejectsSpacesInsideBraces()
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(
                () => PlaceholderMerger.EnsureWellFormed("Dear {{ Name }}", "Test.Code", "Subject"));

            Assert.AreEqual(NotificationTemplateErrorCodes.PlaceholderMalformed, failure.ErrorCode);
        }

        [TestMethod]
        public void EnsureWellFormed_AllowsClosingBracesInStyleBlocks()
        {
            PlaceholderMerger.EnsureWellFormed("<style>@media (max-width:600px){p{margin:0}}</style><p>{{Name}}</p>", "Test.Code", "Body");
        }
    }
}
