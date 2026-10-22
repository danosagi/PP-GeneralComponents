using System;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Checks the settings of each parameter a template actually uses before any value is read.
    /// </summary>
    public static class TemplateParameterValidator
    {
        public static void Validate(TemplateParameterDefinition parameter, NotificationTemplateDefinition template)
        {
            if (!parameter.SourceType.HasValue)
            {
                throw ParameterInvalid(template, string.Format("has no valid Source Type for placeholder '{0}'.", parameter.PlaceholderName));
            }
            if (!parameter.ValueFormat.HasValue)
            {
                throw ParameterInvalid(template, string.Format("has no valid Value Format for placeholder '{0}'.", parameter.PlaceholderName));
            }
            if (string.IsNullOrWhiteSpace(parameter.SourceName))
            {
                throw ParameterInvalid(template, string.Format("has no Source Name for placeholder '{0}'.", parameter.PlaceholderName));
            }
            if (parameter.SourceType == ParameterSourceType.QueryColumn && !template.HasFetchXmlQuery)
            {
                throw ParameterInvalid(template, string.Format("maps placeholder '{0}' to a query column but has no FetchXML Query.", parameter.PlaceholderName));
            }
            if (parameter.SourceType == ParameterSourceType.SecondaryQueryColumn && !template.HasSecondaryFetchXmlQuery)
            {
                throw ParameterInvalid(template, string.Format("maps placeholder '{0}' to a secondary query column but has no Secondary FetchXML Query.", parameter.PlaceholderName));
            }
            if (parameter.SourceType == ParameterSourceType.SystemValue && !IsKnownSystemValue(parameter.SourceName))
            {
                throw ParameterInvalid(template, string.Format("uses unknown system value '{0}' for placeholder '{1}'. Use {2} or {3}.",
                    parameter.SourceName, parameter.PlaceholderName, SystemValueNames.RecordId, SystemValueNames.CurrentDateTime));
            }
            if (parameter.ValueFormat == ParameterValueFormat.Custom && string.IsNullOrWhiteSpace(parameter.FormatString))
            {
                throw ParameterInvalid(template, string.Format("uses Custom format without a Format String for placeholder '{0}'.", parameter.PlaceholderName));
            }
        }

        private static bool IsKnownSystemValue(string systemValueName)
        {
            return string.Equals(systemValueName, SystemValueNames.RecordId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(systemValueName, SystemValueNames.CurrentDateTime, StringComparison.OrdinalIgnoreCase);
        }

        private static NotificationTemplateException ParameterInvalid(NotificationTemplateDefinition template, string problemDescription)
        {
            return new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, template.TemplateCode, problemDescription);
        }
    }
}
