using System;
using System.Globalization;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Date and time column behavior, which decides whether a value is converted to the template time zone.
    /// </summary>
    public enum DateTimeValueBehavior
    {
        UserLocal,
        DateOnly,
        TimeZoneIndependent
    }

    /// <summary>
    /// A placeholder value as read from its source, before formatting.
    /// </summary>
    public sealed class SourceValue
    {
        public object RawValue { get; set; }
        public string FormattedValue { get; set; }
        public DateTimeValueBehavior DateTimeBehavior { get; set; }
    }

    /// <summary>
    /// Turns source values into text according to a parameter's Value Format.
    /// Raw and Custom use invariant culture; user-local date and time values are converted to the template time zone for Custom formats.
    /// </summary>
    public sealed class ParameterValueFormatter
    {
        private readonly Func<DateTime, DateTime> convertUtcToTemplateTimeZone;

        public ParameterValueFormatter(Func<DateTime, DateTime> convertUtcToTemplateTimeZone)
        {
            this.convertUtcToTemplateTimeZone = convertUtcToTemplateTimeZone;
        }

        public string Format(TemplateParameterDefinition parameter, SourceValue sourceValue, string templateCode)
        {
            if (sourceValue == null || sourceValue.RawValue == null) return null;
            switch (parameter.ValueFormat)
            {
                case ParameterValueFormat.Formatted:
                    return string.IsNullOrEmpty(sourceValue.FormattedValue)
                        ? FormatRawValue(parameter, sourceValue, templateCode)
                        : sourceValue.FormattedValue;
                case ParameterValueFormat.Custom:
                    return FormatCustomValue(parameter, sourceValue, templateCode);
                default:
                    return FormatRawValue(parameter, sourceValue, templateCode);
            }
        }

        private static string FormatRawValue(TemplateParameterDefinition parameter, SourceValue sourceValue, string templateCode)
        {
            var rawValue = sourceValue.RawValue;
            if (rawValue is string textValue) return textValue;
            if (rawValue is bool yesNoValue) return yesNoValue ? "true" : "false";
            if (rawValue is DateTime dateTimeValue) return FormatRawDateTime(dateTimeValue, sourceValue.DateTimeBehavior);
            if (rawValue is Guid identifierValue) return identifierValue.ToString("D");
            if (rawValue is EntityReference lookupValue) return lookupValue.Id.ToString("D");
            if (rawValue is OptionSetValue choiceValue) return choiceValue.Value.ToString(CultureInfo.InvariantCulture);
            if (rawValue is OptionSetValueCollection choiceValues)
            {
                return string.Join(",", choiceValues.Select(selectedChoice => selectedChoice.Value.ToString(CultureInfo.InvariantCulture)));
            }
            if (rawValue is Money moneyValue) return moneyValue.Value.ToString(CultureInfo.InvariantCulture);
            if (rawValue is int || rawValue is long || rawValue is decimal || rawValue is double)
            {
                return Convert.ToString(rawValue, CultureInfo.InvariantCulture);
            }
            throw new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, templateCode,
                string.Format("maps placeholder '{0}' to a column type the engine can't insert ({1}).", parameter.PlaceholderName, rawValue.GetType().Name));
        }

        private string FormatCustomValue(TemplateParameterDefinition parameter, SourceValue sourceValue, string templateCode)
        {
            var rawValue = sourceValue.RawValue;
            try
            {
                if (rawValue is DateTime dateTimeValue)
                {
                    var displayedDateTime = sourceValue.DateTimeBehavior == DateTimeValueBehavior.UserLocal
                        ? convertUtcToTemplateTimeZone(AsUtc(dateTimeValue))
                        : dateTimeValue;
                    return displayedDateTime.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
                }
                if (rawValue is Money moneyValue) return moneyValue.Value.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
                if (rawValue is int integerValue) return integerValue.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
                if (rawValue is long longValue) return longValue.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
                if (rawValue is decimal decimalValue) return decimalValue.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
                if (rawValue is double doubleValue) return doubleValue.ToString(parameter.FormatString, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, templateCode,
                    string.Format("has an invalid Format String for placeholder '{0}'.", parameter.PlaceholderName));
            }
            throw new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, templateCode,
                string.Format("uses Custom format for placeholder '{0}', but Custom only supports dates, times and numbers.", parameter.PlaceholderName));
        }

        private static string FormatRawDateTime(DateTime dateTimeValue, DateTimeValueBehavior dateTimeBehavior)
        {
            switch (dateTimeBehavior)
            {
                case DateTimeValueBehavior.DateOnly:
                    return dateTimeValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case DateTimeValueBehavior.TimeZoneIndependent:
                    return dateTimeValue.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                default:
                    return AsUtc(dateTimeValue).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            }
        }

        private static DateTime AsUtc(DateTime dateTimeValue)
        {
            return dateTimeValue.Kind == DateTimeKind.Local
                ? dateTimeValue.ToUniversalTime()
                : DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
        }
    }
}
