window.homeschoolTheme = {
    get: () => document.documentElement.dataset.theme || "light",
    set: (theme) => {
        const normalizedTheme = theme === "dark" ? "dark" : "light";
        document.documentElement.dataset.theme = normalizedTheme;
        try {
            localStorage.setItem("homeschool-theme", normalizedTheme);
        } catch {
            // Some browser privacy modes can block storage; the visible theme can still update.
        }

        window.homeschoolTheme.syncControls();
    },
    toggle: () => {
        const nextTheme = window.homeschoolTheme.get() === "dark" ? "light" : "dark";
        window.homeschoolTheme.set(nextTheme);
    },
    syncControls: () => {
        const isDark = window.homeschoolTheme.get() === "dark";
        document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
            button.setAttribute("aria-pressed", isDark ? "true" : "false");
        });

        document.querySelectorAll("[data-theme-label]").forEach((label) => {
            label.textContent = isDark ? "Light" : "Dark";
        });

        document.querySelectorAll("[data-theme-icon]").forEach((icon) => {
            icon.classList.toggle("oi-moon", !isDark);
            icon.classList.toggle("oi-sun", isDark);
        });
    },
    init: () => {
        window.homeschoolTheme.syncControls();
    },
};

window.homeschoolTheme.init();
