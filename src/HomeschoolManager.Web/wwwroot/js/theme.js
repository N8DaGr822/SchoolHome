window.homeschoolTheme = {
    get: () => document.documentElement.dataset.theme || "light",
    set: (theme) => {
        const normalizedTheme = theme === "dark" ? "dark" : "light";
        document.documentElement.dataset.theme = normalizedTheme;
        localStorage.setItem("homeschool-theme", normalizedTheme);
    }
};
