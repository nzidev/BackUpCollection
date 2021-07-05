// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var popup = document.getElementById("PopupTable");
var span = document.getElementsByClassName("close")[0];



function clickDropDown() {
    window.document.getElementById('SearchButton').click();
}

function prev(e) {
   
    window.document.getElementById("page").value = e;
    window.document.getElementById("SearchButton").click();
}

function next(e) {
    
    window.document.getElementById("page").value = e;
    window.document.getElementById("SearchButton").click();
}


// When the user clicks the button, open the modal
function showPopup(client) {

    var url = 'Home/ClientInfo?clientName=' + client;
    var params = `width=550,height=300, top=140`;
    newwindow = window.open(url, 'ff', params);
    
    if (window.focus) { newwindow.focus() }
    return false;
}

span.onclick = function () {
    popup.style.display = "none";
}

// When the user clicks anywhere outside of the modal, close it
window.onclick = function (event) {
    if (event.target == popup) {
        popup.style.display = "none";
    }
}


 //$("#data-body").hide();

    