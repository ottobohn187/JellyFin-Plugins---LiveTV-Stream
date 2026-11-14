(function () {
    'use strict';

    const overlays = new WeakMap();
    let settingsPromise;
    const palettes = {
        Rainbow: ['#ff5733', '#ffd43b', '#32e875', '#22d3ee', '#4f67ff', '#d946ef', '#ff4f91'],
        Reds: ['#3b0707', '#7f1010', '#c51f1f', '#ff3b30', '#ff735c', '#ffb199'],
        Dark: ['#050505', '#151126', '#202b46', '#263f48', '#38274f', '#121212'],
        Bright: ['#fff200', '#39ff14', '#00f5ff', '#2474ff', '#ff35e8', '#ff4d00'],
        Monochrome: ['#090909', '#343434', '#777777', '#bdbdbd', '#ffffff']
    };

    function api(path) {
        if (window.ApiClient) {
            return ApiClient.ajax({ type: 'GET', url: ApiClient.getUrl(path), dataType: 'json' });
        }
        return fetch('/' + path, { credentials: 'same-origin' }).then(response => {
            if (!response.ok) throw new Error('Request failed: ' + response.status);
            return response.json();
        });
    }

    function getSettings() {
        settingsPromise ||= api('Plugins/MusicVideoVisualizer/settings');
        return settingsPromise;
    }

    function getItemId(video) {
        const hash = window.location.hash || '';
        const query = hash.includes('?') ? hash.slice(hash.indexOf('?') + 1) : '';
        const fromHash = new URLSearchParams(query).get('id');
        if (fromHash) return fromHash;
        const match = (video.currentSrc || video.src || '').match(/(?:Videos|Audio)\/([a-f0-9]{32})/i);
        if (match) return match[1];
        const itemHost = video.closest('[data-id]') || document.querySelector('.videoOsdPage [data-id]');
        return itemHost?.dataset?.id;
    }

    async function getPlayingItemId(video, userId) {
        const directId = getItemId(video);
        if (directId) return directId;
        const sessions = await api('Sessions?ControllableByUserId=' + encodeURIComponent(userId));
        const deviceId = typeof ApiClient.deviceId === 'function' ? ApiClient.deviceId() : null;
        const current = sessions.find(session =>
            (session.NowPlayingItem || session.nowPlayingItem) &&
            (!deviceId || String(session.DeviceId || session.deviceId) === String(deviceId))
        ) || sessions.find(session => session.NowPlayingItem || session.nowPlayingItem);
        const item = current && (current.NowPlayingItem || current.nowPlayingItem);
        return item && (item.Id || item.id);
    }

    async function isSelectedVideo(video, settings) {
        if (!settings.LibraryIds?.length || !window.ApiClient) return false;
        const userId = ApiClient.getCurrentUserId && ApiClient.getCurrentUserId();
        if (!userId) return false;
        const itemId = await getPlayingItemId(video, userId);
        if (!itemId) return false;
        const [item, ancestors] = await Promise.all([
            api('Users/' + userId + '/Items/' + itemId),
            api('Items/' + itemId + '/Ancestors')
        ]);
        if ((item.MediaType || item.mediaType) !== 'Video') return false;
        const ids = new Set(settings.LibraryIds.map(String));
        return ids.has(String(itemId)) || ancestors.some(ancestor => ids.has(String(ancestor.Id || ancestor.id)));
    }

    function createOverlay(video, settings) {
        if (overlays.has(video)) return overlays.get(video);
        const host = video.parentElement;
        if (!host) return null;
        if (getComputedStyle(host).position === 'static') host.style.position = 'relative';

        const canvas = document.createElement('canvas');
        canvas.className = 'musicVideoSpectrumOverlay';
        Object.assign(canvas.style, {
            position: 'absolute', left: '0', right: '0', bottom: '0', width: '100%',
            height: settings.HeightPercent + '%', zIndex: '12', pointerEvents: 'none',
            opacity: String(settings.Opacity), mixBlendMode: 'screen'
        });
        host.appendChild(canvas);

        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const source = audioContext.createMediaElementSource(video);
        const analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        analyser.smoothingTimeConstant = 0.82;
        source.connect(analyser);
        analyser.connect(audioContext.destination);

        const state = { canvas, audioContext, analyser, frame: 0, settings };
        overlays.set(video, state);
        draw(state);
        return state;
    }

    function draw(state) {
        const { canvas, analyser, settings } = state;
        if (!canvas.isConnected) {
            cancelAnimationFrame(state.frame);
            return;
        }
        const ratio = Math.min(window.devicePixelRatio || 1, 2);
        const width = Math.max(1, Math.floor(canvas.clientWidth * ratio));
        const height = Math.max(1, Math.floor(canvas.clientHeight * ratio));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width; canvas.height = height;
        }
        const context = canvas.getContext('2d');
        context.clearRect(0, 0, width, height);
        const values = new Uint8Array(analyser.frequencyBinCount);
        analyser.getByteFrequencyData(values);
        const count = Math.min(settings.BarCount, values.length);
        const gap = Math.max(1, width / count * 0.18);
        const barWidth = width / count - gap;
        const gradient = context.createLinearGradient(0, 0, width, 0);
        const colors = palettes[settings.Palette] || palettes.Rainbow;
        colors.forEach((color, index) => gradient.addColorStop(index / (colors.length - 1), color));
        context.fillStyle = gradient;
        for (let index = 0; index < count; index++) {
            const sourceIndex = Math.floor(index / count * values.length * 0.72);
            const amplitude = values[sourceIndex] / 255;
            const barHeight = Math.max(2 * ratio, Math.pow(amplitude, 1.45) * height * 0.94);
            const x = index * (barWidth + gap) + gap / 2;
            const y = height - barHeight;
            context.beginPath();
            context.roundRect(x, y, barWidth, barHeight, Math.min(barWidth / 2, 4 * ratio));
            context.fill();
        }
        state.frame = requestAnimationFrame(() => draw(state));
    }

    async function activate(video) {
        try {
            const settings = await getSettings();
            if (!(settings.Enabled ?? settings.enabled)) return;
            settings.LibraryIds = settings.LibraryIds ?? settings.libraryIds ?? [];
            settings.BarCount = settings.BarCount ?? settings.barCount ?? 64;
            settings.HeightPercent = settings.HeightPercent ?? settings.heightPercent ?? 22;
            settings.Opacity = settings.Opacity ?? settings.opacity ?? 0.9;
            settings.Palette = settings.Palette ?? settings.palette ?? 'Rainbow';
            if (!await isSelectedVideo(video, settings)) return;
            const state = createOverlay(video, settings);
            if (state?.audioContext.state === 'suspended') await state.audioContext.resume();
        } catch (error) {
            console.warn('Music Video Visualizer could not start', error);
        }
    }

    document.addEventListener('play', event => {
        if (event.target instanceof HTMLVideoElement) activate(event.target);
    }, true);
    document.addEventListener('emptied', event => {
        const state = overlays.get(event.target);
        if (state) state.canvas.remove();
    }, true);
}());
