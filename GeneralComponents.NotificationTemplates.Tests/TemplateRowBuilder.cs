using System;
using Microsoft.Xrm.Sdk;

namespace GeneralComponents.NotificationTemplates.Tests
{
    /// <summary>
    /// Builds template, parameter and query rows shaped like Dataverse returns them.
    /// </summary>
    internal static class TemplateRowBuilder
    {
        public static Entity NewTemplateRow(string templateCode, string targetTable, string subject, string body, string fetchXmlQuery, int? timeZoneCode = null)
        {
            var templateRow = new Entity(NotificationTemplateSchema.TemplateTable, Guid.NewGuid());
            templateRow[NotificationTemplateSchema.TemplateCodeColumn] = templateCode;
            templateRow[NotificationTemplateSchema.TargetTableColumn] = targetTable;
            templateRow[NotificationTemplateSchema.SubjectColumn] = subject;
            templateRow[NotificationTemplateSchema.BodyColumn] = body;
            if (fetchXmlQuery != null) templateRow[NotificationTemplateSchema.FetchXmlQueryColumn] = fetchXmlQuery;
            if (timeZoneCode.HasValue) templateRow[NotificationTemplateSchema.TimeZoneCodeColumn] = timeZoneCode.Value;
            return templateRow;
        }

        public static Entity NewParameterRow(
            string placeholderName,
            ParameterSourceType sourceType,
            string sourceName,
            ParameterValueFormat valueFormat = ParameterValueFormat.Raw,
            bool htmlEncode = true,
            bool isRequired = true,
            string defaultValue = null,
            string formatString = null)
        {
            var parameterRow = new Entity(NotificationTemplateSchema.ParameterTable, Guid.NewGuid());
            parameterRow[NotificationTemplateSchema.PlaceholderNameColumn] = placeholderName;
            parameterRow[NotificationTemplateSchema.SourceTypeColumn] = new OptionSetValue((int)sourceType);
            parameterRow[NotificationTemplateSchema.SourceNameColumn] = sourceName;
            parameterRow[NotificationTemplateSchema.ValueFormatColumn] = new OptionSetValue((int)valueFormat);
            parameterRow[NotificationTemplateSchema.HtmlEncodeColumn] = htmlEncode;
            parameterRow[NotificationTemplateSchema.IsRequiredColumn] = isRequired;
            if (defaultValue != null) parameterRow[NotificationTemplateSchema.DefaultValueColumn] = defaultValue;
            if (formatString != null) parameterRow[NotificationTemplateSchema.FormatStringColumn] = formatString;
            return parameterRow;
        }

        public static void AddAliasedColumn(Entity queryRow, string columnAlias, string sourceTable, string sourceColumn, object value, string formattedValue = null)
        {
            queryRow[columnAlias] = new AliasedValue(sourceTable, sourceColumn, value);
            if (formattedValue != null) queryRow.FormattedValues[columnAlias] = formattedValue;
        }

        public static Entity NewEnvironmentVariableRow(string schemaName, string currentValue, string defaultValue = null, int variableType = 100000000)
        {
            var definitionRow = new Entity("environmentvariabledefinition", Guid.NewGuid());
            definitionRow["schemaname"] = schemaName;
            definitionRow["type"] = new OptionSetValue(variableType);
            if (defaultValue != null) definitionRow["defaultvalue"] = defaultValue;
            if (currentValue != null) definitionRow["currentvalue.value"] = new AliasedValue("environmentvariablevalue", "value", currentValue);
            return definitionRow;
        }
    }
}
