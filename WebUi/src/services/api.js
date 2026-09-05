const BASE = "http://localhost:5160/api"

export function saveTokens(accessToken, refreshToken) {
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
}

export function getAccessToken() {
    return localStorage.getItem("access_token"); // null if not there
}

export function isLoggedIn() {
    return getAccessToken() !== null;
}

export function saveDashboardPath(path) {
    localStorage.setItem("dashboard_path", path);
}

export function getDashboardPath() {
    return localStorage.getItem("dashboard_path") || "/vol-dashboard";
}

export function clearTokens() {
    localStorage.removeItem("access_token");
    localStorage.removeItem("refresh_token");
    localStorage.removeItem("dashboard_path");
}

// Pulls a readable sentence out of whatever the API sent back. Validation
// failures arrive as { errors: { Field: ["message"] } }, other failures as a
// problem document or as plain text.
function describeFailure(data, text, status) {
    if (typeof data === "string" && data.length > 0) {
        return data;
    }

    if (data && data.errors) {
        const messages = Object.values(data.errors).flat();
        if (messages.length > 0) {
            return messages.join(" ");
        }
    }

    if (data && data.detail) return data.detail;
    if (data && data.title) return data.title;
    if (text) return text;

    return "Request failed with status " + status;
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
        const text = await response.text();

        let data;
        try {
            data = JSON.parse(text);
        } catch {
            data = null;
        }

        const error = new Error(describeFailure(data, text, response.status));
        error.status = response.status;
        error.data = data;

        throw error;
    }

    // 204, and 200 from an endpoint like register that returns no body.
    const responseBody = await response.text();

    return responseBody ? JSON.parse(responseBody) : null;
}

export const api = {
    get: (path) => callApi(path, "GET"),
    post: (path, body) => callApi(path, "POST", body),
    put: (path, body) => callApi(path, "PUT", body),
    del: (path) => callApi(path, "DELETE"),
};
