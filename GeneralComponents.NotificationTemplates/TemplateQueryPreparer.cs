using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// A template query with the record ID inserted, plus the column aliases it defines.
    /// </summary>
    public sealed class PreparedTemplateQuery
    {
        public PreparedTemplateQuery(string fetchXml, ISet<string> columnAliases)
        {
            FetchXml = fetchXml;
            ColumnAliases = columnAliases;
        }

        public string FetchXml { get; private set; }
        public ISet<string> ColumnAliases { get; private set; }
    }

    /// <summary>
    /// Inserts the record ID into a template's FetchXML and enforces the query rules the engine relies on.
    /// Both queries allow only {{RecordId}} as a placeholder and need an alias on every column.
    /// The primary query must also use {{RecordId}} and start from the Target Table; the secondary query may query any table.
    /// </summary>
    public static class TemplateQueryPreparer
    {
        public const string PrimaryQueryLabel = "FetchXML Query";
        public const string SecondaryQueryLabel = "Secondary FetchXML Query";
        private static readonly Regex RecordIdPlaceholderPattern = new Regex(@"\{\{RecordId\}\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static PreparedTemplateQuery Prepare(string fetchXmlQuery, Guid recordId, string targetTable, string templateCode)
        {
            return PrepareQuery(fetchXmlQuery, recordId, targetTable, templateCode, PrimaryQueryLabel);
        }

        public static PreparedTemplateQuery PrepareSecondary(string fetchXmlQuery, Guid recordId, string templateCode)
        {
            return PrepareQuery(fetchXmlQuery, recordId, null, templateCode, SecondaryQueryLabel);
        }

        private static PreparedTemplateQuery PrepareQuery(string fetchXmlQuery, Guid recordId, string requiredRootTable, string templateCode, string queryLabel)
        {
            var isPrimaryQuery = requiredRootTable != null;
            if (string.IsNullOrWhiteSpace(fetchXmlQuery) || (isPrimaryQuery && !RecordIdPlaceholderPattern.IsMatch(fetchXmlQuery)))
            {
                throw QueryInvalid(templateCode, string.Format("has a {0} without {{{{RecordId}}}}.", queryLabel));
            }

            var preparedFetchXml = RecordIdPlaceholderPattern.Replace(fetchXmlQuery, recordId.ToString("D"));
            if (preparedFetchXml.Contains("{{"))
            {
                throw QueryInvalid(templateCode, string.Format("uses a placeholder other than {{{{RecordId}}}} in its {0}.", queryLabel));
            }

            var queryDocument = ParseFetchXml(preparedFetchXml, templateCode, queryLabel);
            var fetchElement = queryDocument.Root;
            if (fetchElement == null || fetchElement.Name.LocalName != "fetch")
            {
                throw QueryInvalid(templateCode, string.Format("has a {0} that doesn't start with a fetch element.", queryLabel));
            }

            var rootEntityElements = fetchElement.Elements("entity").ToList();
            if (rootEntityElements.Count != 1)
            {
                throw QueryInvalid(templateCode, string.Format("has a {0} without exactly one root entity element.", queryLabel));
            }

            var rootTableName = (string)rootEntityElements[0].Attribute("name");
            if (isPrimaryQuery && !string.Equals(rootTableName, requiredRootTable, StringComparison.OrdinalIgnoreCase))
            {
                throw QueryInvalid(templateCode, string.Format("queries table '{0}', but its Target Table is '{1}'.", rootTableName, requiredRootTable));
            }

            if (queryDocument.Descendants("all-attributes").Any())
            {
                throw QueryInvalid(templateCode, string.Format("uses all-attributes in its {0}. List each column with an alias instead.", queryLabel));
            }

            var columnAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var columnElement in queryDocument.Descendants("attribute"))
            {
                var columnAlias = (string)columnElement.Attribute("alias");
                if (string.IsNullOrWhiteSpace(columnAlias))
                {
                    throw QueryInvalid(templateCode, string.Format("selects column '{0}' without an alias in its {1}.", (string)columnElement.Attribute("name"), queryLabel));
                }
                if (!columnAliases.Add(columnAlias))
                {
                    throw QueryInvalid(templateCode, string.Format("uses the alias '{0}' more than once in its {1}.", columnAlias, queryLabel));
                }
            }

            return new PreparedTemplateQuery(preparedFetchXml, columnAliases);
        }

        private static XDocument ParseFetchXml(string fetchXml, string templateCode, string queryLabel)
        {
            var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            try
            {
                using (var fetchXmlTextReader = new StringReader(fetchXml))
                using (var fetchXmlReader = XmlReader.Create(fetchXmlTextReader, readerSettings))
                {
                    return XDocument.Load(fetchXmlReader);
                }
            }
            catch (XmlException)
            {
                throw QueryInvalid(templateCode, string.Format("has a {0} that isn't valid XML.", queryLabel));
            }
        }

        private static NotificationTemplateException QueryInvalid(string templateCode, string problemDescription)
        {
            return new NotificationTemplateException(NotificationTemplateErrorCodes.QueryInvalid, templateCode, problemDescription);
        }
    }
}
