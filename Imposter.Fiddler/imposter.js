(function (exports) {
    var interval = null,
        lastLoadTime = new Date();

    function _pollForChanges() {
        var req = new XMLHttpRequest(),
            url = [
                window.location.protocol,
                '//',
                window.location.host,
                '/',
                'imposter-poll-for-changes'
            ].join('');

        req.open('POST', url, true);
        req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        req.onreadystatechange = function onStateChanged() {
            if (req.readyState !== 4) {
                return;
            }
            if (req.status !== 200 && req.status !== 304) {
                return;
            }
            if (req.responseText == 'true') {
                window.location.reload();
            }
        };
        req.send(JSON.stringify({ lastLoadTime: lastLoadTime }));
    }

    function start() {
        if (!interval) {
            interval = setInterval(_pollForChanges, 3000);
        }
    }

    function stop() {
        if (interval) {
            clearInterval(interval);
            interval = null;
        }
    }

    exports.Imposter = {
        stop: stop,
        start: start
    };

    start();
}(window));

