// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// site.js - Theme Toggler Logic

(function () {
    const themeToggleBtn = document.getElementById('themeToggle');
    const currentTheme = localStorage.getItem('theme');
    const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');

    // Function to apply the theme
    function applyTheme(theme) {
        if (theme === 'dark') {
            document.body.setAttribute('data-theme', 'dark');
        } else {
            document.body.removeAttribute('data-theme');
        }
        // Optionally, update the button text/icon here if you want to show only one icon (sun or moon)
        // For example: themeToggleBtn.textContent = theme === 'dark' ? '☀️' : '🌙';
    }

    // Apply stored theme or preferred scheme on initial load
    if (currentTheme) {
        applyTheme(currentTheme);
    } else if (prefersDarkScheme.matches) {
        applyTheme('dark');
        // No localStorage.setItem here, so it only applies if no explicit choice was made
    } else {
        applyTheme('light'); // Default to light if no preference and no stored theme
    }


    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            let theme = 'light'; // Default to light if switching from dark or no theme
            if (!document.body.hasAttribute('data-theme') || document.body.getAttribute('data-theme') === 'light') {
                // If current is light (or no attribute), switch to dark
                theme = 'dark';
            }
            // else if current is dark, it will switch to light (as theme is already 'light' by default)

            applyTheme(theme);
            localStorage.setItem('theme', theme);
        });
    }

    // Optional: Listen for changes in OS theme preference
    prefersDarkScheme.addEventListener('change', function(e) {
        const storedTheme = localStorage.getItem('theme');
        if (!storedTheme) { // Only apply OS preference if user hasn't made an explicit choice
            applyTheme(e.matches ? 'dark' : 'light');
        }
    });

})(); // IIFE to encapsulate the logic
