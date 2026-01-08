window.addEventListener('DOMContentLoaded', (event) => {
    getVisitCount();
});

const functionApiUrl = 'https://resumefunctionapp-win-cqczeqc6d5gtdfbb.australiaeast-01.azurewebsites.net/api/getResumeFunction';
const localfunctionApi = 'http://localhost:7071/api/GetResumeFunction';

const getVisitCount = () => {
    let count = 30;

    fetch(functionApiUrl)
    .then(response => {
        return response.json()
    })
    .then(response => {
        console.log("Website called function API.");
        count = response.count;
        document.getElementById('counter').innerText = count;
    }).catch(function(error) {
        console.log(error);
        document.getElementById('counter').innerText = 'Error loading count';
      });
    return count;
}