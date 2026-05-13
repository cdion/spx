const timelineObservers = new WeakMap();

function isObserverRoot(value) {
    return value instanceof Element || value instanceof Document;
}

export function setTimelineObserver(container, sentinel, dotNetRef) {
    if (sentinel instanceof Element) {
        disposeTimelineObserver(sentinel);
    }

    if (!isObserverRoot(container) || !(sentinel instanceof Element) || !dotNetRef) {
        return;
    }

    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            dotNetRef.invokeMethodAsync("HandleTimelineNearTopAsync");
        }
    }, {
        root: container,
        threshold: 0.1
    });

    observer.observe(sentinel);
    timelineObservers.set(sentinel, observer);
}

export function disposeTimelineObserver(sentinel) {
    if (!(sentinel instanceof Element)) {
        return;
    }

    const observer = timelineObservers.get(sentinel);
    if (!observer) {
        return;
    }

    observer.disconnect();
    timelineObservers.delete(sentinel);
}

export function getScrollMetrics(container) {
    if (!container) {
        return {
            scrollTop: 0,
            scrollHeight: 0,
            clientHeight: 0
        };
    }

    return {
        scrollTop: container.scrollTop,
        scrollHeight: container.scrollHeight,
        clientHeight: container.clientHeight
    };
}

export function restoreScrollAfterPrepend(container, previousScrollHeight, previousScrollTop) {
    if (!container) {
        return;
    }

    container.scrollTop = container.scrollHeight - previousScrollHeight + previousScrollTop;
}

export function scrollToBottom(container) {
    if (!container) {
        return;
    }

    container.scrollTop = container.scrollHeight;
}

export function isNearBottom(container, threshold = 96) {
    if (!container) {
        return true;
    }

    return container.scrollHeight - container.scrollTop - container.clientHeight <= threshold;
}