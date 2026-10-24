(() => {
    'use strict';
    const app = document.getElementById('exam-app');
    if (!app) return;
    const feedback = document.getElementById('exam-feedback');
    const saveStatus = document.getElementById('save-status');
    const submitButton = document.getElementById('submit-attempt');
    const retryButton = document.getElementById('retry-save');
    const dialog = document.getElementById('submit-dialog');
    const confirmButton = document.getElementById('confirm-submit');
    const cards = [...app.querySelectorAll('[data-question-id]')];
    const token = app.querySelector('input[name="__RequestVerificationToken"]').value;
    const dirty = new Map();
    let flushing = null, submitting = false, finished = false, deadlineReached = false, blocked = false;
    const utcMs = value => new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(value) ? value : value + 'Z').getTime();
    let remainingAtSync = utcMs(app.dataset.deadline) - utcMs(app.dataset.serverNow);
    let syncTick = performance.now();
    const report = message => { feedback.textContent = message; feedback.hidden = false; };
    const state = (text, kind) => { saveStatus.textContent = text; saveStatus.className = 'save-status ' + kind; };
    const remaining = () => remainingAtSync - (performance.now() - syncTick);
    function disableAnswers(value) { cards.forEach(card => card.querySelectorAll('input').forEach(input => { input.disabled = value; })); }
    function progress() {
        let answered = 0, flagged = 0;
        cards.forEach(card => {
            const selected = !!card.querySelector('input[type=radio]:checked');
            const flag = card.querySelector('.flag-question').checked;
            answered += Number(selected); flagged += Number(flag);
            const nav = app.querySelector('[data-nav-question="' + card.dataset.questionId + '"]');
            nav.classList.toggle('answered', selected); nav.classList.toggle('flagged', flag);
            nav.setAttribute('aria-label', 'Đến ' + card.querySelector('legend').textContent + (selected ? ', đã chọn đáp án' : ', chưa trả lời') + (flag ? ', đã đánh dấu' : ''));
        });
        document.getElementById('answer-progress').textContent = answered + '/' + cards.length + ' câu đã chọn · ' + flagged + ' câu đánh dấu';
        return answered;
    }
    async function request(url, body) {
        const controller = new AbortController();
        const timer = window.setTimeout(() => controller.abort(), 12000);
        try {
            const response = await fetch(url, { method: body === undefined ? 'GET' : 'POST', credentials: 'same-origin', redirect: 'error', signal: controller.signal, headers: { 'Accept': 'application/json', ...(body === undefined ? {} : { 'Content-Type': 'application/json', 'RequestVerificationToken': token }) }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
            let data = null;
            try { data = await response.json(); } catch { /* Friendly fallback below. */ }
            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    blocked = true; disableAnswers(true); submitButton.disabled = true;
                    throw new Error(response.status === 401 ? 'Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại và mở lại lượt thi. Chỉ dữ liệu đã lưu được khôi phục.' : 'Bạn không có quyền thao tác với bài thi này. Hãy liên hệ cán bộ phụ trách.');
                }
                throw new Error(data?.detail || data?.title || 'Yêu cầu chưa thành công. Hãy thử lại hoặc tải lại trang để kiểm tra dữ liệu đã lưu.');
            }
            if (!data) throw new Error('Không nhận được dữ liệu hợp lệ từ máy chủ. Hãy thử lại.');
            return data;
        } catch (error) {
            if (error.name === 'AbortError' || error instanceof TypeError) throw new Error('Không kết nối được máy chủ. Kiểm tra mạng và thử lại. Thời gian thi vẫn tiếp tục.');
            throw error;
        } finally { clearTimeout(timer); }
    }
    function sync(data) {
        if (data.serverNow && data.deadline) { remainingAtSync = utcMs(data.deadline) - utcMs(data.serverNow); syncTick = performance.now(); }
        if (data.status && data.status !== 'InProgress') { finished = true; disableAnswers(true); window.location.assign(app.dataset.resultUrl); }
    }
    async function flush() {
        if (flushing) return flushing;
        flushing = (async () => {
            while (dirty.size && !finished && !blocked) {
                const [id, payload] = dirty.entries().next().value;
                const card = document.getElementById('question-' + id);
                const label = card.querySelector('.question-save');
                label.textContent = 'Đang lưu…'; label.classList.remove('failed'); state('Đang lưu đáp án…', '');
                try {
                    const response = await request(app.dataset.saveUrl, payload);
                    sync(response);
                    // New choices made during an in-flight save must remain queued.
                    if (dirty.get(id) === payload) { dirty.delete(id); label.textContent = 'Đã lưu'; }
                } catch (error) {
                    label.textContent = 'Lưu thất bại'; label.classList.add('failed');
                    state('Có đáp án chưa được lưu', 'failed'); report(error.message); retryButton.hidden = blocked;
                    return false;
                }
            }
            if (!dirty.size) { state('Đã lưu tất cả thay đổi', 'saved'); retryButton.hidden = true; feedback.hidden = true; }
            return !dirty.size;
        })();
        try { return await flushing; } finally { flushing = null; }
    }
    cards.forEach(card => card.addEventListener('change', () => {
        if (submitting || finished || deadlineReached || blocked) return;
        const selected = card.querySelector('input[type=radio]:checked');
        const id = Number(card.dataset.questionId);
        dirty.set(id, { questionId: id, optionId: selected ? Number(selected.value) : null, isFlagged: card.querySelector('.flag-question').checked });
        card.querySelector('.question-save').textContent = 'Chờ lưu…';
        progress(); void flush();
    }));
    retryButton.addEventListener('click', () => { if (deadlineReached) void submit(true); else void flush(); });
    async function submit(automatic) {
        if (submitting || finished || blocked) return;
        submitting = true; submitButton.disabled = true; confirmButton.disabled = true; disableAnswers(true);
        if (dialog.open) dialog.close();
        state(automatic ? 'Hết giờ. Đang kết thúc bài thi…' : 'Đang lưu và nộp bài…', '');
        try {
            const saved = await flush();
            if (finished) return;
            if (!saved && !automatic) { report('Chưa thể nộp vì có đáp án lưu thất bại. Hãy thử lưu lại rồi nộp bài.'); return; }
            const response = await request(app.dataset.submitUrl, {});
            finished = true; dirty.clear();
            // Redirect only to a local route supplied by this application.
            const target = new URL(response.redirectUrl || app.dataset.resultUrl, window.location.origin);
            window.location.assign(target.origin === window.location.origin ? target.href : app.dataset.resultUrl);
        } catch (error) { report(error.message); state('Chưa nhận được xác nhận kết thúc bài', 'failed'); retryButton.hidden = false; }
        finally { submitting = false; confirmButton.disabled = false; submitButton.disabled = finished || blocked; if (!deadlineReached && !finished && !blocked) disableAnswers(false); }
    }
    submitButton.addEventListener('click', () => {
        if (deadlineReached) { void submit(true); return; }
        const count = progress();
        document.getElementById('submit-description').textContent = 'Bạn đã chọn đáp án cho ' + count + '/' + cards.length + ' câu. ' + (count < cards.length ? 'Còn ' + (cards.length - count) + ' câu chưa trả lời.' : 'Bạn đã trả lời tất cả câu hỏi.');
        dialog.showModal(); document.getElementById('cancel-submit').focus();
    });
    document.getElementById('cancel-submit').addEventListener('click', () => dialog.close());
    confirmButton.addEventListener('click', () => void submit(false));
    function tick() {
        const seconds = Math.max(0, Math.ceil(remaining() / 1000));
        document.getElementById('remaining-time').textContent = Math.floor(seconds / 60).toString().padStart(2, '0') + ':' + (seconds % 60).toString().padStart(2, '0');
        if (seconds <= 0 && !deadlineReached) { deadlineReached = true; disableAnswers(true); void submit(true); }
    }
    let checking = false;
    async function checkStatus() {
        if (finished || blocked || checking) return;
        checking = true;
        try { sync(await request(app.dataset.statusUrl)); }
        catch (error) { report(error.message); }
        finally { checking = false; }
    }
    window.addEventListener('online', () => { if (!blocked) { void checkStatus(); if (deadlineReached) void submit(true); else void flush(); } });
    window.addEventListener('offline', () => report('Mất kết nối mạng. Đáp án chưa được xác nhận cần thử lưu lại khi có mạng. Thời gian thi vẫn tiếp tục.'));
    window.addEventListener('beforeunload', event => { if ((dirty.size || submitting) && !finished) { event.preventDefault(); event.returnValue = ''; } });
    document.addEventListener('visibilitychange', () => { if (!document.hidden) void checkStatus(); });
    progress(); tick(); window.setInterval(tick, 1000); window.setInterval(checkStatus, 15000);
    if (app.dataset.status !== 'InProgress') window.location.replace(app.dataset.resultUrl);
})();
