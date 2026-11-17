/* Shared question Markdown/TeX renderer. Store source only; never accept raw HTML. */
(() => {
    'use strict';
    if (!window.markdownit || !window.katex || !window.texmath) return;
    const markdown = window.markdownit({ html: false, breaks: true, linkify: false })
        .disable(['image'])
        .use(window.texmath, {
            engine: window.katex,
            delimiters: ['dollars', 'brackets'],
            katexOptions: { trust: false, throwOnError: false, maxExpand: 100, maxSize: 20, strict: 'ignore' }
        });
    // One malformed expression must not prevent the rest of the exam from loading.
    function render(target, source) {
        try { target.innerHTML = markdown.render(source); }
        catch { target.textContent = source; }
    }
    document.querySelectorAll('.question-markdown').forEach(target => render(target, target.textContent));
    document.querySelectorAll('[data-preview-for]').forEach(preview => {
        const input = document.getElementById(preview.dataset.previewFor);
        const output = preview.querySelector('.preview-content');
        if (!input || !output) return;
        input.setAttribute('aria-describedby', 'question-format-help');
        let timer;
        const update = () => render(output, input.value || 'Chưa có nội dung.');
        input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(update, 150); });
        update();
    });
})();
