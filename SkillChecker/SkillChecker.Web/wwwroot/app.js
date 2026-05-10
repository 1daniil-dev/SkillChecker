var allTests = [];
var allResults = [];
var resultsSort = { field: "date", direction: "desc" };

window.onload = function () {
    loadTests();
};

function showTab(event, name) {
    var tabs = ["tests", "results"];
    for (var i = 0; i < tabs.length; i++) {
        document.getElementById("tab-" + tabs[i]).classList.add("hidden");
    }
    document.getElementById("tab-" + name).classList.remove("hidden");

    var btns = document.querySelectorAll(".tab");
    for (var i = 0; i < btns.length; i++) {
        btns[i].classList.remove("active");
        btns[i].setAttribute("aria-selected", "false");
    }
    event.target.classList.add("active");
    event.target.setAttribute("aria-selected", "true");

    if (name === "results") {
        loadResults();
    }
}
