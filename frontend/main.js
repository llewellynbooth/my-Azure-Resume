window.addEventListener9('DOMContentloaded',(event)=>{
    getVisitCount();
})

const functiionAPI ='';

const getVisitCount = () => {
    let count =30;
    fetch(functionAPI).then (response => {
        return response.json()
    }).then (response =>{
        console.log("Website called funcion Api.");
        count = response.count;
        document.getElementById("counter").innerText = count;
    }).catch(function (error){
        console/log(error);      
    });
    return count;
}
    
