namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Logical names of the notification template tables and columns in solution NotificationTemplatesAutomations.
    /// </summary>
    public static class NotificationTemplateSchema
    {
        public const string TemplateTable = "gen_notificationtemplate";
        public const string TemplateCodeColumn = "gen_templatecode";
        public const string TargetTableColumn = "gen_targettable";
        public const string SubjectColumn = "gen_subject";
        public const string BodyColumn = "gen_body";
        public const string FetchXmlQueryColumn = "gen_fetchxmlquery";
        public const string SecondaryFetchXmlQueryColumn = "gen_secondaryfetchxmlquery";
        public const string TimeZoneCodeColumn = "gen_timezonecode";

        public const string ParameterTable = "gen_notificationtemplateparameter";
        public const string ParameterTemplateLookupColumn = "gen_notificationtemplateid";
        public const string PlaceholderNameColumn = "gen_placeholdername";
        public const string SourceTypeColumn = "gen_sourcetype";
        public const string SourceNameColumn = "gen_sourcename";
        public const string ValueFormatColumn = "gen_valueformat";
        public const string FormatStringColumn = "gen_formatstring";
        public const string HtmlEncodeColumn = "gen_htmlencode";
        public const string IsRequiredColumn = "gen_isrequired";
        public const string DefaultValueColumn = "gen_defaultvalue";

        public const string StateCodeColumn = "statecode";
        public const int ActiveStateCode = 0;
    }

    /// <summary>
    /// Source Name values accepted for System Value parameters.
    /// </summary>
    public static class SystemValueNames
    {
        public const string RecordId = "RecordId";
        public const string CurrentDateTime = "CurrentDateTime";
    }
}
