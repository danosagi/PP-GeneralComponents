using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Reads an environment variable's current value, falling back to its default value. Secret variables are refused.
    /// </summary>
    public sealed class EnvironmentVariableReader
    {
        private const int SecretVariableType = 100000005;
        private readonly IOrganizationService organizationService;

        public EnvironmentVariableReader(IOrganizationService organizationService)
        {
            this.organizationService = organizationService;
        }

        public string ReadValue(string schemaName, string templateCode)
        {
            var definitionQuery = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("schemaname", "type", "defaultvalue")
            };
            definitionQuery.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);
            var currentValueLink = definitionQuery.AddLink("environmentvariablevalue", "environmentvariabledefinitionid", "environmentvariabledefinitionid", JoinOperator.LeftOuter);
            currentValueLink.EntityAlias = "currentvalue";
            currentValueLink.Columns = new ColumnSet("value");

            var definitionRows = organizationService.RetrieveMultiple(definitionQuery).Entities;
            if (definitionRows.Count == 0)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.EnvironmentVariableInvalid, templateCode,
                    string.Format("refers to environment variable '{0}', which doesn't exist.", schemaName));
            }

            var variableType = definitionRows[0].GetAttributeValue<OptionSetValue>("type");
            if (variableType != null && variableType.Value == SecretVariableType)
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.EnvironmentVariableInvalid, templateCode,
                    string.Format("refers to secret environment variable '{0}'. Secret values can't be used in templates.", schemaName));
            }

            var currentValue = definitionRows
                .Select(definitionRow => definitionRow.GetAttributeValue<AliasedValue>("currentvalue.value"))
                .Where(aliasedValue => aliasedValue != null)
                .Select(aliasedValue => aliasedValue.Value as string)
                .FirstOrDefault(value => value != null);
            return currentValue ?? definitionRows[0].GetAttributeValue<string>("defaultvalue");
        }
    }
}
