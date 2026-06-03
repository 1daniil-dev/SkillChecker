function deleteResult(fileName, studentName) {
    if (!confirm("Удалить результат \"" + studentName + "\"?")) return;

    authFetch("/api/results/" + encodeURIComponent(fileName), {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadResults();
    });
}

function clearResults() {
    if (!confirm("Удалить все результаты тестирования?")) return;

    authFetch("/api/results", {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadResults();
    });
}

function loadResults() {
    authFetch("/api/results")
        .then(function (r) { return r.json(); })
        .then(function (results) {
            allResults = results;
            renderResults();
        });
}

function changeSort(field) {
    if (resultsSort.field === field) {
        if (resultsSort.direction === "desc") {
            resultsSort.direction = "asc";
        }
        else {
            resultsSort.direction = "desc";
        }
    }
    else {
        resultsSort.field = field;
        resultsSort.direction = "desc";
    }
    renderResults();
}

function getSortValue(result) {
    if (resultsSort.field === "score") {
        return result.Score;
    }
    return new Date(result.Date).getTime();
}

function sortResults(list) {
    var copy = [];
    for (var k = 0; k < list.length; k++) {
        copy.push(list[k]);
    }
    copy.sort(function (a, b) {
        var aValue = getSortValue(a);
        var bValue = getSortValue(b);
        if (resultsSort.direction === "desc") {
            return bValue - aValue;
        }
        return aValue - bValue;
    });
    return copy;
}

function updateSortButtons() {
    var dateBtn = document.getElementById("sortByDate");
    var scoreBtn = document.getElementById("sortByScore");
    var dateArrow = dateBtn.querySelector(".sort-arrow");
    var scoreArrow = scoreBtn.querySelector(".sort-arrow");
    var arrow;
    if (resultsSort.direction === "desc") {
        arrow = "\u25BC";
    }
    else {
        arrow = "\u25B2";
    }
    if (resultsSort.field === "date") {
        dateBtn.classList.add("sort-active");
        scoreBtn.classList.remove("sort-active");
        dateArrow.textContent = arrow;
        scoreArrow.textContent = "";
    }
    else {
        scoreBtn.classList.add("sort-active");
        dateBtn.classList.remove("sort-active");
        scoreArrow.textContent = arrow;
        dateArrow.textContent = "";
    }
}

function applySearch() {
    var q = document.getElementById("searchInput").value.toLowerCase();
    if (!q) { renderResults(); return; }
    var filtered = [];
    for (var i = 0; i < allResults.length; i++) {
        var r = allResults[i];
        if (r.StudentName.toLowerCase().indexOf(q) >= 0 ||
            r.Group.toLowerCase().indexOf(q) >= 0 ||
            r.TestName.toLowerCase().indexOf(q) >= 0) {
            filtered.push(r);
        }
    }
    renderResults(filtered);
}

function renderResults(list) {
    var results = list || allResults;
    var div = document.getElementById("resultsList");
    div.innerHTML = "";
    var sortDiv = document.getElementById("resultsSort");

    if (results.length === 0) {
        div.innerHTML = '<div class="empty" role="status">Пока нет результатов</div>';
        document.getElementById("resultsStats").classList.add("hidden");
        sortDiv.classList.add("hidden");
        return;
    }

    sortDiv.classList.remove("hidden");
    updateSortButtons();

    var totalScore = 0;
    var bestScore = 0;
    var worstScore = 101;
    for (var s = 0; s < results.length; s++) {
        totalScore += results[s].Score;
        if (results[s].Score > bestScore) bestScore = results[s].Score;
        if (results[s].Score < worstScore) worstScore = results[s].Score;
    }
    var avgScore = (totalScore / results.length).toFixed(1);

    var statsDiv = document.getElementById("resultsStats");
    statsDiv.classList.remove("hidden");
    statsDiv.innerHTML = "<div class='stat-item'><span class='stat-value'>" + results.length + "</span><span class='stat-label'>Результатов</span></div>";

    var sorted = sortResults(results);

    for (var i = 0; i < sorted.length; i++) {
                var r = sorted[i];
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

                var deleteBtn = document.createElement("button");
                deleteBtn.className = "btn-delete-result";
                deleteBtn.textContent = "Удалить";
                deleteBtn.setAttribute("data-filename", r.FileName);
                deleteBtn.setAttribute("data-name", r.StudentName);
                deleteBtn.onclick = function () {
                    deleteResult(this.getAttribute("data-filename"), this.getAttribute("data-name"));
                };
                meta.appendChild(deleteBtn);

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

var exportFiltered = [];

function openExportModal() {
    if (allResults.length === 0) {
        alert("Нет результатов для экспорта");
        return;
    }

    var groups = {};
    for (var i = 0; i < allResults.length; i++) {
        var g = allResults[i].Group.trim();
        if (g.length === 0) g = "(без группы)";
        if (!groups[g]) groups[g] = 0;
        groups[g]++;
    }

    var groupsDiv = document.getElementById("exportGroups");
    groupsDiv.innerHTML = "";
    var groupNames = Object.keys(groups);
    groupNames.sort();

    for (var i = 0; i < groupNames.length; i++) {
        var label = document.createElement("label");
        label.className = "export-group-label";
        var cb = document.createElement("input");
        cb.type = "checkbox";
        cb.checked = true;
        cb.value = groupNames[i];
        cb.onchange = function () { filterExportResults(); };
        label.appendChild(cb);
        var text = document.createTextNode(" " + groupNames[i] + " (" + groups[groupNames[i]] + ")");
        label.appendChild(text);
        groupsDiv.appendChild(label);
    }

    filterExportResults();
    document.getElementById("exportModal").classList.remove("hidden");
}

function closeExportModal() {
    document.getElementById("exportModal").classList.add("hidden");
}

function getSelectedGroups() {
    var checkboxes = document.getElementById("exportGroups").querySelectorAll("input[type=checkbox]");
    var selected = [];
    for (var i = 0; i < checkboxes.length; i++) {
        if (checkboxes[i].checked) selected.push(checkboxes[i].value);
    }
    return selected;
}

function filterExportResults() {
    var selected = getSelectedGroups();
    exportFiltered = [];
    for (var i = 0; i < allResults.length; i++) {
        var g = allResults[i].Group.trim();
        if (g.length === 0) g = "(без группы)";
        var found = false;
        for (var j = 0; j < selected.length; j++) {
            if (selected[j] === g) { found = true; break; }
        }
        if (found) exportFiltered.push(allResults[i]);
    }

    var div = document.getElementById("exportResults");
    div.innerHTML = "";

    for (var i = 0; i < exportFiltered.length; i++) {
        var r = exportFiltered[i];
        var row = document.createElement("label");
        row.className = "export-result-row";
        var cb = document.createElement("input");
        cb.type = "checkbox";
        cb.checked = true;
        cb.className = "export-result-cb";
        cb.setAttribute("data-filename", r.FileName);
        cb.onchange = function () { updateExportCount(); };
        row.appendChild(cb);
        var text = document.createTextNode(
            " " + r.StudentName + " — " + r.Group + " — " + r.TestName + " — " + r.Score + "%"
        );
        row.appendChild(text);
        div.appendChild(row);
    }

    updateExportCount();
}

function updateExportCount() {
    var checkboxes = document.getElementById("exportResults").querySelectorAll(".export-result-cb");
    var count = 0;
    for (var i = 0; i < checkboxes.length; i++) {
        if (checkboxes[i].checked) count++;
    }
    document.getElementById("exportCount").textContent = "Выбрано: " + count + " из " + exportFiltered.length;
}

function selectAllExport() {
    var checkboxes = document.getElementById("exportResults").querySelectorAll(".export-result-cb");
    for (var i = 0; i < checkboxes.length; i++) {
        checkboxes[i].checked = true;
    }
    updateExportCount();
}

function deselectAllExport() {
    var checkboxes = document.getElementById("exportResults").querySelectorAll(".export-result-cb");
    for (var i = 0; i < checkboxes.length; i++) {
        checkboxes[i].checked = false;
    }
    updateExportCount();
}

function doExport() {
    var checkboxes = document.getElementById("exportResults").querySelectorAll(".export-result-cb");
    var fileNames = [];
    for (var i = 0; i < checkboxes.length; i++) {
        if (checkboxes[i].checked) {
            fileNames.push(checkboxes[i].getAttribute("data-filename"));
        }
    }

    if (fileNames.length === 0) {
        alert("Выберите хотя бы один результат");
        return;
    }

    authFetch("/api/results/export", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ FileNames: fileNames })
    })
    .then(function (r) {
        if (!r.ok) {
            return r.json().then(function (data) {
                alert("Ошибка: " + (data.error || "неизвестная"));
                return null;
            });
        }
        return r.blob();
    })
    .then(function (blob) {
        if (!blob) return;
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = "results.xlsx";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        closeExportModal();
    });
}

var searchEl = document.getElementById("searchInput");
if (searchEl) searchEl.oninput = applySearch;
