window.onload = function () {
    fetch("/api/auth/state")
        .then(function (response) {
            return response.json();
        })
        .then(function (state) {
            if (!state.setup) {
                window.location.replace("/setup.html");
            }
        });

    document.getElementById("loginPassword").addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
            doLogin();
        }
    });
};

function doLogin() {
    var password = document.getElementById("loginPassword").value;
    var errorDiv = document.getElementById("loginError");
    errorDiv.classList.add("hidden");

    if (password.length === 0) {
        errorDiv.textContent = "Введите пароль";
        errorDiv.classList.remove("hidden");
        return;
    }

    fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Password: password })
    })
        .then(function (response) {
            return response.json();
        })
        .then(function (data) {
            if (data.ok) {
                sessionStorage.setItem("authPassword", password);
                window.location.replace("/index.html");
            } else {
                errorDiv.textContent = "Неверный пароль";
                errorDiv.classList.remove("hidden");
            }
        });
}
