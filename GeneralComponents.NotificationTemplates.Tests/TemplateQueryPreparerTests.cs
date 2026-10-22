using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeneralComponents.NotificationTemplates.Tests
{
    [TestClass]
    public class TemplateQueryPreparerTests
    {
        private static readonly Guid SampleRecordId = new Guid("1bdd8f15-f7bc-f111-aaaf-6045bd0153e8");

        [TestMethod]
        public void Prepare_InsertsRecordIdAndCollectsAliases()
        {
            const string fetchXmlQuery = "<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='WBSItemName' />" +
                "<link-entity name='dle_rootgroup' from='dle_rootgroupid' to='dle_rootgroup'><attribute name='dle_name' alias='RootGroupName' /></link-entity>" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{recordid}}' /></filter></entity></fetch>";

            var preparedQuery = TemplateQueryPreparer.Prepare(fetchXmlQuery, SampleRecordId, "dle_wbsitem", "Test.Code");

            StringAssert.Contains(preparedQuery.FetchXml, "value='1bdd8f15-f7bc-f111-aaaf-6045bd0153e8'");
            Assert.IsTrue(preparedQuery.ColumnAliases.Contains("wbsitemname"));
            Assert.IsTrue(preparedQuery.ColumnAliases.Contains("RootGroupName"));
        }

        [TestMethod]
        public void Prepare_RejectsQueryWithoutRecordId()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='Name' /></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsOtherPlaceholders()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='Name' />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /><condition attribute='dle_name' operator='eq' value='{{Reason}}' /></filter></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsColumnWithoutAlias()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><attribute name='dle_name' />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsRootTableThatDiffersFromTargetTable()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='Name' />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>", "account");
        }

        [TestMethod]
        public void Prepare_RejectsAllAttributes()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><all-attributes />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsDuplicateAlias()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='Name' /><attribute name='dle_wbselementnumber' alias='name' />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsDocumentTypeDefinitions()
        {
            AssertQueryInvalid("<!DOCTYPE fetch [<!ENTITY loop 'x'>]><fetch><entity name='dle_wbsitem'><attribute name='dle_name' alias='Name' />" +
                "<filter><condition attribute='dle_wbsitemid' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void Prepare_RejectsInvalidXml()
        {
            AssertQueryInvalid("<fetch><entity name='dle_wbsitem'><filter><condition value='{{RecordId}}'></fetch>", "dle_wbsitem");
        }

        [TestMethod]
        public void PrepareSecondary_AllowsAnyRootTableWithoutRecordId()
        {
            var preparedQuery = TemplateQueryPreparer.PrepareSecondary(
                "<fetch top='1'><entity name='businessunit'><attribute name='name' alias='RootBusinessUnitName' /><filter><condition attribute='parentbusinessunitid' operator='null' /></filter></entity></fetch>",
                SampleRecordId, "Test.Code");

            Assert.IsTrue(preparedQuery.ColumnAliases.Contains("RootBusinessUnitName"));
        }

        [TestMethod]
        public void PrepareSecondary_InsertsRecordIdWhenUsed()
        {
            var preparedQuery = TemplateQueryPreparer.PrepareSecondary(
                "<fetch><entity name='dle_projectwbsassignment'><attribute name='dle_name' alias='AssignmentName' /><filter><condition attribute='dle_wbsitem' operator='eq' value='{{RecordId}}' /></filter></entity></fetch>",
                SampleRecordId, "Test.Code");

            StringAssert.Contains(preparedQuery.FetchXml, "value='1bdd8f15-f7bc-f111-aaaf-6045bd0153e8'");
        }

        [TestMethod]
        public void PrepareSecondary_RejectsOtherPlaceholdersAndUnaliasedColumns()
        {
            var otherPlaceholderFailure = Assert.ThrowsException<NotificationTemplateException>(() => TemplateQueryPreparer.PrepareSecondary(
                "<fetch><entity name='account'><attribute name='name' alias='Name' /><filter><condition attribute='name' operator='eq' value='{{Reason}}' /></filter></entity></fetch>",
                SampleRecordId, "Test.Code"));
            var unaliasedColumnFailure = Assert.ThrowsException<NotificationTemplateException>(() => TemplateQueryPreparer.PrepareSecondary(
                "<fetch><entity name='account'><attribute name='name' /></entity></fetch>", SampleRecordId, "Test.Code"));

            Assert.AreEqual(NotificationTemplateErrorCodes.QueryInvalid, otherPlaceholderFailure.ErrorCode);
            StringAssert.Contains(otherPlaceholderFailure.Message, "Secondary FetchXML Query");
            Assert.AreEqual(NotificationTemplateErrorCodes.QueryInvalid, unaliasedColumnFailure.ErrorCode);
        }

        private static void AssertQueryInvalid(string fetchXmlQuery, string targetTable)
        {
            var failure = Assert.ThrowsException<NotificationTemplateException>(
                () => TemplateQueryPreparer.Prepare(fetchXmlQuery, SampleRecordId, targetTable, "Test.Code"));
            Assert.AreEqual(NotificationTemplateErrorCodes.QueryInvalid, failure.ErrorCode);
        }
    }
}
