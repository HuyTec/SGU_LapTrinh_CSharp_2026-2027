// Run with: node scripts/Test-QuestionContent.cjs (no npm install required).
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const root = resolve(__dirname, '../Exam.Web/wwwroot');
const source = String.raw`**Tính** $\frac{1}{2}$

$$\int_0^1 x\,dx$$

\(x^2\)

${String.fromCharCode(96)}$literal$${String.fromCharCode(96)}

<script>alert(1)</script>
[bad](javascript:alert(1))
![image](https://example.com/track.png)
$\href{javascript:alert(1)}{click}$`;
const target = { textContent: source, innerHTML: '' };
const output = { textContent: '', innerHTML: '' };
let onInput;
const input = { value: '$x^2$', setAttribute() {}, addEventListener(_, callback) { onInput = callback; } };
const context = vm.createContext({ console, setTimeout: callback => callback(), clearTimeout() {}, document: {
    compatMode: 'CSS1Compat',
    querySelectorAll: selector => selector === '.question-markdown' ? [target] : [{
        dataset: { previewFor: 'text' }, querySelector: () => output
    }], getElementById: () => input
} });
context.window = context;
for (const file of ['lib/markdown-it/markdown-it.min.js', 'lib/katex/katex.min.js',
    'lib/markdown-it-texmath/texmath.js', 'js/question-content.js']) {
    vm.runInContext(readFileSync(resolve(root, file), 'utf8'), context, { filename: file });
}
assert.match(target.innerHTML, /<strong>Tính<\/strong>/);
assert.match(target.innerHTML, /class="katex"/);
assert.match(target.innerHTML, /katex-display/);
assert.match(target.innerHTML, /<code>\$literal\$<\/code>/);
assert.match(target.innerHTML, /&lt;script&gt;/);
assert.doesNotMatch(target.innerHTML, /<script|<img|href="javascript:/i);
assert.match(output.innerHTML, /class="katex"/);
input.value = '**Đáp án mới**'; onInput();
assert.match(output.innerHTML, /<strong>Đáp án mới<\/strong>/);
input.value = '$\\invalidcommand{<img src=x onerror=alert(1)>}$'; onInput();
assert.doesNotMatch(output.innerHTML, /<img|onerror="/i);
input.value = '$\\frac{1}{$'; onInput();
assert.match(output.innerHTML, /katex-error/);
assert.equal(target.textContent, source, 'Source is not rewritten by rendering');
console.log('PASS: Markdown, inline/block/bracket math, code, live preview, invalid TeX and XSS checks.');
