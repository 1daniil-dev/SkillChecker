function deleteResult(fileName, studentName) {
    if (!confirm("Удалить результат \"" + studentName + "\"?")) return;

    fetch("/api/results/" + encodeURIComponent(fileName), {
        method: "DELETE"
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        loadResults();
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

function renderResults() {
    var div = document.getElementById("resultsList");
    div.innerHTML = "";
    var sortDiv = document.getElementById("resultsSort");

    if (allResults.length === 0) {
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
    for (var s = 0; s < allResults.length; s++) {
        totalScore += allResults[s].Score;
        if (allResults[s].Score > bestScore) bestScore = allResults[s].Score;
        if (allResults[s].Score < worstScore) worstScore = allResults[s].Score;
    }
    var avgScore = (totalScore / allResults.length).toFixed(1);

    var statsDiv = document.getElementById("resultsStats");
    statsDiv.classList.remove("hidden");
    statsDiv.innerHTML = "<div class='stat-item'><span class='stat-value'>" + allResults.length + "</span><span class='stat-label'>Результатов</span></div>" +
        "<div class='stat-item'><span class='stat-value'>" + avgScore + "%</span><span class='stat-label'>Средний балл</span></div>" +
        "<div class='stat-item'><span class='stat-value'>" + bestScore + "%</span><span class='stat-label'>Лучший</span></div>" +
        "<div class='stat-item'><span class='stat-value'>" + worstScore + "%</span><span class='stat-label'>Худший</span></div>";

    var results = sortResults(allResults);

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
