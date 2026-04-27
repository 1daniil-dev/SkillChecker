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
    }
    event.target.classList.add("active");

    if (name === "results") {
        loadResults();
    }
}

function loadTests() {
    fetch("/api/tests")
        .then(function (r) { return r.json(); })
        .then(function (tests) {
            var div = document.getElementById("testsList");
            div.innerHTML = "";

            if (tests.length === 0) {
                div.innerHTML = '<div class="empty">Нет загруженных тестов</div>';
                return;
            }

            for (var i = 0; i < tests.length; i++) {
                var card = document.createElement("div");
                card.className = "test-card";

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

function loadResults() {
    fetch("/api/results")
        .then(function (r) { return r.json(); })
        .then(function (results) {
            var div = document.getElementById("resultsList");
            div.innerHTML = "";

            if (results.length === 0) {
                div.innerHTML = '<div class="empty">Пока нет результатов</div>';
                return;
            }

            for (var i = 0; i < results.length; i++) {
                var r = results[i];
                var card = document.createElement("div");
                card.className = "result-card";

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
                dateSpan.textContent = r.Date;
                meta.appendChild(dateSpan);

                card.appendChild(meta);
                div.appendChild(card);
            }
        });
}
