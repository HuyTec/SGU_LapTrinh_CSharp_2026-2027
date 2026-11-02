(() => {
    const accountType = document.getElementById('AccountType');
    const field = document.getElementById('student-code-field');
    const code = document.getElementById('StudentCode');
    if (!accountType || !field || !code) return;
    function update() {
        const student = accountType.value === 'Student';
        field.hidden = !student;
        code.required = student;
        code.disabled = !student;
    }
    accountType.addEventListener('change', update);
    update();
})();
