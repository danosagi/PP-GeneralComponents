using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace GeneralComponents.NotificationTemplates.Tests
{
    [TestClass]
    public class NotificationTemplateRendererTests
    {
        private const string StandardTemplateCode = "Vista.WBSItem.CompletionStandard";
        private const string AppBaseUrl = "https://example.crm.dynamics.com/main.aspx?appid=00000000-0000-0000-0000-000000000001";
        private const string WbsItemQuery =
            "<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='WBSItemName' /><attribute name='dle_wbselementnumber' alias='WBSElementNumber' />" +
            "<attribute name='createdon' alias='CreatedOn' /><attribute name='dle_activewbsitemstatus' alias='ProgressStatus' />" +
            "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>";
        private const string StandardSubject = "SAP WBS creation completed — {{WBSItemName}}";
        private const string StandardBody =
            "<p>The SAP creation process for <strong>{{WBSItemName}}</strong> is complete.</p>\n" +
            "<p>WBS Element Number: <strong>{{WBSElementNumber}}</strong></p>\n" +
            "<p><a href=\"{{AppBaseUrl}}&amp;pagetype=entityrecord&amp;etn=dle_wbsitem&amp;id={{RecordId}}\">Open WBS Item</a></p>";
        private static readonly Guid WbsItemId = new Guid("1bdd8f15-f7bc-f111-aaaf-6045bd0153e8");

        [TestMethod]
        public void Render_InsertsValuesDirectlyWhenHtmlEncodeIsOff()
        {
            var service = NewStandardCompletionService("R&D <Item>", "VST-STD-01", htmlEncode: false);

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("SAP WBS creation completed — R&D <Item>", renderedNotification.Subject);
            Assert.AreEqual(
                "<p>The SAP creation process for <strong>R&D <Item></strong> is complete.</p>\n" +
                "<p>WBS Element Number: <strong>VST-STD-01</strong></p>\n" +
                "<p><a href=\"" + AppBaseUrl + "&amp;pagetype=entityrecord&amp;etn=dle_wbsitem&amp;id=1bdd8f15-f7bc-f111-aaaf-6045bd0153e8\">Open WBS Item</a></p>",
                renderedNotification.Body);
            Assert.AreEqual(4, renderedNotification.PlaceholderCount);
            StringAssert.Contains(service.ExecutedFetchXmlQueries[0], "value='1bdd8f15-f7bc-f111-aaaf-6045bd0153e8'");
        }

        [TestMethod]
        public void Render_EncodesBodyValuesAndFlattensSubjectLineBreaks()
        {
            var service = NewStandardCompletionService("R&D <Item>\nPhase 2", "VST-STD-01", htmlEncode: true);

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("SAP WBS creation completed — R&D <Item> Phase 2", renderedNotification.Subject);
            StringAssert.Contains(renderedNotification.Body, "<strong>R&amp;D &lt;Item&gt;<br>Phase 2</strong>");
        }

        [TestMethod]
        public void Render_KeepsPlaceholderTextInsideValuesLiteral()
        {
            var service = NewStandardCompletionService("{{RecordId}}", "VST-STD-01", htmlEncode: false);

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("SAP WBS creation completed — {{RecordId}}", renderedNotification.Subject);
        }

        [TestMethod]
        public void Render_FailsWhenTemplateCodeIsUnknown()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);

            AssertRenderFails(service, "Vista.WBSItem.Unknown", null, NotificationTemplateErrorCodes.TemplateNotFound);
        }

        [TestMethod]
        public void Render_FailsWhenPlaceholderHasNoParameterRow()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.BodyColumn] = StandardBody + "<p>{{Reason}} {{Approver}}</p>";

            var failure = AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.PlaceholderUnresolved);
            StringAssert.Contains(failure.Message, "Reason, Approver");
        }

        [TestMethod]
        public void Render_FailsWhenRequiredValueIsBlank()
        {
            var service = NewStandardCompletionService("Item", null, htmlEncode: false);

            var failure = AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.RequiredValueMissing);
            StringAssert.Contains(failure.Message, "WBSElementNumber");
        }

        [TestMethod]
        public void Render_UsesDefaultValuesAndLeavesOptionalBlanksEmpty()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.BodyColumn] = "<p>[{{Reason}}] [{{Note}}]</p>";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("Reason", ParameterSourceType.CallerInput, "Reason", defaultValue: "No reason given"));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("Note", ParameterSourceType.CallerInput, "Note", isRequired: false));

            var withoutInputs = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);
            var withInputs = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, "{\"reason\":\"Late <submission>\",\"Note\":\"Checked\"}");

            Assert.AreEqual("<p>[No reason given] []</p>", withoutInputs.Body);
            Assert.AreEqual("<p>[Late &lt;submission&gt;] [Checked]</p>", withInputs.Body);
        }

        [TestMethod]
        public void Render_FailsWhenQueryFindsNoRow()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateQueryRows.Clear();

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.RecordNotFound);
        }

        [TestMethod]
        public void Render_FailsWhenQueryFindsSeveralRows()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateQueryRows.Add(service.TemplateQueryRows[0]);

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.MultipleRows);
        }

        [TestMethod]
        public void Render_RefusesSecretEnvironmentVariables()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.EnvironmentVariableRows.Clear();
            service.EnvironmentVariableRows.Add(TemplateRowBuilder.NewEnvironmentVariableRow("dle_Vista_AppBaseUrl", "secret reference", variableType: 100000005));

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.EnvironmentVariableInvalid);
        }

        [TestMethod]
        public void Render_FallsBackToEnvironmentVariableDefaultValue()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.EnvironmentVariableRows.Clear();
            service.EnvironmentVariableRows.Add(TemplateRowBuilder.NewEnvironmentVariableRow("dle_Vista_AppBaseUrl", null, defaultValue: "https://default.example.com/main.aspx?appid=2"));

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            StringAssert.Contains(renderedNotification.Body, "href=\"https://default.example.com/main.aspx?appid=2&amp;pagetype");
        }

        [TestMethod]
        public void Render_FormatsLabelsAndConvertsUserLocalDatesToTemplateTimeZone()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.TimeZoneCodeColumn] = 4;
            service.TemplateTimeZoneOffset = TimeSpan.FromHours(-7);
            service.TemplateRow[NotificationTemplateSchema.SubjectColumn] = "{{ProgressLabel}} ({{ProgressValue}}) on {{CreatedLocal}}, sent {{SentDate}}";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("ProgressLabel", ParameterSourceType.QueryColumn, "ProgressStatus", ParameterValueFormat.Formatted));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("ProgressValue", ParameterSourceType.QueryColumn, "ProgressStatus"));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("CreatedLocal", ParameterSourceType.QueryColumn, "CreatedOn", ParameterValueFormat.Custom, formatString: "yyyy-MM-dd HH:mm"));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("SentDate", ParameterSourceType.SystemValue, "CurrentDateTime", ParameterValueFormat.Custom, formatString: "dd MMM yyyy"));
            var fixedClock = new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc);

            var renderedNotification = new NotificationTemplateRenderer(service, () => fixedClock).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("Completed (4) on 2026-10-19 20:30, sent 19 Oct 2026", renderedNotification.Subject);
        }

        [TestMethod]
        public void Render_DoesNotConvertDateOnlyColumns()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.TimeZoneCodeColumn] = 4;
            service.TemplateTimeZoneOffset = TimeSpan.FromHours(-7);
            service.DateTimeBehaviorsByColumn["dle_wbsitem.createdon"] = DateTimeBehavior.DateOnly;
            service.TemplateRow[NotificationTemplateSchema.SubjectColumn] = "Created {{CreatedLocal}}";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("CreatedLocal", ParameterSourceType.QueryColumn, "CreatedOn", ParameterValueFormat.Custom, formatString: "yyyy-MM-dd HH:mm"));

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("Created 2026-10-20 03:30", renderedNotification.Subject);
        }

        [TestMethod]
        public void Render_FailsWhenQueryColumnAliasIsNotInQuery()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.SubjectColumn] = "{{OwnerName}}";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("OwnerName", ParameterSourceType.QueryColumn, "OwnerName"));

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.ParameterInvalid);
        }

        [TestMethod]
        public void Render_FailsForUnknownSystemValue()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.SubjectColumn] = "{{Today}}";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("Today", ParameterSourceType.SystemValue, "Today"));

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.ParameterInvalid);
        }

        [TestMethod]
        public void Render_UsesSecondaryQueryValues()
        {
            var service = NewSecondaryQueryService();
            var secondaryRow = new Entity("businessunit", Guid.NewGuid());
            TemplateRowBuilder.AddAliasedColumn(secondaryRow, "RootBusinessUnitName", "businessunit", "name", "Vista Root & Co");
            service.SecondaryQueryRows.Add(secondaryRow);

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("Item for Vista Root & Co", renderedNotification.Subject);
            StringAssert.Contains(renderedNotification.Body, "<p>Vista Root &amp; Co</p>");
            Assert.AreEqual(2, service.ExecutedFetchXmlQueries.Count);
        }

        [TestMethod]
        public void Render_TreatsSecondaryQueryWithoutRowsAsBlankValues()
        {
            var service = NewSecondaryQueryService();

            var renderedNotification = new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual("Item for No business unit", renderedNotification.Subject);
        }

        [TestMethod]
        public void Render_FailsWhenSecondaryQueryFindsSeveralRows()
        {
            var service = NewSecondaryQueryService();
            service.SecondaryQueryRows.Add(new Entity("businessunit", Guid.NewGuid()));
            service.SecondaryQueryRows.Add(new Entity("businessunit", Guid.NewGuid()));

            var failure = AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.MultipleRows);
            StringAssert.Contains(failure.Message, "Secondary FetchXML Query");
        }

        [TestMethod]
        public void Render_FailsWhenSecondaryColumnIsUsedWithoutSecondaryQuery()
        {
            var service = NewSecondaryQueryService();
            service.TemplateRow.Attributes.Remove(NotificationTemplateSchema.SecondaryFetchXmlQueryColumn);

            AssertRenderFails(service, StandardTemplateCode, null, NotificationTemplateErrorCodes.ParameterInvalid);
        }

        [TestMethod]
        public void Render_RunsOnlyThePrimaryQueryWhenSecondaryQueryIsBlank()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.TemplateRow[NotificationTemplateSchema.SecondaryFetchXmlQueryColumn] = "   ";

            new NotificationTemplateRenderer(service).Render(StandardTemplateCode, WbsItemId, null);

            Assert.AreEqual(1, service.ExecutedFetchXmlQueries.Count);
        }

        private static FakeOrganizationService NewSecondaryQueryService()
        {
            var service = NewStandardCompletionService("Item", "VST-STD-01", htmlEncode: false);
            service.SecondaryQueryRootTable = "businessunit";
            service.TemplateRow[NotificationTemplateSchema.SecondaryFetchXmlQueryColumn] =
                "<fetch top='2'><entity name='businessunit'><attribute name='name' alias='RootBusinessUnitName' /><filter><condition attribute='parentbusinessunitid' operator='null' /></filter></entity></fetch>";
            service.TemplateRow[NotificationTemplateSchema.SubjectColumn] = "{{WBSItemName}} for {{BusinessUnit}}";
            service.TemplateRow[NotificationTemplateSchema.BodyColumn] = "<p>{{BusinessUnit}}</p>";
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("BusinessUnit", ParameterSourceType.SecondaryQueryColumn, "RootBusinessUnitName", defaultValue: "No business unit"));
            return service;
        }

        private static NotificationTemplateException AssertRenderFails(FakeOrganizationService service, string templateCode, string inputValuesJson, string expectedErrorCode)
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(
                () => new NotificationTemplateRenderer(service).Render(templateCode, WbsItemId, inputValuesJson));
            Assert.AreEqual(expectedErrorCode, failure.ErrorCode);
            return failure;
        }

        private static FakeOrganizationService NewStandardCompletionService(string wbsItemName, string wbsElementNumber, bool htmlEncode)
        {
            var service = new FakeOrganizationService
            {
                TemplateRow = TemplateRowBuilder.NewTemplateRow(StandardTemplateCode, "dle_wbsitem", StandardSubject, StandardBody, WbsItemQuery)
            };
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("WBSItemName", ParameterSourceType.QueryColumn, "WBSItemName", htmlEncode: htmlEncode));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("WBSElementNumber", ParameterSourceType.QueryColumn, "WBSElementNumber", htmlEncode: htmlEncode));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("AppBaseUrl", ParameterSourceType.EnvironmentVariable, "dle_Vista_AppBaseUrl", htmlEncode: false));
            service.ParameterRows.Add(TemplateRowBuilder.NewParameterRow("RecordId", ParameterSourceType.SystemValue, "RecordId"));
            service.EnvironmentVariableRows.Add(TemplateRowBuilder.NewEnvironmentVariableRow("dle_Vista_AppBaseUrl", AppBaseUrl));

            var wbsItemRow = new Entity("dle_wbsitem", WbsItemId);
            if (wbsItemName != null) TemplateRowBuilder.AddAliasedColumn(wbsItemRow, "WBSItemName", "dle_wbsitem", "dle_name", wbsItemName);
            if (wbsElementNumber != null) TemplateRowBuilder.AddAliasedColumn(wbsItemRow, "WBSElementNumber", "dle_wbsitem", "dle_wbselementnumber", wbsElementNumber);
            TemplateRowBuilder.AddAliasedColumn(wbsItemRow, "CreatedOn", "dle_wbsitem", "createdon", new DateTime(2026, 10, 20, 3, 30, 0, DateTimeKind.Utc), "10/19/2026 8:30 PM");
            TemplateRowBuilder.AddAliasedColumn(wbsItemRow, "ProgressStatus", "dle_wbsitem", "dle_activewbsitemstatus", new OptionSetValue(4), "Completed");
            service.TemplateQueryRows.Add(wbsItemRow);
            return service;
        }
    }
}
