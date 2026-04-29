var allTests = [];

window.onload = function () {
    loadTests();
};

function showTab(name) {
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

            var visibleTests = [];
            var hiddenTests = [];
            for (var i = 0; i < tests.length; i++) {
                if (tests[i].Visible) {
                    visibleTests.push(tests[i]);
                } else {
                    hiddenTests.push(tests[i]);
                }
            }

            var sorted = [];
            for (var i = 0; i < visibleTests.length; i++) sorted.push(visibleTests[i]);
            for (var i = 0; i < hiddenTests.length; i++) sorted.push(hiddenTests[i]);

            for (var i = 0; i < sorted.length; i++) {
                var test = sorted[i];
                var card = document.createElement("div");
                card.className = "test-card";
                if (!test.Visible) card.classList.add("test-hidden");
                if (test.HasSettings) card.classList.add("test-scheduled");
                card.setAttribute("role", "listitem");
                card.setAttribute("aria-label", "Тест " + test.Name + ", " + test.QuestionCount + " вопросов");

                var mainRow = document.createElement("div");
                mainRow.className = "test-main-row";

                var toggleLabel = document.createElement("label");
                toggleLabel.className = "toggle";
                var toggleInput = document.createElement("input");
                toggleInput.type = "checkbox";
                toggleInput.checked = test.Visible;
                toggleInput.setAttribute("data-name", test.Name);
                toggleInput.setAttribute("aria-label", "Видимость теста " + test.Name);
                toggleInput.onchange = function () {
                    toggleVisibility(this.getAttribute("data-name"), this.checked);
                };
                var toggleSpan = document.createElement("span");
                toggleSpan.className = "toggle-slider";
                toggleLabel.appendChild(toggleInput);
                toggleLabel.appendChild(toggleSpan);
                mainRow.appendChild(toggleLabel);

                var info = document.createElement("div");
                info.className = "test-info";

                var nameSpan = document.createElement("div");
                nameSpan.className = "test-name";
                nameSpan.textContent = test.Name;
                info.appendChild(nameSpan);

                var countSpan = document.createElement("div");
                countSpan.className = "test-count";
                countSpan.textContent = test.QuestionCount + " вопросов";
                info.appendChild(countSpan);

                mainRow.appendChild(info);

                var statusBadge = document.createElement("span");
                statusBadge.className = "status-badge";
                if (test.Visible) {
                    if (test.HasSettings) {
                        statusBadge.className += " badge-scheduled";
                        statusBadge.textContent = "Запланирован";
                    } else {
                        statusBadge.className += " badge-available";
                        statusBadge.textContent = "Доступен";
                    }
                } else {
                    statusBadge.className += " badge-hidden";
                    statusBadge.textContent = "Скрыт";
                }
                mainRow.appendChild(statusBadge);

                var actions = document.createElement("div");
                actions.className = "test-actions";

                var configBtn = document.createElement("button");
                configBtn.className = "btn-configure";
                configBtn.textContent = "Настроить";
                configBtn.setAttribute("data-name", test.Name);
                configBtn.setAttribute("data-visible", test.Visible ? "true" : "false");
                configBtn.setAttribute("aria-label", "Настроить тест " + test.Name);
                configBtn.onclick = function () {
                    openSettingsForTest(this.getAttribute("data-name"));
                };
                actions.appendChild(configBtn);

                var delBtn = document.createElement("button");
                delBtn.textContent = "Удалить";
                delBtn.setAttribute("data-name", test.Name);
                delBtn.setAttribute("aria-label", "Удалить тест " + test.Name);
                delBtn.onclick = function () {
                    deleteTest(this.getAttribute("data-name"));
                };
                actions.appendChild(delBtn);

                mainRow.appendChild(actions);
                card.appendChild(mainRow);

                if (test.HasSettings) {
                    var settingsRow = document.createElement("div");
                    settingsRow.className = "test-settings-row";

                    var details = [];
                    if (test.DisplayTime && test.DisplayTime.length > 0) {
                        details.push("Начало: " + test.DisplayTime);
                    }
                    if (test.TimeMinutes > 0) {
                        details.push("Лимит: " + test.TimeMinutes + " мин");
                    }
                    if (details.length === 0) {
                        details.push("Без ограничений");
                    }

                    var settingsText = document.createElement("span");
                    settingsText.className = "test-settings-text";
                    settingsText.textContent = details.join(" | ");
                    settingsRow.appendChild(settingsText);

                    var deleteSettingsBtn = document.createElement("button");
                    deleteSettingsBtn.className = "btn-delete-settings";
                    deleteSettingsBtn.textContent = "Удалить настройку";
                    deleteSettingsBtn.setAttribute("data-name", test.Name);
                    deleteSettingsBtn.setAttribute("aria-label", "Удалить настройки теста " + test.Name);
                    deleteSettingsBtn.onclick = function () {
                        deleteSettings(this.getAttribute("data-name"));
                    };
                    settingsRow.appendChild(deleteSettingsBtn);

                    card.appendChild(settingsRow);
                }

                div.appendChild(card);
            }
        });
}

function toggleVisibility(testName, visible) {
    fetch("/api/settings/" + encodeURIComponent(testName) + "/visibility", {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Visible: visible })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadTests();
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

function deleteSettings(testName) {
    if (!confirm("Удалить настройки для \"" + testName + "\"?")) return;

    fetch("/api/settings/" + encodeURIComponent(testName), {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadTests();
    });
}

function openSettingsForTest(testName) {
    var select = document.getElementById("scheduleTestSelect");
    select.innerHTML = '<option value="">Выберите тест</option>';
    for (var i = 0; i < allTests.length; i++) {
        var opt = document.createElement("option");
        opt.value = allTests[i].Name;
        opt.textContent = allTests[i].Name;
        if (allTests[i].Name === testName) opt.selected = true;
        select.appendChild(opt);
    }

    var test = null;
    for (var i = 0; i < allTests.length; i++) {
        if (allTests[i].Name === testName) {
            test = allTests[i];
            break;
        }
    }

    if (test && test.HasSettings && test.DisplayTime && test.DisplayTime.length > 0) {
        var dateObj = null;
        for (var i = 0; i < allTests.length; i++) {
            if (allTests[i].Name === testName) {
                fetch("/api/settings")
                    .then(function (r) { return r.json(); })
                    .then(function (items) {
                        for (var j = 0; j < items.length; j++) {
                            if (items[j].TestName === testName) {
                                if (items[j].StartTime && items[j].StartTime.length > 0) {
                                    var d = new Date(items[j].StartTime);
                                    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
                                    document.getElementById("scheduleTime").value = d.toISOString().slice(0, 16);
                                }
                                document.getElementById("timeLimit").value = items[j].TimeMinutes;
                                break;
                            }
                        }
                    });
                break;
            }
        }
    } else {
        var now = new Date();
        now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
        document.getElementById("scheduleTime").value = now.toISOString().slice(0, 16);
        document.getElementById("timeLimit").value = "0";
    }

    document.getElementById("settingsFormTitle").textContent = "Настройка: " + testName;
    document.getElementById("settingsForm").classList.remove("hidden");
    select.focus();
}

function hideSettingsForm() {
    document.getElementById("settingsForm").classList.add("hidden");
}

function saveSettings() {
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
            hideSettingsForm();
            loadTests();
        }
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
