// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// site.js - Theme Toggler Logic (Updated for Checkbox Switch)

(function () {
    const themeSwitchCheckbox = document.getElementById('themeSwitchCheckbox');
    // const currentTheme = localStorage.getItem('theme'); // Already defined if keeping structure
    // const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)'); // Already defined

    // Function to apply the theme to the body and set checkbox state
    function applyThemeAndToggleState(theme) {
        if (theme === 'dark') {
            document.body.setAttribute('data-theme', 'dark');
            if (themeSwitchCheckbox) themeSwitchCheckbox.checked = true;
        } else {
            document.body.removeAttribute('data-theme');
            if (themeSwitchCheckbox) themeSwitchCheckbox.checked = false;
        }
    }

    // Determine initial theme (keep existing logic for this part but use new apply function)
    let initialTheme = 'light'; // Default
    const storedTheme = localStorage.getItem('theme');
    const osPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (storedTheme) {
        initialTheme = storedTheme;
    } else if (osPrefersDark) {
        initialTheme = 'dark';
    }
    applyThemeAndToggleState(initialTheme); // Apply it and set checkbox


    // Event Listener for the new checkbox switch
    if (themeSwitchCheckbox) {
        themeSwitchCheckbox.addEventListener('change', function(event) {
            const newTheme = event.target.checked ? 'dark' : 'light';
            applyThemeAndToggleState(newTheme);
            localStorage.setItem('theme', newTheme);
        });
    }

    // Optional: Listen for changes in OS theme preference (keep existing logic for this)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
        const storedTheme = localStorage.getItem('theme');
        if (!storedTheme) { // Only apply OS preference if user hasn't made an explicit choice
            applyThemeAndToggleState(e.matches ? 'dark' : 'light');
        }
    });

})(); // IIFE
