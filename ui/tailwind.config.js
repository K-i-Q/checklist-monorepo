/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
  extend: {
    fontFamily: {
      sans: ['Inter', 'ui-sans-serif', 'system-ui', 'Segoe UI', 'Roboto', 'Helvetica', 'Arial', 'Apple Color Emoji', 'Segoe UI Emoji'],
    },
    colors: {
      brand: {
        primary: '#2563eb',
        accent:  '#9333ea',
        success: '#059669',
        danger:  '#dc2626',
      }
    }
  },
},
  plugins: [],
};
