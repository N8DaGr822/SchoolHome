const PORTFOLIO_DB_NAME = 'homeschool-portfolio-files';
const PORTFOLIO_STORE_NAME = 'files';
const portfolioUrlCache = new Map();
let portfolioDbPromise = null;

function openPortfolioDb() {
    if (!portfolioDbPromise) {
        portfolioDbPromise = new Promise((resolve, reject) => {
            const request = indexedDB.open(PORTFOLIO_DB_NAME, 1);
            request.onupgradeneeded = () => {
                request.result.createObjectStore(PORTFOLIO_STORE_NAME);
            };
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    return portfolioDbPromise;
}

function idbPut(db, key, value) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(PORTFOLIO_STORE_NAME, 'readwrite');
        tx.objectStore(PORTFOLIO_STORE_NAME).put(value, key);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

function idbGet(db, key) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(PORTFOLIO_STORE_NAME, 'readonly');
        const request = tx.objectStore(PORTFOLIO_STORE_NAME).get(key);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function idbDelete(db, key) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(PORTFOLIO_STORE_NAME, 'readwrite');
        tx.objectStore(PORTFOLIO_STORE_NAME).delete(key);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

window.homeschoolData = {
    downloadTextFile(fileName, contentType, content) {
        const blob = new Blob([content], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = fileName;
        link.style.display = 'none';

        document.body.appendChild(link);
        link.click();
        link.remove();

        window.setTimeout(() => URL.revokeObjectURL(url), 0);
    },

    getItem(key) {
        return localStorage.getItem(key);
    },

    setItem(key, value) {
        localStorage.setItem(key, value);
    },

    async savePortfolioFile(id, contentType, originalFileName, streamRef) {
        const buffer = await streamRef.arrayBuffer();
        const blob = new Blob([buffer], { type: contentType || 'application/octet-stream' });
        const db = await openPortfolioDb();
        await idbPut(db, id, { blob, contentType, originalFileName });

        const cached = portfolioUrlCache.get(id);
        if (cached) {
            URL.revokeObjectURL(cached);
            portfolioUrlCache.delete(id);
        }
    },

    async getPortfolioFileUrl(id) {
        if (portfolioUrlCache.has(id)) {
            return portfolioUrlCache.get(id);
        }

        const db = await openPortfolioDb();
        const record = await idbGet(db, id);
        if (!record) {
            return null;
        }

        const url = URL.createObjectURL(record.blob);
        portfolioUrlCache.set(id, url);
        return url;
    },

    async deletePortfolioFile(id) {
        const db = await openPortfolioDb();
        await idbDelete(db, id);

        const cached = portfolioUrlCache.get(id);
        if (cached) {
            URL.revokeObjectURL(cached);
            portfolioUrlCache.delete(id);
        }
    }
};
