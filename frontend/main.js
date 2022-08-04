window.addEventListener('DOMContentLoaded', (event) => {
    getVisitCount();
});
const functionApiUrl = 'https://getresumefunctionapp.azurewebsites.net/api/getResumeFunction?code=PtIMqehc0n2nh_b3Y1jaNLp13etnryqqfxjQwrkFonGJAzFurcN-0g==';
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
      });
    return count;
}