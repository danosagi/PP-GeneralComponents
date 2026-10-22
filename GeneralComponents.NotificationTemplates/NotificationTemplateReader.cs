using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Reads an active notification template and its active parameter rows by Template Code.
    /// </summary>
    public sealed class NotificationTemplateReader
    {
        private readonly IOrganizationService organizationService;

        public NotificationTemplateReader(IOrganizationService organizationService)
        {
            this.organizationService = organizationService;
        }

        public NotificationTemplateDefinition ReadActiveTemplate(string templateCode)
        {
            var requestedTemplateCode = (templateCode ?? string.Empty).Trim();
            if (requestedTemplateCode.Length == 0)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.TemplateNotFound, requestedTemplateCode,
                    "was not found because no Template Code was passed.");
            }

            var templateQuery = new QueryExpression(NotificationTemplateSchema.TemplateTable)
            {
                ColumnSet = new ColumnSet(
                    NotificationTemplateSchema.TemplateCodeColumn,
                    NotificationTemplateSchema.TargetTableColumn,
                    NotificationTemplateSchema.SubjectColumn,
                    NotificationTemplateSchema.BodyColumn,
                    NotificationTemplateSchema.FetchXmlQueryColumn,
                    NotificationTemplateSchema.SecondaryFetchXmlQueryColumn,
                    NotificationTemplateSchema.TimeZoneCodeColumn),
                TopCount = 2
            };
            templateQuery.Criteria.AddCondition(NotificationTemplateSchema.TemplateCodeColumn, ConditionOperator.Equal, requestedTemplateCode);
            templateQuery.Criteria.AddCondition(NotificationTemplateSchema.StateCodeColumn, ConditionOperator.Equal, NotificationTemplateSchema.ActiveStateCode);
            var templateRows = organizationService.RetrieveMultiple(templateQuery).Entities;
            if (templateRows.Count != 1)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.TemplateNotFound, requestedTemplateCode,
                    "was not found among active notification templates.");
            }

            var templateRow = templateRows[0];
            var template = new NotificationTemplateDefinition
            {
                TemplateId = templateRow.Id,
                TemplateCode = templateRow.GetAttributeValue<string>(NotificationTemplateSchema.TemplateCodeColumn),
                TargetTable = (templateRow.GetAttributeValue<string>(NotificationTemplateSchema.TargetTableColumn) ?? string.Empty).Trim(),
                Subject = templateRow.GetAttributeValue<string>(NotificationTemplateSchema.SubjectColumn) ?? string.Empty,
                Body = templateRow.GetAttributeValue<string>(NotificationTemplateSchema.BodyColumn) ?? string.Empty,
                FetchXmlQuery = templateRow.GetAttributeValue<string>(NotificationTemplateSchema.FetchXmlQueryColumn),
                SecondaryFetchXmlQuery = templateRow.GetAttributeValue<string>(NotificationTemplateSchema.SecondaryFetchXmlQueryColumn),
                TimeZoneCode = templateRow.GetAttributeValue<int?>(NotificationTemplateSchema.TimeZoneCodeColumn)
            };

            var parameterQuery = new QueryExpression(NotificationTemplateSchema.ParameterTable)
            {
                ColumnSet = new ColumnSet(
                    NotificationTemplateSchema.PlaceholderNameColumn,
                    NotificationTemplateSchema.SourceTypeColumn,
                    NotificationTemplateSchema.SourceNameColumn,
                    NotificationTemplateSchema.ValueFormatColumn,
                    NotificationTemplateSchema.FormatStringColumn,
                    NotificationTemplateSchema.HtmlEncodeColumn,
                    NotificationTemplateSchema.IsRequiredColumn,
                    NotificationTemplateSchema.DefaultValueColumn)
            };
            parameterQuery.Criteria.AddCondition(NotificationTemplateSchema.ParameterTemplateLookupColumn, ConditionOperator.Equal, template.TemplateId);
            parameterQuery.Criteria.AddCondition(NotificationTemplateSchema.StateCodeColumn, ConditionOperator.Equal, NotificationTemplateSchema.ActiveStateCode);
            foreach (var parameterRow in organizationService.RetrieveMultiple(parameterQuery).Entities)
            {
                template.Parameters.Add(new TemplateParameterDefinition
                {
                    PlaceholderName = (parameterRow.GetAttributeValue<string>(NotificationTemplateSchema.PlaceholderNameColumn) ?? string.Empty).Trim(),
                    SourceType = ReadChoice<ParameterSourceType>(parameterRow, NotificationTemplateSchema.SourceTypeColumn),
                    SourceName = (parameterRow.GetAttributeValue<string>(NotificationTemplateSchema.SourceNameColumn) ?? string.Empty).Trim(),
                    ValueFormat = ReadChoice<ParameterValueFormat>(parameterRow, NotificationTemplateSchema.ValueFormatColumn),
                    FormatString = parameterRow.GetAttributeValue<string>(NotificationTemplateSchema.FormatStringColumn),
                    HtmlEncode = parameterRow.GetAttributeValue<bool?>(NotificationTemplateSchema.HtmlEncodeColumn) ?? true,
                    IsRequired = parameterRow.GetAttributeValue<bool?>(NotificationTemplateSchema.IsRequiredColumn) ?? true,
                    DefaultValue = parameterRow.GetAttributeValue<string>(NotificationTemplateSchema.DefaultValueColumn)
                });
            }
            return template;
        }

        private static TChoice? ReadChoice<TChoice>(Entity row, string choiceColumnName) where TChoice : struct
        {
            var storedChoice = row.GetAttributeValue<OptionSetValue>(choiceColumnName);
            if (storedChoice == null || !Enum.IsDefined(typeof(TChoice), storedChoice.Value)) return null;
            return (TChoice)Enum.ToObject(typeof(TChoice), storedChoice.Value);
        }
    }
}
