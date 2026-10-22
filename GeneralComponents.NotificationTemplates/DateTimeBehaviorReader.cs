using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Looks up the date and time behavior of query columns once per render.
    /// </summary>
    public sealed class DateTimeBehaviorReader
    {
        private readonly IOrganizationService organizationService;
        private readonly Dictionary<string, DateTimeValueBehavior> behaviorsByColumn = new Dictionary<string, DateTimeValueBehavior>(StringComparer.OrdinalIgnoreCase);

        public DateTimeBehaviorReader(IOrganizationService organizationService)
        {
            this.organizationService = organizationService;
        }

        public DateTimeValueBehavior ReadBehavior(string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName)) return DateTimeValueBehavior.UserLocal;

            var columnKey = tableName + "." + columnName;
            if (behaviorsByColumn.TryGetValue(columnKey, out var knownBehavior)) return knownBehavior;

            var columnResponse = (RetrieveAttributeResponse)organizationService.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = tableName,
                LogicalName = columnName
            });
            var dateTimeColumn = columnResponse.AttributeMetadata as DateTimeAttributeMetadata;
            var columnBehavior = DateTimeValueBehavior.UserLocal;
            if (dateTimeColumn != null && dateTimeColumn.DateTimeBehavior != null)
            {
                if (dateTimeColumn.DateTimeBehavior.Value == DateTimeBehavior.DateOnly.Value) columnBehavior = DateTimeValueBehavior.DateOnly;
                else if (dateTimeColumn.DateTimeBehavior.Value == DateTimeBehavior.TimeZoneIndependent.Value) columnBehavior = DateTimeValueBehavior.TimeZoneIndependent;
            }
            behaviorsByColumn[columnKey] = columnBehavior;
            return columnBehavior;
        }
    }
}
