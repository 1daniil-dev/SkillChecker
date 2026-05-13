window.onload = function () {
    fetch("/api/auth/state")
        .then(function (response) {
            return response.json();
        })
        .then(function (state) {
            if (state.setup) {
                window.location.replace("/login.html");
            }
        });

    document.getElementById("setupPasswordConfirm").addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
            doSetup();
        }
    });
};

function doSetup() {
    var password = document.getElementById("setupPassword").value;
    var confirmPassword = document.getElementById("setupPasswordConfirm").value;
    var errorDiv = document.getElementById("setupError");
    errorDiv.classList.add("hidden");

    if (password.length < 4) {
        errorDiv.textContent = "Пароль должен быть не менее 4 символов";
        errorDiv.classList.remove("hidden");
        return;
    }

    if (password !== confirmPassword) {
        errorDiv.textContent = "Пароли не совпадают";
        errorDiv.classList.remove("hidden");
        return;
    }

    fetch("/api/auth/setup", {
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
                errorDiv.textContent = data.error || "Ошибка сохранения пароля";
                errorDiv.classList.remove("hidden");
            }
        });
}
