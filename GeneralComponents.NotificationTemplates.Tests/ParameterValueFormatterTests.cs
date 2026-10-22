using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;

namespace GeneralComponents.NotificationTemplates.Tests
{
    [TestClass]
    public class ParameterValueFormatterTests
    {
        private static readonly ParameterValueFormatter PacificDaylightFormatter = new ParameterValueFormatter(utcDateTime => utcDateTime.AddHours(-7));

        [TestMethod]
        public void Format_RawWritesStoredValuesInInvariantForm()
        {
            var lookupId = Guid.NewGuid();

            Assert.AreEqual("4", FormatRaw(new OptionSetValue(4)));
            Assert.AreEqual(lookupId.ToString("D"), FormatRaw(new EntityReference("systemuser", lookupId)));
            Assert.AreEqual("true", FormatRaw(true));
            Assert.AreEqual("1234.5", FormatRaw(new Money(1234.5m)));
            Assert.AreEqual("2026-10-19T22:15:00Z", FormatRaw(new DateTime(2026, 10, 19, 22, 15, 0, DateTimeKind.Utc)));
            Assert.AreEqual("2026-10-19", FormatRaw(new DateTime(2026, 10, 19), DateTimeValueBehavior.DateOnly));
        }

        [TestMethod]
        public void Format_FormattedUsesDataverseLabelAndFallsBackToRaw()
        {
            var formattedParameter = NewParameter(ParameterValueFormat.Formatted);

            Assert.AreEqual("Completed", PacificDaylightFormatter.Format(formattedParameter, new SourceValue { RawValue = new OptionSetValue(4), FormattedValue = "Completed" }, "Test.Code"));
            Assert.AreEqual("VST-STD-01", PacificDaylightFormatter.Format(formattedParameter, new SourceValue { RawValue = "VST-STD-01" }, "Test.Code"));
        }

        [TestMethod]
        public void Format_CustomConvertsOnlyUserLocalDatesToTemplateTimeZone()
        {
            var customDateParameter = NewParameter(ParameterValueFormat.Custom, "yyyy-MM-dd HH:mm");
            var storedDateTime = new DateTime(2026, 10, 20, 3, 30, 0, DateTimeKind.Utc);

            Assert.AreEqual("2026-10-19 20:30", PacificDaylightFormatter.Format(customDateParameter,
                new SourceValue { RawValue = storedDateTime, DateTimeBehavior = DateTimeValueBehavior.UserLocal }, "Test.Code"));
            Assert.AreEqual("2026-10-20 03:30", PacificDaylightFormatter.Format(customDateParameter,
                new SourceValue { RawValue = storedDateTime, DateTimeBehavior = DateTimeValueBehavior.DateOnly }, "Test.Code"));
        }

        [TestMethod]
        public void Format_CustomFormatsNumbers()
        {
            Assert.AreEqual("1,234.50", PacificDaylightFormatter.Format(NewParameter(ParameterValueFormat.Custom, "N2"), new SourceValue { RawValue = 1234.5m }, "Test.Code"));
        }

        [TestMethod]
        public void Format_CustomRejectsText()
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(() =>
                PacificDaylightFormatter.Format(NewParameter(ParameterValueFormat.Custom, "N2"), new SourceValue { RawValue = "text" }, "Test.Code"));
            Assert.AreEqual(NotificationTemplateErrorCodes.ParameterInvalid, failure.ErrorCode);
        }

        [TestMethod]
        public void Format_CustomRejectsInvalidFormatString()
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(() =>
                PacificDaylightFormatter.Format(NewParameter(ParameterValueFormat.Custom, "%"), new SourceValue { RawValue = DateTime.UtcNow }, "Test.Code"));
            Assert.AreEqual(NotificationTemplateErrorCodes.ParameterInvalid, failure.ErrorCode);
        }

        [TestMethod]
        public void Format_ReturnsNullForMissingValues()
        {
            Assert.IsNull(PacificDaylightFormatter.Format(NewParameter(ParameterValueFormat.Raw), new SourceValue(), "Test.Code"));
        }

        private static string FormatRaw(object rawValue, DateTimeValueBehavior dateTimeBehavior = DateTimeValueBehavior.UserLocal)
        {
            return PacificDaylightFormatter.Format(NewParameter(ParameterValueFormat.Raw), new SourceValue { RawValue = rawValue, DateTimeBehavior = dateTimeBehavior }, "Test.Code");
        }

        private static TemplateParameterDefinition NewParameter(ParameterValueFormat valueFormat, string formatString = null)
        {
            return new TemplateParameterDefinition
            {
                PlaceholderName = "Value",
                SourceType = ParameterSourceType.QueryColumn,
                SourceName = "Value",
                ValueFormat = valueFormat,
                FormatString = formatString
            };
        }
    }
}
