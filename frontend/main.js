// Performance: Load visitor count
window.addEventListener('DOMContentLoaded', (event) => {
    getVisitCount();
    initDarkMode();
});

const functionApiUrl = 'https://resumefunctionapp-win-cqczeqc6d5gtdfbb.australiaeast-01.azurewebsites.net/api/getResumeFunction';
const localfunctionApi = 'http://localhost:7071/api/GetResumeFunction';

const getVisitCount = () => {
    const counterElement = document.getElementById('counter');

    // Show loading state
    if (counterElement) {
        counterElement.innerText = '...';
        counterElement.setAttribute('aria-live', 'polite');
    }

    fetch(functionApiUrl, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log("Website called function API.");
        const count = data.count || 0;
        if (counterElement) {
            counterElement.innerText = count.toLocaleString();
        }
    })
    .catch(error => {
        console.error('Error fetching visitor count:', error);
        if (counterElement) {
            counterElement.innerText = 'N/A';
            counterElement.title = 'Unable to load visitor count';
        }
    });
};

// Dark Mode Toggle
const initDarkMode = () => {
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
        document.documentElement.classList.add('dark-mode');
    }
};

const toggleDarkMode = () => {
    const isDark = document.documentElement.classList.toggle('dark-mode');
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
};