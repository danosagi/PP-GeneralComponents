using System;
using System.Collections.Generic;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Reads each placeholder's value from its source: the primary or secondary query row, an environment variable, caller input or a system value.
    /// </summary>
    public sealed class ParameterValueResolver
    {
        private readonly NotificationTemplateDefinition template;
        private readonly TemplateQueryRow queryRow;
        private readonly TemplateQueryRow secondaryQueryRow;
        private readonly IDictionary<string, string> callerInputValues;
        private readonly Guid recordId;
        private readonly EnvironmentVariableReader environmentVariableReader;
        private readonly DateTimeBehaviorReader dateTimeBehaviorReader;
        private readonly DateTime renderedAtUtc;

        public ParameterValueResolver(
            NotificationTemplateDefinition template,
            TemplateQueryRow queryRow,
            TemplateQueryRow secondaryQueryRow,
            IDictionary<string, string> callerInputValues,
            Guid recordId,
            EnvironmentVariableReader environmentVariableReader,
            DateTimeBehaviorReader dateTimeBehaviorReader,
            DateTime renderedAtUtc)
        {
            this.template = template;
            this.queryRow = queryRow;
            this.secondaryQueryRow = secondaryQueryRow;
            this.callerInputValues = callerInputValues;
            this.recordId = recordId;
            this.environmentVariableReader = environmentVariableReader;
            this.dateTimeBehaviorReader = dateTimeBehaviorReader;
            this.renderedAtUtc = renderedAtUtc;
        }

        public SourceValue Resolve(TemplateParameterDefinition parameter)
        {
            switch (parameter.SourceType.Value)
            {
                case ParameterSourceType.QueryColumn:
                    return ResolveQueryColumn(parameter, queryRow, TemplateQueryPreparer.PrimaryQueryLabel);
                case ParameterSourceType.SecondaryQueryColumn:
                    return ResolveQueryColumn(parameter, secondaryQueryRow, TemplateQueryPreparer.SecondaryQueryLabel);
                case ParameterSourceType.EnvironmentVariable:
                    return new SourceValue { RawValue = environmentVariableReader.ReadValue(parameter.SourceName, template.TemplateCode) };
                case ParameterSourceType.CallerInput:
                    callerInputValues.TryGetValue(parameter.SourceName, out var callerInputValue);
                    return new SourceValue { RawValue = callerInputValue };
                default:
                    return ResolveSystemValue(parameter);
            }
        }

        private SourceValue ResolveQueryColumn(TemplateParameterDefinition parameter, TemplateQueryRow sourceQueryRow, string queryLabel)
        {
            if (sourceQueryRow == null || !sourceQueryRow.DefinesAlias(parameter.SourceName))
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.ParameterInvalid, template.TemplateCode,
                    string.Format("maps placeholder '{0}' to alias '{1}', which its {2} doesn't define.", parameter.PlaceholderName, parameter.SourceName, queryLabel));
            }

            var columnValue = sourceQueryRow.GetColumnValue(parameter.SourceName);
            var dateTimeBehavior = columnValue.Value is DateTime
                ? dateTimeBehaviorReader.ReadBehavior(columnValue.SourceTableName, columnValue.SourceColumnName)
                : DateTimeValueBehavior.UserLocal;
            return new SourceValue
            {
                RawValue = columnValue.Value,
                FormattedValue = columnValue.FormattedValue,
                DateTimeBehavior = dateTimeBehavior
            };
        }

        private SourceValue ResolveSystemValue(TemplateParameterDefinition parameter)
        {
            if (string.Equals(parameter.SourceName, SystemValueNames.CurrentDateTime, StringComparison.OrdinalIgnoreCase))
            {
                return new SourceValue { RawValue = renderedAtUtc, DateTimeBehavior = DateTimeValueBehavior.UserLocal };
            }
            return new SourceValue { RawValue = recordId };
        }
    }
}
