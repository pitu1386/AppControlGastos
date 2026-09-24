let deferredPrompt = null;

window.addEventListener('beforeinstallprompt', (e) => {
    // Evitar que el navegador muestre el banner por defecto automáticamente
    e.preventDefault();
    deferredPrompt = e;
    console.log("PWA install prompt capturado y listo para usar.");
    if (window.financialApp && window.financialApp.dotNetRef) {
        window.financialApp.dotNetRef.invokeMethodAsync('OnPwaInstallableChanged', true);
    }
});

window.addEventListener('appinstalled', () => {
    console.log("PWA instalada satisfactoriamente en el dispositivo.");
    deferredPrompt = null;
    if (window.financialApp && window.financialApp.dotNetRef) {
        window.financialApp.dotNetRef.invokeMethodAsync('OnPwaInstallableChanged', false);
    }
});

window.financialApp = {
    dotNetRef: null,
    registerDotNetRef: function (ref) {
        this.dotNetRef = ref;
        if (deferredPrompt && ref) {
            ref.invokeMethodAsync('OnPwaInstallableChanged', true);
        }
    },
    isInstallable: function () {
        return deferredPrompt !== null;
    },
    isStandalone: function () {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    },
    promptInstall: async function () {
        if (!deferredPrompt) {
            alert("Para instalar esta aplicación, haz clic en el icono de instalación (pantalla o '+') en la barra de direcciones de tu navegador (Chrome o Edge), o en 'Añadir a pantalla de inicio' en Safari móvil.");
            return false;
        }
        deferredPrompt.prompt();
        const choice = await deferredPrompt.userChoice;
        deferredPrompt = null;
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnPwaInstallableChanged', false);
        }
        return choice.outcome === 'accepted';
    },
    saveData: function (key, dataJson) {
        try {
            localStorage.setItem(key, dataJson);
            return true;
        } catch (e) {
            console.error("Error guardando en localStorage:", e);
            return false;
        }
    },
    loadData: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            console.error("Error leyendo de localStorage:", e);
            return null;
        }
    },
    downloadJsonFile: function (filename, content) {
        const blob = new Blob([content], { type: 'application/json;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};
