function authFetch(url, options) {
    if (!options) {
        options = {};
    }
    if (!options.headers) {
        options.headers = {};
    }
    var password = sessionStorage.getItem("authPassword");
    if (password) {
        options.headers["X-Auth-Password"] = password;
    }
    return fetch(url, options).then(function (response) {
        if (response.status === 401) {
            sessionStorage.removeItem("authPassword");
            window.location.replace("/login.html");
        }
        return response;
    });
}

function initAuth(callback) {
    fetch("/api/auth/state")
        .then(function (response) {
            return response.json();
        })
        .then(function (state) {
            if (!state.setup) {
                window.location.replace("/setup.html");
                return;
            }
            var password = sessionStorage.getItem("authPassword");
            if (!password) {
                window.location.replace("/login.html");
                return;
            }
            callback();
        });
}

function logout() {
    sessionStorage.removeItem("authPassword");
    window.location.replace("/login.html");
}
