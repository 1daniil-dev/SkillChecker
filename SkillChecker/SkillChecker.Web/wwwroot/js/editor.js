var editingTestName = "";
var editorQuestions = [];

function getTestNameFromUrl() {
    var params = new URLSearchParams(window.location.search);
    return params.get("test") || "";
}

function init() {
    editingTestName = getTestNameFromUrl();
    if (!editingTestName) {
        alert("Не указан тест");
        window.location = "index.html";
        return;
    }
    document.getElementById("editorTitle").textContent = "Редактор: " + editingTestName;

    authFetch("/api/test/" + encodeURIComponent(editingTestName) + "/preview")
        .then(function (r) { return r.json(); })
        .then(function (questions) {
            editorQuestions = questions;
            renderEditor();
        });
}

function renderEditor() {
    var container = document.getElementById("questionsContainer");
    container.innerHTML = "";
    document.getElementById("editError").classList.add("hidden");

    for (let i = 0; i < editorQuestions.length; i++) {
        var q = editorQuestions[i];
        var card = document.createElement("div");
        card.className = "question-editor-card";

        var header = document.createElement("div");
        header.className = "q-card-header";

        var label = document.createElement("span");
        label.className = "q-card-num";
        label.textContent = "Вопрос " + (i + 1);
        header.appendChild(label);

        var typeSelect = document.createElement("select");
        typeSelect.className = "q-type-select";
        typeSelect.setAttribute("data-qidx", i);

        var types = ["Single", "Multiple", "Text"];
        var typeNames = ["Одиночный выбор", "Множественный выбор", "Текстовый ввод"];
        for (var t = 0; t < types.length; t++) {
            var opt = document.createElement("option");
            opt.value = types[t];
            opt.textContent = typeNames[t];
            if (q.Type === types[t]) opt.selected = true;
            typeSelect.appendChild(opt);
        }
        typeSelect.onchange = function () {
            var idx = parseInt(this.getAttribute("data-qidx"));
            editorQuestions[idx].Type = this.value;
            if (this.value === "Single") editorQuestions[idx].CorrectAnswerIndex = 0;
            if (this.value === "Multiple") editorQuestions[idx].CorrectAnswerIndices = [];
            if (this.value === "Text") editorQuestions[idx].AcceptableAnswers = [];
            renderEditor();
        };
        header.appendChild(typeSelect);

        var removeBtn = document.createElement("button");
        removeBtn.className = "btn-remove-q";
        removeBtn.textContent = "\u00D7";
        removeBtn.title = "Удалить вопрос";
        removeBtn.onclick = function () {
            editorQuestions.splice(i, 1);
            renderEditor();
        };
        header.appendChild(removeBtn);

        card.appendChild(header);

        var textInput = document.createElement("input");
        textInput.type = "text";
        textInput.className = "q-text-input";
        textInput.placeholder = "Текст вопроса";
        textInput.value = q.Text || "";
        textInput.setAttribute("data-qidx", i);
        textInput.oninput = function () {
            var idx = parseInt(this.getAttribute("data-qidx"));
            editorQuestions[idx].Text = this.value;
        };
        card.appendChild(textInput);

        if (q.Type === "Single" || q.Type === "Multiple") {
            var optsLabel = document.createElement("div");
            optsLabel.className = "q-options-label";
            optsLabel.textContent = "Варианты ответа:";
            card.appendChild(optsLabel);

            var options = q.Options || [];
            for (let j = 0; j < options.length; j++) {
                var row = buildOptionRow(i, j, q, options);
                card.appendChild(row);
            }

            var addBtn = document.createElement("button");
            addBtn.className = "btn-add-opt";
            addBtn.textContent = "+ Добавить вариант";
            addBtn.onclick = function () {
                var opts = editorQuestions[i].Options || [];
                opts.push("");
                editorQuestions[i].Options = opts;
                renderEditor();
            };
            card.appendChild(addBtn);
        } else {
            var accLabel = document.createElement("div");
            accLabel.className = "q-options-label";
            accLabel.textContent = "Допустимые ответы:";
            card.appendChild(accLabel);

            var acceptable = q.AcceptableAnswers || [];
            for (let j = 0; j < acceptable.length; j++) {
                var row = document.createElement("div");
                row.className = "q-option-row";

                var accInput = document.createElement("input");
                accInput.type = "text";
                accInput.className = "q-option-input";
                accInput.placeholder = "Допустимый ответ";
                accInput.value = acceptable[j] || "";
                accInput.setAttribute("data-qidx", i);
                accInput.setAttribute("data-oidx", j);
                accInput.oninput = function () {
                    var qidx = parseInt(this.getAttribute("data-qidx"));
                    var oidx = parseInt(this.getAttribute("data-oidx"));
                    var arr = editorQuestions[qidx].AcceptableAnswers || [];
                    arr[oidx] = this.value;
                    editorQuestions[qidx].AcceptableAnswers = arr;
                };
                row.appendChild(accInput);

                var removeAcc = document.createElement("button");
                removeAcc.className = "btn-remove-opt";
                removeAcc.textContent = "\u00D7";
                removeAcc.onclick = function () {
                    var arr = editorQuestions[i].AcceptableAnswers || [];
                    arr.splice(j, 1);
                    editorQuestions[i].AcceptableAnswers = arr;
                    renderEditor();
                };
                row.appendChild(removeAcc);

                card.appendChild(row);
            }

            var addAccBtn = document.createElement("button");
            addAccBtn.className = "btn-add-opt";
            addAccBtn.textContent = "+ Добавить ответ";
            addAccBtn.onclick = function () {
                var arr = editorQuestions[i].AcceptableAnswers || [];
                arr.push("");
                editorQuestions[i].AcceptableAnswers = arr;
                renderEditor();
            };
            card.appendChild(addAccBtn);
        }

        container.appendChild(card);
    }
}

function buildOptionRow(qIdx, oIdx, q, options) {
    var row = document.createElement("div");
    row.className = "q-option-row";

    if (q.Type === "Single") {
        var radio = document.createElement("input");
        radio.type = "radio";
        radio.className = "q-option-radio";
        radio.name = "correct-q" + qIdx;
        radio.setAttribute("data-qidx", qIdx);
        radio.setAttribute("data-oidx", oIdx);
        if (q.CorrectAnswerIndex === oIdx) radio.checked = true;
        radio.onchange = function () {
            var qi = parseInt(this.getAttribute("data-qidx"));
            var oi = parseInt(this.getAttribute("data-oidx"));
            editorQuestions[qi].CorrectAnswerIndex = oi;
        };
        row.appendChild(radio);
    } else {
        var check = document.createElement("input");
        check.type = "checkbox";
        check.className = "q-option-check";
        check.setAttribute("data-qidx", qIdx);
        check.setAttribute("data-oidx", oIdx);
        var indices = q.CorrectAnswerIndices || [];
        if (indices.indexOf(oIdx) >= 0) check.checked = true;
        check.onchange = function () {
            var qi = parseInt(this.getAttribute("data-qidx"));
            var oi = parseInt(this.getAttribute("data-oidx"));
            var arr = editorQuestions[qi].CorrectAnswerIndices || [];
            if (this.checked) {
                if (arr.indexOf(oi) < 0) arr.push(oi);
            } else {
                var pos = arr.indexOf(oi);
                if (pos >= 0) arr.splice(pos, 1);
            }
            editorQuestions[qi].CorrectAnswerIndices = arr;
        };
        row.appendChild(check);
    }

    var optInput = document.createElement("input");
    optInput.type = "text";
    optInput.className = "q-option-input";
    optInput.placeholder = "Вариант ответа";
    optInput.value = options[oIdx] || "";
    optInput.setAttribute("data-qidx", qIdx);
    optInput.setAttribute("data-oidx", oIdx);
    optInput.oninput = function () {
        var qi = parseInt(this.getAttribute("data-qidx"));
        var oi = parseInt(this.getAttribute("data-oidx"));
        editorQuestions[qi].Options[oi] = this.value;
    };
    row.appendChild(optInput);

    var removeOpt = document.createElement("button");
    removeOpt.className = "btn-remove-opt";
    removeOpt.textContent = "\u00D7";
    removeOpt.title = "Удалить вариант";
    removeOpt.onclick = function () {
        var q = editorQuestions[qIdx];
        q.Options.splice(oIdx, 1);
        if (q.Type === "Single") {
            if (q.CorrectAnswerIndex === oIdx) q.CorrectAnswerIndex = 0;
            else if (q.CorrectAnswerIndex > oIdx) q.CorrectAnswerIndex--;
        }
        if (q.Type === "Multiple" && q.CorrectAnswerIndices) {
            var newArr = [];
            for (var k = 0; k < q.CorrectAnswerIndices.length; k++) {
                var v = q.CorrectAnswerIndices[k];
                if (v > oIdx) v--;
                if (v !== oIdx) newArr.push(v);
            }
            q.CorrectAnswerIndices = newArr;
        }
        renderEditor();
    };
    row.appendChild(removeOpt);

    return row;
}

function addQuestion() {
    editorQuestions.push({
        Text: "",
        Options: ["", ""],
        CorrectAnswerIndex: 0,
        Type: "Single"
    });
    renderEditor();
}

function saveTestEdit() {
    var errDiv = document.getElementById("editError");
    errDiv.classList.add("hidden");

    if (editorQuestions.length === 0) {
        errDiv.textContent = "Тест должен содержать хотя бы один вопрос";
        errDiv.classList.remove("hidden");
        return;
    }

    for (let i = 0; i < editorQuestions.length; i++) {
        var q = editorQuestions[i];
        if (!q.Text || q.Text.trim().length === 0) {
            errDiv.textContent = "Вопрос " + (i + 1) + ": не указан текст";
            errDiv.classList.remove("hidden");
            return;
        }
        if (q.Type === "Single" || q.Type === "Multiple") {
            if (!q.Options || q.Options.length < 2) {
                errDiv.textContent = "Вопрос " + (i + 1) + ": минимум 2 варианта ответа";
                errDiv.classList.remove("hidden");
                return;
            }
            for (let j = 0; j < q.Options.length; j++) {
                if (!q.Options[j] || q.Options[j].trim().length === 0) {
                    errDiv.textContent = "Вопрос " + (i + 1) + ": вариант " + (j + 1) + " не заполнен";
                    errDiv.classList.remove("hidden");
                    return;
                }
            }
        }
        if (q.Type === "Text" && (!q.AcceptableAnswers || q.AcceptableAnswers.length === 0)) {
            errDiv.textContent = "Вопрос " + (i + 1) + ": укажите хотя бы один допустимый ответ";
            errDiv.classList.remove("hidden");
            return;
        }
    }

    var json = JSON.stringify(editorQuestions, null, 2);

    authFetch("/api/test/" + encodeURIComponent(editingTestName), {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: json
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.ok) {
            window.location = "index.html";
        } else {
            errDiv.textContent = data.error || "Ошибка сохранения";
            errDiv.classList.remove("hidden");
        }
    });
}

init();
