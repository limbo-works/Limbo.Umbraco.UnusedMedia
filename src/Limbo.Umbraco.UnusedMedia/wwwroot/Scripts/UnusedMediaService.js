async function hi(url, config) {

    if (!config) config = {};
    if (!config.method) config.method = "GET";
    if (!config.headers) config.headers = {};

    if (config.params) {
        const queryString = new URLSearchParams(config.params).toString();
        url += `?${queryString}`;
    }

    const response = await fetch(url, config);

    const contentType = response.headers.get("content-type");

    if (contentType && contentType.includes("application/json")) {

        const text = await response.text();

        // Remove the XSSI prefix
        const cleaned = text.replace(/^\)\]\}',?\n/, '');

        // Parse JSON
        response.data = JSON.parse(cleaned);

    }

    return response;

}

async function get(url, config) {
    return await hi(url, config);
}

async function post(url, config) {
    if (!config) config = {};
    config.method = "POST";
    return await hi(url, config);
}

async function postJson(url, body, config) {
    if (!config) config = {};
    if (!config.headers) config.headers = {};
    config.headers["Content-Type"] = "application/json";
    config.body = JSON.stringify(body);
    return await post(url, config);
}

export class UnusedMediaService {

    static async getUnusedMedia(config) {
        return await get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetUnusedMedia", config);
    };

    static async getFilters() {
        return await get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetFilters");
    }

    static async trashMedia(media) {
        if (Array.isArray(media)) {
            const keys = media.map(m => typeof m === "object" ? m.key : m);
            return await postJson("/umbraco/backoffice/api/UnusedMediaBackOffice/TrashMedia", { mediaKeys: keys });
        } else {
            const key = typeof media === "object" ? media.key : media;
            return await postJson("/umbraco/backoffice/api/UnusedMediaBackOffice/TrashMedia", { mediaKey: key });
        }
    }

    static async startScan(provider) {
        if (typeof provider === "object") provider = provider.alias;
        return await post("/umbraco/backoffice/api/UnusedMediaBackOffice/StartScan", { params: { provider } });
    }

};

export default UnusedMediaService;