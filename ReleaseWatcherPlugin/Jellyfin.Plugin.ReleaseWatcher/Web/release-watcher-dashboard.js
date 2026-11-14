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

    function findServerInfo() {
        const labels = Array.from(document.querySelectorAll('div, span'));
        const buildLabel = labels.find(function (element) {
            return element.children.length === 0 && element.textContent.trim() === 'Build version';
        });

        if (!buildLabel) {
            return null;
        }

        let container = buildLabel.parentElement;
        while (container && container !== document.body) {
            const text = container.textContent;
            if (text.includes('Server name') && text.includes('Server version') && text.includes('Web version')) {
                return container;
            }

            container = container.parentElement;
        }

        return null;
    }

    function getAccessToken() {
        try {
            const credentials = JSON.parse(localStorage.getItem('jellyfin_credentials') || '{}');
            const servers = credentials.Servers || credentials.servers || [];
            const server = servers.find(function (item) {
                return item.AccessToken || item.accessToken;
            });
            return server && (server.AccessToken || server.accessToken);
        } catch (error) {
            return null;
        }
    }

    async function getStatus() {
        if (window.ApiClient) {
            return ApiClient.ajax({
                type: 'GET',
                url: ApiClient.getUrl('Plugins/ReleaseWatcher/status'),
                dataType: 'json'
            });
        }

        const token = getAccessToken();
        const response = await fetch('/Plugins/ReleaseWatcher/status', {
            headers: token ? { 'X-Emby-Token': token } : {},
            credentials: 'same-origin'
        });
        if (!response.ok) {
            throw new Error('Release Watcher status request failed: ' + response.status);
        }

        return response.json();
    }

    async function refreshWidget() {
        refreshPending = false;
        const container = findServerInfo();
        if (!container) {
            return;
        }

        const statusElement = ensureRow(container, 'Release status', statusId);
        const checkedElement = ensureRow(container, 'Last update check', checkedId);

        try {
            const status = await getStatus();

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
