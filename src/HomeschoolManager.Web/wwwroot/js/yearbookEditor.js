window.yearbookEditor = (() => {
    const controllers = new WeakMap();
    const minWidth = 32;
    const minHeight = 24;

    function initialize(canvas, dotNetRef) {
        if (!canvas) {
            return;
        }

        dispose(canvas);

        const controller = {
            canvas,
            dotNetRef,
            onPointerDown: null
        };

        controller.onPointerDown = (event) => startInteraction(event, controller);
        canvas.addEventListener("pointerdown", controller.onPointerDown);
        controllers.set(canvas, controller);
    }

    function dispose(canvas) {
        const controller = controllers.get(canvas);
        if (!controller) {
            return;
        }

        controller.canvas.removeEventListener("pointerdown", controller.onPointerDown);
        controllers.delete(canvas);
    }

    function startInteraction(event, controller) {
        if (event.button !== 0 || isInteractiveTarget(event.target)) {
            return;
        }

        const handle = event.target.closest("[data-resize-handle]");
        const element = event.target.closest(".editable-element");
        if (!element || !controller.canvas.contains(element) || !element.classList.contains("editable-element-selected")) {
            return;
        }

        const elementId = element.dataset.elementId;
        if (!elementId) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        element.setPointerCapture(event.pointerId);

        const start = {
            pointerX: event.clientX,
            pointerY: event.clientY,
            x: readPixels(element.style.left),
            y: readPixels(element.style.top),
            width: element.offsetWidth,
            height: element.offsetHeight,
            handle: handle?.dataset.resizeHandle || null
        };

        const onPointerMove = (moveEvent) => {
            if (start.handle) {
                resizeElement(controller.canvas, element, start, moveEvent);
            } else {
                dragElement(controller.canvas, element, start, moveEvent);
            }
        };

        const onPointerUp = async (upEvent) => {
            element.removeEventListener("pointermove", onPointerMove);
            element.removeEventListener("pointerup", onPointerUp);
            element.removeEventListener("pointercancel", onPointerUp);

            if (element.hasPointerCapture(upEvent.pointerId)) {
                element.releasePointerCapture(upEvent.pointerId);
            }

            await controller.dotNetRef.invokeMethodAsync("OnElementMovedOrResized", {
                id: elementId,
                x: readPixels(element.style.left),
                y: readPixels(element.style.top),
                width: readPixels(element.style.width),
                height: readPixels(element.style.height)
            });
        };

        element.addEventListener("pointermove", onPointerMove);
        element.addEventListener("pointerup", onPointerUp);
        element.addEventListener("pointercancel", onPointerUp);
    }

    function dragElement(canvas, element, start, event) {
        const dx = event.clientX - start.pointerX;
        const dy = event.clientY - start.pointerY;
        const nextX = clamp(start.x + dx, 0, canvas.clientWidth - element.offsetWidth);
        const nextY = clamp(start.y + dy, 0, canvas.clientHeight - element.offsetHeight);

        element.style.left = `${nextX}px`;
        element.style.top = `${nextY}px`;
    }

    function resizeElement(canvas, element, start, event) {
        const dx = event.clientX - start.pointerX;
        const dy = event.clientY - start.pointerY;
        let x = start.x;
        let y = start.y;
        let width = start.width;
        let height = start.height;

        if (start.handle.includes("e")) {
            width = start.width + dx;
        }

        if (start.handle.includes("s")) {
            height = start.height + dy;
        }

        if (start.handle.includes("w")) {
            x = start.x + dx;
            width = start.width - dx;
        }

        if (start.handle.includes("n")) {
            y = start.y + dy;
            height = start.height - dy;
        }

        if (width < minWidth) {
            if (start.handle.includes("w")) {
                x -= minWidth - width;
            }

            width = minWidth;
        }

        if (height < minHeight) {
            if (start.handle.includes("n")) {
                y -= minHeight - height;
            }

            height = minHeight;
        }

        x = clamp(x, 0, canvas.clientWidth - minWidth);
        y = clamp(y, 0, canvas.clientHeight - minHeight);
        width = clamp(width, minWidth, canvas.clientWidth - x);
        height = clamp(height, minHeight, canvas.clientHeight - y);

        element.style.left = `${x}px`;
        element.style.top = `${y}px`;
        element.style.width = `${width}px`;
        element.style.height = `${height}px`;
    }

    function isInteractiveTarget(target) {
        const interactive = target.closest("input, textarea, select, button, [contenteditable='true'], [contenteditable='']");
        return Boolean(interactive);
    }

    function readPixels(value) {
        const parsed = Number.parseFloat(value || "0");
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), Math.max(min, max));
    }

    return {
        initialize,
        dispose
    };
})();
