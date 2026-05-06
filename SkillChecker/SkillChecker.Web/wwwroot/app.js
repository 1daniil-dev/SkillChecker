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

                var previewBtn = document.createElement("button");
                previewBtn.className = "btn-preview";
                previewBtn.textContent = "Просмотр";
                previewBtn.setAttribute("data-name", test.Name);
                previewBtn.setAttribute("aria-label", "Предпросмотр теста " + test.Name);
                previewBtn.onclick = function () {
                    previewTest(this.getAttribute("data-name"));
                };
                actions.appendChild(previewBtn);

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

function clearResults() {
    if (!confirm("Удалить все результаты тестирования?")) return;

    fetch("/api/results", {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadResults();
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
                header.className = "result-header result-clickable";
                header.setAttribute("data-index", i);
                header.onclick = function () {
                    toggleDetails(this.getAttribute("data-index"));
                };

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

                var rightGroup = document.createElement("div");
                rightGroup.className = "result-right";

                var scoreDiv = document.createElement("div");
                scoreDiv.className = "result-score";
                if (r.Score >= 70) scoreDiv.classList.add("badge-green");
                else if (r.Score >= 40) scoreDiv.classList.add("badge-orange");
                else scoreDiv.classList.add("badge-red");
                scoreDiv.textContent = r.Score + "%";
                rightGroup.appendChild(scoreDiv);

                var expandIcon = document.createElement("span");
                expandIcon.className = "expand-icon";
                expandIcon.id = "expand-icon-" + i;
                expandIcon.textContent = "\u25B6";
                rightGroup.appendChild(expandIcon);

                header.appendChild(rightGroup);

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

                var details = document.createElement("div");
                details.className = "result-details hidden";
                details.id = "result-details-" + i;

                if (r.Answers && r.Answers.length > 0) {
                    for (var j = 0; j < r.Answers.length; j++) {
                        var a = r.Answers[j];
                        var qDiv = document.createElement("div");
                        qDiv.className = "question-detail";
                        if (a.IsCorrect) {
                            qDiv.classList.add("question-correct");
                        } else {
                            qDiv.classList.add("question-wrong");
                        }

                        var qNum = document.createElement("span");
                        qNum.className = "question-num";
                        qNum.textContent = (j + 1) + ".";
                        qDiv.appendChild(qNum);

                        var qText = document.createElement("span");
                        qText.className = "question-text";
                        qText.textContent = a.QuestionText;
                        qDiv.appendChild(qText);

                        var qStatus = document.createElement("span");
                        qStatus.className = "question-status";
                        if (a.IsCorrect) {
                            qStatus.textContent = "\u2713";
                        } else {
                            qStatus.textContent = "\u2717";
                        }
                        qDiv.appendChild(qStatus);

                        if (!a.IsCorrect) {
                            var answerInfo = document.createElement("div");
                            answerInfo.className = "answer-info";

                            if (a.QuestionType === "Multiple") {
                                var selText = a.SelectedIndices && a.SelectedIndices.length > 0 ? a.SelectedIndices.join(", ") : "нет ответа";
                                answerInfo.textContent = "Выбрал: " + selText + " | Правильно: " + a.CorrectIndex;
                            } else {
                                var selText = a.SelectedIndex >= 0 ? a.SelectedIndex : "нет ответа";
                                answerInfo.textContent = "Выбрал: " + selText + " | Правильно: " + a.CorrectIndex;
                            }

                            qDiv.appendChild(answerInfo);
                        }

                        details.appendChild(qDiv);
                    }
                }

                card.appendChild(details);
                div.appendChild(card);
            }
        });
}

function toggleDetails(index) {
    var details = document.getElementById("result-details-" + index);
    var icon = document.getElementById("expand-icon-" + index);
    if (details.classList.contains("hidden")) {
        details.classList.remove("hidden");
        icon.textContent = "\u25BC";
    } else {
        details.classList.add("hidden");
        icon.textContent = "\u25B6";
    }
}

function previewTest(name) {
    var existing = document.getElementById("preview-" + name);
    if (existing) {
        existing.parentElement.removeChild(existing);
        return;
    }

    fetch("/api/test/" + encodeURIComponent(name) + "/preview")
        .then(function (r) { return r.json(); })
        .then(function (questions) {
            var card = null;
            var cards = document.getElementById("testsList").children;
            for (var i = 0; i < cards.length; i++) {
                var btns = cards[i].querySelectorAll(".btn-preview");
                for (var j = 0; j < btns.length; j++) {
                    if (btns[j].getAttribute("data-name") === name) {
                        card = cards[i];
                        break;
                    }
                }
                if (card) break;
            }
            if (!card) return;

            var previewDiv = document.createElement("div");
            previewDiv.className = "preview-inline";
            previewDiv.id = "preview-" + name;

            for (var i = 0; i < questions.length; i++) {
                var q = questions[i];
                var qBlock = document.createElement("div");
                qBlock.className = "preview-question";

                var qHeader = document.createElement("div");
                qHeader.className = "preview-q-header";

                var qNum = document.createElement("span");
                qNum.className = "preview-q-num";
                qNum.textContent = (i + 1) + ".";
                qHeader.appendChild(qNum);

                var qText = document.createElement("span");
                qText.className = "preview-q-text";
                qText.textContent = q.Text;
                qHeader.appendChild(qText);

                if (q.Type && q.Type === "Multiple") {
                    var typeLabel = document.createElement("span");
                    typeLabel.className = "preview-type-label";
                    typeLabel.textContent = "Множественный";
                    qHeader.appendChild(typeLabel);
                }

                qBlock.appendChild(qHeader);

                var optionsList = document.createElement("div");
                optionsList.className = "preview-options";

                for (var j = 0; j < q.Options.length; j++) {
                    var optDiv = document.createElement("div");
                    optDiv.className = "preview-option";
                    if (q.Type === "Multiple") {
                        if (q.CorrectAnswerIndices && q.CorrectAnswerIndices.indexOf(j) >= 0) {
                            optDiv.classList.add("preview-correct");
                        }
                    } else {
                        if (j === q.CorrectAnswerIndex) {
                            optDiv.classList.add("preview-correct");
                        }
                    }

                    var optLabel = document.createElement("span");
                    optLabel.className = "preview-opt-letter";
                    optLabel.textContent = String.fromCharCode(1040 + j);
                    optDiv.appendChild(optLabel);

                    var optText = document.createElement("span");
                    optText.textContent = q.Options[j];
                    optDiv.appendChild(optText);

                    optionsList.appendChild(optDiv);
                }

                qBlock.appendChild(optionsList);
                previewDiv.appendChild(qBlock);
            }

            card.appendChild(previewDiv);
        });
}

function hidePreview() {
}
