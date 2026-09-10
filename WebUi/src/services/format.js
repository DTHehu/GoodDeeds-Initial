/* Shared date formatting so every screen reads the same way. */

export function formatDate(value) {
    return new Date(value).toLocaleDateString(undefined, {
        weekday: "short",
        month: "short",
        day: "numeric",
    });
}

export function formatTime(value) {
    return new Date(value).toLocaleTimeString(undefined, {
        hour: "numeric",
        minute: "2-digit",
    });
}

export function formatDateTime(value) {
    return `${formatDate(value)}, ${formatTime(value)}`;
}

// "Sat, Mar 14 · 9:00 AM – 12:00 PM" when an event starts and ends on the
// same day, and the full date on both sides when it does not.
export function formatEventWhen(startTime, endTime) {
    const start = new Date(startTime);
    const end = new Date(endTime);

    if (start.toDateString() === end.toDateString()) {
        return `${formatDate(start)} · ${formatTime(start)} – ${formatTime(end)}`;
    }

    return `${formatDateTime(start)} – ${formatDateTime(end)}`;
}

export function isUpcoming(value) {
    return new Date(value).getTime() >= Date.now();
}
