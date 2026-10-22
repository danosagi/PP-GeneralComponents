using System;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Error codes that start every rendering failure message.
    /// </summary>
    public static class NotificationTemplateErrorCodes
    {
        public const string TemplateNotFound = "NotificationTemplateNotFound";
        public const string QueryInvalid = "NotificationTemplateQueryInvalid";
        public const string RecordNotFound = "NotificationTemplateRecordNotFound";
        public const string MultipleRows = "NotificationTemplateMultipleRows";
        public const string PlaceholderUnresolved = "NotificationTemplatePlaceholderUnresolved";
        public const string PlaceholderMalformed = "NotificationTemplatePlaceholderMalformed";
        public const string RequiredValueMissing = "NotificationTemplateRequiredValueMissing";
        public const string ParameterInvalid = "NotificationTemplateParameterInvalid";
        public const string EnvironmentVariableInvalid = "NotificationTemplateEnvironmentVariableInvalid";
        public const string InputValuesInvalid = "NotificationTemplateInputValuesInvalid";
    }

    /// <summary>
    /// Rendering failure. The message names the template and the cause, never data values.
    /// </summary>
    public sealed class NotificationTemplateException : Exception
    {
        public NotificationTemplateException(string errorCode, string templateCode, string problemDescription)
            : base(string.Format("{0}: Template '{1}' {2}", errorCode, templateCode, problemDescription))
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; private set; }
    }
}
