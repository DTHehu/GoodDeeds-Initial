const BASE = " http://localhost:5160"

export function saveTokens(accessToken, refreshToken) {
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
}

export function getAccessToken() {
    return localStorage.getItem("access_token"); // null if not there
}

export function clearTokens() {
    localStorage.removeItem("access_token");
    localStorage.removeItem("refresh_token");
}

async function tryRefresh() {
    const refreshToken = localStorage.getItem("refresh_token");
    if (!refreshToken) return false;

    const response = await fetch(BASE + "/auth/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: refreshToken }),
    });

    if (!response.ok) return false;

    const data = await response.json();
    saveTokens(data.accessToken, data.refreshToken);
    return true;
}

export async function callApi(path, method, body, isRetry) {
    const token = getAccessToken();

    const headers = { "Content-Type": "application/json" };
    if (token) {
        headers.Authorization = "Bearer " + token;
    }

    const response = await fetch(BASE + path, {
        method: method || "GET",
        headers: headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    // 401 means the access token expired. Get a new one, then try again once.
    if (response.status === 401 && !isRetry) {
        const refreshed = await tryRefresh();
        if (refreshed) {
            return callApi(path, method, body, true);
        }
        clearTokens();
        throw new Error("Please log in again");
    }

    if (!response.ok) {
        throw new Error("Request failed: " + response.status);
    }

    if (response.status === 204) return null; // 204 = success, no content
    return await response.json();
}

export const api = {
    get: (path) => callApi(path, "GET"),
    post: (path, body) => callApi(path, "POST", body),
    put: (path, body) => callApi(path, "PUT", body),
    del: (path) => callApi(path, "DELETE"),
};