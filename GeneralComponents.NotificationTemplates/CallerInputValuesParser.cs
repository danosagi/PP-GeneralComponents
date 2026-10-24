using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Reads the optional InputValues_Optional request parameter: a JSON object such as {"Reason":"Late submission"}.
    /// Names are matched without regard to case.
    /// </summary>
    public static class CallerInputValuesParser
    {
        public static IDictionary<string, string> Parse(string inputValuesJson, string templateCode)
        {
            var callerInputValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(inputValuesJson)) return callerInputValues;
            if (!inputValuesJson.TrimStart().StartsWith("{", StringComparison.Ordinal)) throw InputValuesInvalid(templateCode);

            Dictionary<string, string> parsedInputValues;
            try
            {
                var inputValuesSerializer = new DataContractJsonSerializer(
                    typeof(Dictionary<string, string>),
                    new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                using (var inputValuesStream = new MemoryStream(Encoding.UTF8.GetBytes(inputValuesJson)))
                {
                    parsedInputValues = (Dictionary<string, string>)inputValuesSerializer.ReadObject(inputValuesStream);
                }
            }
            catch (Exception parseFailure) when (parseFailure is SerializationException || parseFailure is XmlException ||
                                                 parseFailure is ArgumentException || parseFailure is InvalidCastException)
            {
                throw InputValuesInvalid(templateCode);
            }

            if (parsedInputValues == null) throw InputValuesInvalid(templateCode);
            foreach (var parsedInputValue in parsedInputValues)
            {
                if (callerInputValues.ContainsKey(parsedInputValue.Key)) throw InputValuesInvalid(templateCode);
                callerInputValues.Add(parsedInputValue.Key, parsedInputValue.Value);
            }
            return callerInputValues;
        }

        private static NotificationTemplateException InputValuesInvalid(string templateCode)
        {
            return new NotificationTemplateException(NotificationTemplateErrorCodes.InputValuesInvalid, templateCode,
                "received InputValues_Optional that isn't a JSON object of uniquely named text values, for example {\"Reason\":\"Late submission\"}.");
        }
    }
}
