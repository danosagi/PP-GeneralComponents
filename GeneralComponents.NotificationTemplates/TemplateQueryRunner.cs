using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// A column value from the template query row, with the table and column it came from.
    /// </summary>
    public sealed class AliasedColumnValue
    {
        public object Value { get; set; }
        public string FormattedValue { get; set; }
        public string SourceTableName { get; set; }
        public string SourceColumnName { get; set; }
    }

    /// <summary>
    /// The single row returned by a template's FetchXML query, read by column alias without regard to case.
    /// </summary>
    public sealed class TemplateQueryRow
    {
        private readonly Entity queryRow;
        private readonly ISet<string> columnAliases;

        public TemplateQueryRow(Entity queryRow, ISet<string> columnAliases)
        {
            this.queryRow = queryRow;
            this.columnAliases = columnAliases;
        }

        public bool DefinesAlias(string columnAlias)
        {
            return columnAliases.Contains(columnAlias);
        }

        public AliasedColumnValue GetColumnValue(string columnAlias)
        {
            var attributeKey = queryRow.Attributes.Keys.FirstOrDefault(key => string.Equals(key, columnAlias, StringComparison.OrdinalIgnoreCase));
            var formattedValueKey = queryRow.FormattedValues.Keys.FirstOrDefault(key => string.Equals(key, columnAlias, StringComparison.OrdinalIgnoreCase));
            var columnValue = attributeKey == null ? null : queryRow.Attributes[attributeKey];
            var aliasedValue = columnValue as AliasedValue;
            return new AliasedColumnValue
            {
                Value = aliasedValue != null ? aliasedValue.Value : columnValue,
                FormattedValue = formattedValueKey == null ? null : queryRow.FormattedValues[formattedValueKey],
                SourceTableName = aliasedValue != null ? aliasedValue.EntityLogicalName : null,
                SourceColumnName = aliasedValue != null ? aliasedValue.AttributeLogicalName : null
            };
        }
    }

    /// <summary>
    /// Runs a template's queries for one record. The primary query must return exactly one row;
    /// the optional secondary query may return one row or none, in which case its values are blank.
    /// </summary>
    public sealed class TemplateQueryRunner
    {
        private readonly IOrganizationService organizationService;

        public TemplateQueryRunner(IOrganizationService organizationService)
        {
            this.organizationService = organizationService;
        }

        public TemplateQueryRow RunSecondaryQuery(NotificationTemplateDefinition template, Guid recordId)
        {
            var preparedQuery = TemplateQueryPreparer.PrepareSecondary(template.SecondaryFetchXmlQuery, recordId, template.TemplateCode);
            var queryRows = RetrieveQueryRows(preparedQuery, template.TemplateCode, TemplateQueryPreparer.SecondaryQueryLabel);
            if (queryRows.Count > 1)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.MultipleRows, template.TemplateCode,
                    string.Format("found {0} rows with its Secondary FetchXML Query for record {1}. It must return at most one row.", queryRows.Count, recordId));
            }
            return new TemplateQueryRow(queryRows.Count == 1 ? queryRows[0] : new Entity(), preparedQuery.ColumnAliases);
        }

        public TemplateQueryRow RunSingleRowQuery(NotificationTemplateDefinition template, Guid recordId)
        {
            var preparedQuery = TemplateQueryPreparer.Prepare(template.FetchXmlQuery, recordId, template.TargetTable, template.TemplateCode);
            var queryRows = RetrieveQueryRows(preparedQuery, template.TemplateCode, TemplateQueryPreparer.PrimaryQueryLabel);

            if (queryRows.Count == 0)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.RecordNotFound, template.TemplateCode,
                    string.Format("found no row for record {0}. The record doesn't exist, isn't a '{1}' record, or the caller can't read it.", recordId, template.TargetTable));
            }
            if (queryRows.Count > 1)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.MultipleRows, template.TemplateCode,
                    string.Format("found {0} rows for record {1}. The FetchXML Query must return exactly one row, so join only many-to-one relationships.", queryRows.Count, recordId));
            }
            return new TemplateQueryRow(queryRows[0], preparedQuery.ColumnAliases);
        }

        private DataCollection<Entity> RetrieveQueryRows(PreparedTemplateQuery preparedQuery, string templateCode, string queryLabel)
        {
            try
            {
                return organizationService.RetrieveMultiple(new FetchExpression(preparedQuery.FetchXml)).Entities;
            }
            catch (FaultException<OrganizationServiceFault> queryFault)
            {
                var faultMessage = queryFault.Detail != null ? queryFault.Detail.Message : queryFault.Message;
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.QueryInvalid, templateCode,
                    string.Format("has a {0} that Dataverse couldn't run: {1}", queryLabel, faultMessage));
            }
        }
    }
}
