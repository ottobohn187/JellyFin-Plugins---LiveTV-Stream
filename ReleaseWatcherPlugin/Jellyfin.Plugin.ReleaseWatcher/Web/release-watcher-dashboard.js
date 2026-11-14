(function () {
    'use strict';

    const statusId = 'releaseWatcherDashboardStatus';
    const checkedId = 'releaseWatcherDashboardChecked';
    let refreshPending = false;

    function ensureRow(container, label, id) {
        let value = document.getElementById(id);
        if (value) {
            return value;
        }

        const labelElement = document.createElement('div');
        labelElement.textContent = label;
        labelElement.style.fontWeight = '700';

        value = document.createElement('div');
        value.id = id;
        value.textContent = 'Checking...';

        container.appendChild(labelElement);
        container.appendChild(value);
        return value;
    }

    async function refreshWidget() {
        refreshPending = false;
        const buildVersion = document.getElementById('buildVersion');
        const container = buildVersion && buildVersion.closest('.serverInfo');
        if (!container || !window.ApiClient) {
            return;
        }

        const statusElement = ensureRow(container, 'Release status', statusId);
        const checkedElement = ensureRow(container, 'Last update check', checkedId);

        try {
            const status = await ApiClient.ajax({
                type: 'GET',
                url: ApiClient.getUrl('Plugins/ReleaseWatcher/status'),
                dataType: 'json'
            });

            statusElement.textContent = status.UpdateAvailable
                ? 'Update available: ' + status.LatestVersion
                : 'Up to date (' + status.LatestVersion + ')';
            statusElement.style.color = status.UpdateAvailable ? '#ffb74d' : '#66bb6a';
            checkedElement.textContent = status.LastCheckedUtc
                ? new Date(status.LastCheckedUtc).toLocaleString()
                : 'Not checked yet';
        } catch (error) {
            statusElement.textContent = 'Unable to check';
            statusElement.style.color = '#ef5350';
            checkedElement.textContent = 'Open Release Watcher settings for details';
        }
    }

    function scheduleRefresh() {
        if (refreshPending) {
            return;
        }

        refreshPending = true;
        window.setTimeout(refreshWidget, 250);
    }

    new MutationObserver(function () {
        if (!document.getElementById(statusId)) {
            scheduleRefresh();
        }
    }).observe(document.documentElement, {
        childList: true,
        subtree: true
    });
    window.addEventListener('hashchange', scheduleRefresh);
    window.setInterval(scheduleRefresh, 60000);
    scheduleRefresh();
}());
