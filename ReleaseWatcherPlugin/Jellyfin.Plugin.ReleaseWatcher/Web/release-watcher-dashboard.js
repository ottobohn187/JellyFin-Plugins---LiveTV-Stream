(function () {
    'use strict';

    const statusId = 'releaseWatcherDashboardStatus';
    const checkedId = 'releaseWatcherDashboardChecked';
    let refreshPending = false;

    function ensureRow(containers, label, id) {
        let value = document.getElementById(id);
        if (value) {
            return value;
        }

        const labelElement = containers.labelTemplate.cloneNode(false);
        labelElement.textContent = label;

        value = containers.valueTemplate.cloneNode(false);
        value.id = id;
        value.textContent = 'Checking...';

        containers.labelContainer.appendChild(labelElement);
        containers.valueContainer.appendChild(value);
        return value;
    }

    function findServerInfo() {
        const labels = Array.from(document.querySelectorAll('p, div, span'));
        const buildLabel = labels.find(function (element) {
            return element.children.length === 0 && element.textContent.trim() === 'Build version';
        });

        if (!buildLabel) {
            return null;
        }

        const labelContainer = buildLabel.parentElement;
        const rowContainer = labelContainer && labelContainer.parentElement;
        const valueContainer = rowContainer && Array.from(rowContainer.children).find(function (element) {
            return element !== labelContainer && element.children.length === labelContainer.children.length;
        });
        if (!labelContainer || !valueContainer || !valueContainer.lastElementChild) {
            return null;
        }

        return {
            labelContainer: labelContainer,
            valueContainer: valueContainer,
            labelTemplate: buildLabel,
            valueTemplate: valueContainer.lastElementChild
        };
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

            const updateAvailable = status.UpdateAvailable ?? status.updateAvailable;
            const latestVersion = status.LatestVersion ?? status.latestVersion;
            const lastCheckedUtc = status.LastCheckedUtc ?? status.lastCheckedUtc;
            statusElement.textContent = updateAvailable
                ? 'Update available: ' + latestVersion
                : 'Up to date (' + latestVersion + ')';
            statusElement.style.color = updateAvailable ? '#ffb74d' : '#66bb6a';
            checkedElement.textContent = lastCheckedUtc
                ? new Date(lastCheckedUtc).toLocaleString()
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
