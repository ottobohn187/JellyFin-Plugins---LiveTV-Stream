(function () {
    'use strict';
    const markerVersion = '1';
    const suffixes = ['movies', 'series', 'episodes', 'musicvideos', 'moviecollections', 'Folder'];
    const api = (path, options = {}) => ApiClient.ajax({ url: ApiClient.getUrl(path), dataType: 'json', ...options });
    async function applyDefaults() {
        if (!window.ApiClient || !ApiClient.getCurrentUserId) return;
        const userId = ApiClient.getCurrentUserId();
        if (!userId) return;
        const marker = `default-date-added-${markerVersion}-${userId}`;
        if (localStorage.getItem(marker)) return;
        const [prefs, libraries] = await Promise.all([
            api(`DisplayPreferences/usersettings?userId=${userId}&client=emby`),
            api('Library/VirtualFolders')
        ]);
        prefs.CustomPrefs ||= {};
        libraries.filter(x => x.ItemId).forEach(library => {
            suffixes.forEach(suffix => {
                prefs.CustomPrefs[`${library.ItemId}-${suffix}`] = JSON.stringify({
                    SortBy: suffix === 'movies' ? 'DateCreated,SortName,ProductionYear' : 'DateCreated,SortName',
                    SortOrder: 'Descending'
                });
            });
            prefs.CustomPrefs[`items-${library.ItemId}-Folder-sortby`] = 'DateCreated,SortName';
            prefs.CustomPrefs[`items-${library.ItemId}-Folder-sortorder`] = 'Descending';
        });
        await api(`DisplayPreferences/usersettings?userId=${userId}&client=emby`, { type: 'POST', contentType: 'application/json', data: JSON.stringify(prefs) });
        localStorage.setItem(marker, new Date().toISOString());
    }
    const run = () => applyDefaults().catch(error => console.warn('Default Date Added Sort failed', error));
    document.addEventListener('viewshow', run);
    window.addEventListener('hashchange', run);
    setTimeout(run, 1500);
}());
