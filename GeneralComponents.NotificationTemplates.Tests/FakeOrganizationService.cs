using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace GeneralComponents.NotificationTemplates.Tests
{
    /// <summary>
    /// In-memory stand-in for the Dataverse service with one template, its parameters, query rows and environment variables.
    /// </summary>
    internal sealed class FakeOrganizationService : IOrganizationService
    {
        public Entity TemplateRow { get; set; }
        public List<Entity> ParameterRows { get; } = new List<Entity>();
        public List<Entity> TemplateQueryRows { get; } = new List<Entity>();
        public string SecondaryQueryRootTable { get; set; }
        public List<Entity> SecondaryQueryRows { get; } = new List<Entity>();
        public List<Entity> EnvironmentVariableRows { get; } = new List<Entity>();
        public Dictionary<string, DateTimeBehavior> DateTimeBehaviorsByColumn { get; } = new Dictionary<string, DateTimeBehavior>(StringComparer.OrdinalIgnoreCase);
        public TimeSpan TemplateTimeZoneOffset { get; set; }
        public List<string> ExecutedFetchXmlQueries { get; } = new List<string>();

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            if (query is FetchExpression fetchQuery)
            {
                ExecutedFetchXmlQueries.Add(fetchQuery.Query);
                var isSecondaryQuery = SecondaryQueryRootTable != null && fetchQuery.Query.Contains("<entity name='" + SecondaryQueryRootTable + "'>");
                return new EntityCollection(isSecondaryQuery ? SecondaryQueryRows : TemplateQueryRows);
            }

            var tableQuery = (QueryExpression)query;
            switch (tableQuery.EntityName)
            {
                case NotificationTemplateSchema.TemplateTable:
                    var requestedCode = (string)tableQuery.Criteria.Conditions.First(condition => condition.AttributeName == NotificationTemplateSchema.TemplateCodeColumn).Values[0];
                    var matchingTemplates = TemplateRow != null && TemplateRow.GetAttributeValue<string>(NotificationTemplateSchema.TemplateCodeColumn) == requestedCode
                        ? new List<Entity> { TemplateRow }
                        : new List<Entity>();
                    return new EntityCollection(matchingTemplates);
                case NotificationTemplateSchema.ParameterTable:
                    return new EntityCollection(ParameterRows);
                case "environmentvariabledefinition":
                    var requestedSchemaName = (string)tableQuery.Criteria.Conditions.First(condition => condition.AttributeName == "schemaname").Values[0];
                    return new EntityCollection(EnvironmentVariableRows.Where(row => row.GetAttributeValue<string>("schemaname") == requestedSchemaName).ToList());
                default:
                    throw new NotSupportedException("Unexpected query for " + tableQuery.EntityName);
            }
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            if (request is LocalTimeFromUtcTimeRequest timeZoneRequest)
            {
                var timeZoneResponse = new LocalTimeFromUtcTimeResponse();
                timeZoneResponse.Results["LocalTime"] = DateTime.SpecifyKind(timeZoneRequest.UtcTime.Add(TemplateTimeZoneOffset), DateTimeKind.Unspecified);
                return timeZoneResponse;
            }
            if (request is RetrieveAttributeRequest columnRequest)
            {
                DateTimeBehavior columnBehavior;
                if (!DateTimeBehaviorsByColumn.TryGetValue(columnRequest.EntityLogicalName + "." + columnRequest.LogicalName, out columnBehavior))
                {
                    columnBehavior = DateTimeBehavior.UserLocal;
                }
                var columnResponse = new RetrieveAttributeResponse();
                columnResponse.Results["AttributeMetadata"] = new DateTimeAttributeMetadata { DateTimeBehavior = columnBehavior };
                return columnResponse;
            }
            throw new NotSupportedException("Unexpected request " + request.RequestName);
        }

        public Guid Create(Entity entity) { throw new NotSupportedException(); }
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) { throw new NotSupportedException(); }
        public void Update(Entity entity) { throw new NotSupportedException(); }
        public void Delete(string entityName, Guid id) { throw new NotSupportedException(); }
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) { throw new NotSupportedException(); }
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) { throw new NotSupportedException(); }
    }
}
