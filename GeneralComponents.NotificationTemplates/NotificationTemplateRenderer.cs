using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Renders a notification template's subject and body for one record.
    /// Every Dataverse read uses the service passed in, which the plugin creates for the calling user.
    /// </summary>
    public sealed class NotificationTemplateRenderer
    {
        private readonly IOrganizationService organizationService;
        private readonly Func<DateTime> getCurrentUtcTime;

        public NotificationTemplateRenderer(IOrganizationService organizationService)
            : this(organizationService, () => DateTime.UtcNow)
        {
        }

        public NotificationTemplateRenderer(IOrganizationService organizationService, Func<DateTime> getCurrentUtcTime)
        {
            this.organizationService = organizationService;
            this.getCurrentUtcTime = getCurrentUtcTime;
        }

        public RenderedNotification Render(string templateCode, Guid recordId, string inputValuesJson)
        {
            var template = new NotificationTemplateReader(organizationService).ReadActiveTemplate(templateCode);
            var callerInputValues = CallerInputValuesParser.Parse(inputValuesJson, template.TemplateCode);
            PlaceholderMerger.EnsureWellFormed(template.Subject, template.TemplateCode, "Subject");
            PlaceholderMerger.EnsureWellFormed(template.Body, template.TemplateCode, "Body");

            var usedPlaceholderNames = PlaceholderMerger.FindPlaceholderNames(template.Subject)
                .Concat(PlaceholderMerger.FindPlaceholderNames(template.Body))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var parametersByPlaceholder = IndexParametersByPlaceholder(template);
            var placeholdersWithoutParameters = usedPlaceholderNames.Where(placeholderName => !parametersByPlaceholder.ContainsKey(placeholderName)).ToList();
            if (placeholdersWithoutParameters.Count > 0)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.PlaceholderUnresolved, template.TemplateCode,
                    string.Format("uses placeholders without parameter rows: {0}.", string.Join(", ", placeholdersWithoutParameters)));
            }

            var usedParameters = usedPlaceholderNames.Select(placeholderName => parametersByPlaceholder[placeholderName]).ToList();
            foreach (var usedParameter in usedParameters)
            {
                TemplateParameterValidator.Validate(usedParameter, template);
            }

            var queryRunner = new TemplateQueryRunner(organizationService);
            var queryRow = template.HasFetchXmlQuery ? queryRunner.RunSingleRowQuery(template, recordId) : null;
            var secondaryQueryRow = template.HasSecondaryFetchXmlQuery ? queryRunner.RunSecondaryQuery(template, recordId) : null;
            var valueResolver = new ParameterValueResolver(template, queryRow, secondaryQueryRow, callerInputValues, recordId,
                new EnvironmentVariableReader(organizationService), new DateTimeBehaviorReader(organizationService), getCurrentUtcTime());
            var valueFormatter = new ParameterValueFormatter(utcDateTime => ConvertUtcToTemplateTimeZone(utcDateTime, template.TimeZoneCode));

            var subjectTextByPlaceholder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bodyTextByPlaceholder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var usedParameter in usedParameters)
            {
                var placeholderText = valueFormatter.Format(usedParameter, valueResolver.Resolve(usedParameter), template.TemplateCode);
                if (string.IsNullOrWhiteSpace(placeholderText))
                {
                    placeholderText = ApplyBlankValueRules(usedParameter, template.TemplateCode);
                }
                subjectTextByPlaceholder[usedParameter.PlaceholderName] = PrepareSubjectText(placeholderText);
                bodyTextByPlaceholder[usedParameter.PlaceholderName] = PrepareBodyText(placeholderText, usedParameter.HtmlEncode);
            }

            return new RenderedNotification
            {
                Subject = PlaceholderMerger.Merge(template.Subject, placeholderName => subjectTextByPlaceholder[placeholderName]),
                Body = PlaceholderMerger.Merge(template.Body, placeholderName => bodyTextByPlaceholder[placeholderName]),
                PlaceholderCount = usedParameters.Count
            };
        }

        private static Dictionary<string, TemplateParameterDefinition> IndexParametersByPlaceholder(NotificationTemplateDefinition template)
        {
            var parametersByPlaceholder = new Dictionary<string, TemplateParameterDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in template.Parameters.Where(parameter => parameter.PlaceholderName.Length > 0))
            {
                if (parametersByPlaceholder.ContainsKey(parameter.PlaceholderName))
                {
                    throw new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, template.TemplateCode,
                        string.Format("has more than one parameter row for placeholder '{0}'.", parameter.PlaceholderName));
                }
                parametersByPlaceholder.Add(parameter.PlaceholderName, parameter);
            }
            return parametersByPlaceholder;
        }

        private static string ApplyBlankValueRules(TemplateParameterDefinition parameter, string templateCode)
        {
            if (!string.IsNullOrEmpty(parameter.DefaultValue)) return parameter.DefaultValue;
            if (parameter.IsRequired)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.RequiredValueMissing, templateCode,
                    string.Format("has no value and no Default Value for required placeholder '{0}'.", parameter.PlaceholderName));
            }
            return string.Empty;
        }

        private static string PrepareSubjectText(string placeholderText)
        {
            return placeholderText.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string PrepareBodyText(string placeholderText, bool htmlEncode)
        {
            if (!htmlEncode) return placeholderText;
            return WebUtility.HtmlEncode(placeholderText).Replace("\r\n", "<br>").Replace("\r", "<br>").Replace("\n", "<br>");
        }

        private DateTime ConvertUtcToTemplateTimeZone(DateTime utcDateTime, int? timeZoneCode)
        {
            if (!timeZoneCode.HasValue) return utcDateTime;
            var conversionResponse = (LocalTimeFromUtcTimeResponse)organizationService.Execute(new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcDateTime
            });
            return conversionResponse.LocalTime;
        }
    }
}
