using System;
using System.Collections.Generic;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Where a placeholder value comes from. Values match the gen_sourcetype choice.
    /// </summary>
    public enum ParameterSourceType
    {
        QueryColumn = 1,
        EnvironmentVariable = 2,
        CallerInput = 3,
        SystemValue = 4,
        SecondaryQueryColumn = 5
    }

    /// <summary>
    /// How a placeholder value becomes text. Values match the gen_valueformat choice.
    /// </summary>
    public enum ParameterValueFormat
    {
        Raw = 1,
        Formatted = 2,
        Custom = 3
    }

    /// <summary>
    /// An active notification template with its active parameter rows.
    /// </summary>
    public sealed class NotificationTemplateDefinition
    {
        public NotificationTemplateDefinition()
        {
            Parameters = new List<TemplateParameterDefinition>();
        }

        public Guid TemplateId { get; set; }
        public string TemplateCode { get; set; }
        public string TargetTable { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string FetchXmlQuery { get; set; }
        public string SecondaryFetchXmlQuery { get; set; }
        public int? TimeZoneCode { get; set; }
        public IList<TemplateParameterDefinition> Parameters { get; private set; }

        public bool HasFetchXmlQuery
        {
            get { return !string.IsNullOrWhiteSpace(FetchXmlQuery); }
        }

        public bool HasSecondaryFetchXmlQuery
        {
            get { return !string.IsNullOrWhiteSpace(SecondaryFetchXmlQuery); }
        }
    }

    /// <summary>
    /// One placeholder of a template. Source Type and Value Format are empty when the stored choice is missing or unknown.
    /// </summary>
    public sealed class TemplateParameterDefinition
    {
        public string PlaceholderName { get; set; }
        public ParameterSourceType? SourceType { get; set; }
        public string SourceName { get; set; }
        public ParameterValueFormat? ValueFormat { get; set; }
        public string FormatString { get; set; }
        public bool HtmlEncode { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Finished subject and body returned to the caller.
    /// </summary>
    public sealed class RenderedNotification
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public int PlaceholderCount { get; set; }
    }
}
