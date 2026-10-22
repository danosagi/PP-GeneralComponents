using System;
using System.Diagnostics;
using Microsoft.Xrm.Sdk;

namespace GeneralComponents.NotificationTemplates
{
    /// <summary>
    /// Main operation of Custom API gen_RenderNotificationTemplate.
    /// Inputs: TemplateCode (String), RecordId (Guid), optional InputValues (String, JSON object).
    /// Outputs: Subject (String), Body (String). All reads run as the calling user.
    /// </summary>
    public sealed class RenderNotificationTemplatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var callingUserService = serviceFactory.CreateOrganizationService(context.UserId);

            var templateCode = context.InputParameters.Contains("TemplateCode") ? context.InputParameters["TemplateCode"] as string : null;
            var recordId = context.InputParameters.Contains("RecordId") && context.InputParameters["RecordId"] is Guid
                ? (Guid)context.InputParameters["RecordId"]
                : Guid.Empty;
            var inputValuesJson = context.InputParameters.Contains("InputValues") ? context.InputParameters["InputValues"] as string : null;

            var renderTimer = Stopwatch.StartNew();
            try
            {
                var renderedNotification = new NotificationTemplateRenderer(callingUserService).Render(templateCode, recordId, inputValuesJson);
                context.OutputParameters["Subject"] = renderedNotification.Subject;
                context.OutputParameters["Body"] = renderedNotification.Body;
                tracing?.Trace("Rendered template {0}: {1} placeholders in {2} ms.", templateCode, renderedNotification.PlaceholderCount, renderTimer.ElapsedMilliseconds);
            }
            catch (NotificationTemplateException renderFailure)
            {
                tracing?.Trace("Template {0} failed with {1} after {2} ms.", templateCode, renderFailure.ErrorCode, renderTimer.ElapsedMilliseconds);
                throw new InvalidPluginExecutionException(renderFailure.Message);
            }
        }
    }
}
