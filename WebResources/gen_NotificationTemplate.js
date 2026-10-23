var NotificationTemplate = NotificationTemplate || {};

(function () {
    "use strict";

    var SOURCE_DATA_AND_PARAMETERS_TAB_NAME = "tab_SourceDataAndParameters";
    var SYSTEM_ADMINISTRATOR_ROLE_NAME = "System Administrator";
    var TEMPLATE_CODE_COLUMN = "gen_templatecode";
    var TARGET_TABLE_COLUMN = "gen_targettable";
    var COLUMNS_LOCKED_AFTER_CREATION = [TEMPLATE_CODE_COLUMN, TARGET_TABLE_COLUMN];
    var CREATE_FORM_TYPE = 1;

    /**
     * Notification Template main form OnLoad.
     * Registered handler: NotificationTemplate.onLoad, pass execution context = true, only handler on this event.
     * Runs again after the first save, when the form switches from Create to Update.
     * 1. Shows the Source Data and Parameters tab (hidden in the form definition) only to System Administrators,
     *    who maintain the FetchXML queries and parameter details.
     * 2. Locks Template Code and Target Table once the template exists, because flows and queries depend on them.
     * Both are form behaviors only; table privileges remain the security control.
     */
    NotificationTemplate.onLoad = function (executionContext) {
        var formContext = executionContext.getFormContext();
        setSourceDataTabVisibility(formContext);
        lockColumnsAfterCreation(formContext);
    };

    function setSourceDataTabVisibility(formContext) {
        var sourceDataAndParametersTab = formContext.ui.tabs.get(SOURCE_DATA_AND_PARAMETERS_TAB_NAME);
        if (!sourceDataAndParametersTab) {
            return;
        }
        sourceDataAndParametersTab.setVisible(currentUserHasSystemAdministratorRole());
    }

    function lockColumnsAfterCreation(formContext) {
        var templateExists = formContext.ui.getFormType() !== CREATE_FORM_TYPE;
        COLUMNS_LOCKED_AFTER_CREATION.forEach(function (columnName) {
            var columnControl = formContext.getControl(columnName);
            if (columnControl) {
                columnControl.setDisabled(templateExists);
            }
        });
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
