// Applies the saved or system colour theme before first paint. Loaded in <head>.
(() => {
    'use strict';
    let saved = null;
    try {
        saved = localStorage.getItem('theme');
    } catch {
        // Storage can be blocked; fall back to the system setting.
    }
    const dark = saved ? saved === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches;
    const apply = () => document.body.classList.add(dark ? 'dark' : 'light');
    if (document.body) {
        apply();
    } else {
        document.addEventListener('DOMContentLoaded', apply);
    }
})();
