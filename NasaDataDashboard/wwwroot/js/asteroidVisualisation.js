window.sendAsteroidDataToIframe = function (asteroidData) {
    // Wait for iframe to load
    const iframe = document.getElementById('asteroid3d-frame');

    const sendData = () => {
        iframe.contentWindow.postMessage({
            type: 'asteroidData',
            data: asteroidData
        }, '*');
    };

    if (iframe.contentDocument.readyState === 'complete') {
        sendData();
    } else {
        iframe.addEventListener('load', sendData);
    }
};