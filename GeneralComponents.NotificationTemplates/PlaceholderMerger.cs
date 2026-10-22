using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Finds and replaces {{Name}} placeholders in one pass, so text inserted for one placeholder is never scanned again.
    /// </summary>
    public static class PlaceholderMerger
    {
        private static readonly Regex PlaceholderPattern = new Regex(@"\{\{([A-Za-z][A-Za-z0-9_]*)\}\}", RegexOptions.CultureInvariant);

        public static IList<string> FindPlaceholderNames(string templateText)
        {
            var placeholderNames = new List<string>();
            var seenPlaceholderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match placeholderMatch in PlaceholderPattern.Matches(templateText ?? string.Empty))
            {
                var placeholderName = placeholderMatch.Groups[1].Value;
                if (seenPlaceholderNames.Add(placeholderName)) placeholderNames.Add(placeholderName);
            }
            return placeholderNames;
        }

        public static void EnsureWellFormed(string templateText, string templateCode, string templateFieldName)
        {
            var textWithoutPlaceholders = PlaceholderPattern.Replace(templateText ?? string.Empty, string.Empty);
            if (textWithoutPlaceholders.Contains("{{"))
            {
                throw new NotificationTemplateException(NotificationTemplateErrorCodes.PlaceholderMalformed, templateCode,
                    string.Format("has a malformed placeholder in its {0}. Write each placeholder as {{{{Name}}}} with no spaces or formatting inside the braces.", templateFieldName));
            }
        }

        public static string Merge(string templateText, Func<string, string> getPlaceholderText)
        {
            return PlaceholderPattern.Replace(templateText ?? string.Empty, placeholderMatch => getPlaceholderText(placeholderMatch.Groups[1].Value));
        }
    }
}
