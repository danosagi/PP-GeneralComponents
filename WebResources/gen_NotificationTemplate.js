var NotificationTemplate = NotificationTemplate || {};

(function () {
    "use strict";

    var SOURCE_DATA_AND_PARAMETERS_TAB_NAME = "tab_SourceDataAndParameters";
    var SYSTEM_ADMINISTRATOR_ROLE_NAME = "System Administrator";

    /**
     * Notification Template main form OnLoad.
     * Registered handler: NotificationTemplate.onLoad, pass execution context = true, only handler on this event.
     * The Source Data and Parameters tab is hidden in the form definition and shown only to System Administrators,
     * who maintain the FetchXML queries and parameter details. This hides the tab only; table privileges remain the security control.
     */
    NotificationTemplate.onLoad = function (executionContext) {
        var formContext = executionContext.getFormContext();
        setSourceDataTabVisibility(formContext);
    };

    function setSourceDataTabVisibility(formContext) {
        var sourceDataAndParametersTab = formContext.ui.tabs.get(SOURCE_DATA_AND_PARAMETERS_TAB_NAME);
        if (!sourceDataAndParametersTab) {
            return;
        }
        sourceDataAndParametersTab.setVisible(currentUserHasSystemAdministratorRole());
    }

    function currentUserHasSystemAdministratorRole() {
        var currentUserRoles = Xrm.Utility.getGlobalContext().userSettings.roles;
        var hasSystemAdministratorRole = false;
        if (!currentUserRoles) {
            return hasSystemAdministratorRole;
        }
        currentUserRoles.forEach(function (currentUserRole) {
            if (currentUserRole && currentUserRole.name === SYSTEM_ADMINISTRATOR_ROLE_NAME) {
                hasSystemAdministratorRole = true;
            }
        });
        return hasSystemAdministratorRole;
    }

    window.NotificationTemplate = NotificationTemplate;
})();
