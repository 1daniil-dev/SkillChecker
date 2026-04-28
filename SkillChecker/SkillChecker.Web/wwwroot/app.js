var allTests = [];

window.onload = function () {
    loadTests();
};

function showTab(name) {
    var tabs = ["tests", "schedule", "results"];
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

    if (name === "schedule") {
        loadSettings();
    }
    if (name === "results") {
        loadResults();
    }
}

function loadTests() {
    fetch("/api/tests")
        .then(function (r) { return r.json(); })
        .then(function (tests) {
            allTests = tests;
            var div = document.getElementById("testsList");
            div.innerHTML = "";

            if (tests.length === 0) {
                div.innerHTML = '<div class="empty" role="status">Нет загруженных тестов</div>';
                return;
            }

            for (var i = 0; i < tests.length; i++) {
                var card = document.createElement("div");
                card.className = "test-card";
                card.setAttribute("role", "listitem");
                card.setAttribute("aria-label", "Тест " + tests[i].Name + ", " + tests[i].QuestionCount + " вопросов");

                var info = document.createElement("div");
                var nameSpan = document.createElement("div");
                nameSpan.className = "test-name";
                nameSpan.textContent = tests[i].Name;
                info.appendChild(nameSpan);

                var countSpan = document.createElement("div");
                countSpan.className = "test-count";
                countSpan.textContent = tests[i].QuestionCount + " вопросов";
                info.appendChild(countSpan);

                card.appendChild(info);

                var actions = document.createElement("div");
                actions.className = "test-actions";
                var delBtn = document.createElement("button");
                delBtn.textContent = "Удалить";
                delBtn.setAttribute("data-name", tests[i].Name);
                delBtn.setAttribute("aria-label", "Удалить тест " + tests[i].Name);
                delBtn.onclick = function () {
                    deleteTest(this.getAttribute("data-name"));
                };
                actions.appendChild(delBtn);
                card.appendChild(actions);

                div.appendChild(card);
            }
        });
}

function showUpload() {
    document.getElementById("uploadForm").classList.remove("hidden");
    document.getElementById("testName").focus();
}

function hideUpload() {
    document.getElementById("uploadForm").classList.add("hidden");
    document.getElementById("testName").value = "";
    document.getElementById("testFile").value = "";
}

function uploadTest() {
    var name = document.getElementById("testName").value.trim();
    var fileInput = document.getElementById("testFile");

    if (fileInput.files.length === 0) {
        alert("Выберите файл");
        return;
    }

    var formData = new FormData();
    formData.append("file", fileInput.files[0]);
    if (name.length > 0) {
        formData.append("name", name);
    }

    fetch("/api/upload", {
        method: "POST",
        body: formData
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.ok) {
            hideUpload();
            loadTests();
        } else {
            alert("Ошибка: " + (data.error || "неизвестная"));
        }
    });
}

function deleteTest(name) {
    if (!confirm("Удалить тест \"" + name + "\"?")) return;

    fetch("/api/test/" + encodeURIComponent(name), {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadTests();
    });
}

function showScheduleForm() {
    var select = document.getElementById("scheduleTestSelect");
    select.innerHTML = '<option value="">Выберите тест</option>';
    for (var i = 0; i < allTests.length; i++) {
        var opt = document.createElement("option");
        opt.value = allTests[i].Name;
        opt.textContent = allTests[i].Name;
        select.appendChild(opt);
    }

    var now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    document.getElementById("scheduleTime").value = now.toISOString().slice(0, 16);
    document.getElementById("timeLimit").value = "0";

    document.getElementById("scheduleForm").classList.remove("hidden");
    select.focus();
}

function hideScheduleForm() {
    document.getElementById("scheduleForm").classList.add("hidden");
}

function scheduleTest() {
    var testName = document.getElementById("scheduleTestSelect").value;
    var time = document.getElementById("scheduleTime").value;
    var timeLimit = parseInt(document.getElementById("timeLimit").value) || 0;

    if (!testName) {
        alert("Выберите тест");
        return;
    }

    fetch("/api/settings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ TestName: testName, StartTime: time, TimeMinutes: timeLimit })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.ok) {
            hideScheduleForm();
            loadSettings();
        }
    });
}

function loadSettings() {
    fetch("/api/settings")
        .then(function (r) { return r.json(); })
        .then(function (items) {
            var div = document.getElementById("scheduleList");
            div.innerHTML = "";

            if (items.length === 0) {
                div.innerHTML = '<div class="empty" role="status">Нет настроек тестов</div>';
                return;
            }

            for (var i = 0; i < items.length; i++) {
                var card = document.createElement("div");
                card.className = "schedule-card";
                card.setAttribute("role", "listitem");

                var info = document.createElement("div");
                info.className = "schedule-info";

                var icon = document.createElement("span");
                icon.className = "schedule-icon";
                icon.textContent = "\u{1F4CB}";
                icon.setAttribute("aria-hidden", "true");
                info.appendChild(icon);

                var textDiv = document.createElement("div");
                var nameDiv = document.createElement("div");
                nameDiv.className = "schedule-test-name";
                nameDiv.textContent = items[i].TestName;
                textDiv.appendChild(nameDiv);

                var details = [];
                if (items[i].DisplayTime && items[i].DisplayTime.length > 0) {
                    details.push("Начало: " + items[i].DisplayTime);
                }
                if (items[i].TimeMinutes > 0) {
                    details.push("Лимит: " + items[i].TimeMinutes + " мин");
                }
                if (details.length === 0) {
                    details.push("Без ограничений");
                }

                var timeDiv = document.createElement("div");
                timeDiv.className = "schedule-time";
                timeDiv.textContent = details.join(" | ");
                textDiv.appendChild(timeDiv);

                info.appendChild(textDiv);
                card.appendChild(info);

                var actions = document.createElement("div");
                actions.className = "schedule-actions";
                var delBtn = document.createElement("button");
                delBtn.textContent = "Удалить";
                delBtn.setAttribute("data-name", items[i].TestName);
                delBtn.setAttribute("aria-label", "Удалить настройки теста " + items[i].TestName);
                delBtn.onclick = function () {
                    deleteSettings(this.getAttribute("data-name"));
                };
                actions.appendChild(delBtn);
                card.appendChild(actions);

                div.appendChild(card);
            }
        });
}

function deleteSettings(testName) {
    if (!confirm("Удалить настройки для \"" + testName + "\"?")) return;

    fetch("/api/settings/" + encodeURIComponent(testName), {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadSettings();
    });
}

function loadResults() {
    fetch("/api/results")
        .then(function (r) { return r.json(); })
        .then(function (results) {
            var div = document.getElementById("resultsList");
            div.innerHTML = "";

            if (results.length === 0) {
                div.innerHTML = '<div class="empty" role="status">Пока нет результатов</div>';
                return;
            }

            for (var i = 0; i < results.length; i++) {
                var r = results[i];
                var card = document.createElement("div");
                card.className = "result-card";
                card.setAttribute("role", "listitem");
                card.setAttribute("aria-label", r.StudentName + ", группа " + r.Group + ", тест " + r.TestName + ", оценка " + r.Score + "%");

                var header = document.createElement("div");
                header.className = "result-header";

                var left = document.createElement("div");
                var nameDiv = document.createElement("div");
                nameDiv.className = "result-name";
                nameDiv.textContent = r.StudentName;
                left.appendChild(nameDiv);

                var groupDiv = document.createElement("div");
                groupDiv.className = "result-group";
                groupDiv.textContent = "Группа: " + r.Group;
                left.appendChild(groupDiv);
                header.appendChild(left);

                var scoreDiv = document.createElement("div");
                scoreDiv.className = "result-score";
                if (r.Score >= 70) scoreDiv.classList.add("badge-green");
                else if (r.Score >= 40) scoreDiv.classList.add("badge-orange");
                else scoreDiv.classList.add("badge-red");
                scoreDiv.textContent = r.Score + "%";
                header.appendChild(scoreDiv);

                card.appendChild(header);

                var meta = document.createElement("div");
                meta.className = "result-meta";

                var testSpan = document.createElement("span");
                testSpan.textContent = "Тест: " + r.TestName;
                meta.appendChild(testSpan);

                var correctSpan = document.createElement("span");
                correctSpan.textContent = "Правильно: " + r.CorrectAnswers + "/" + r.TotalQuestions;
                meta.appendChild(correctSpan);

                var dateSpan = document.createElement("span");
                var d = new Date(r.Date);
                dateSpan.textContent = d.toLocaleString("ru-RU");
                meta.appendChild(dateSpan);

                card.appendChild(meta);
                div.appendChild(card);
            }
        });
}
