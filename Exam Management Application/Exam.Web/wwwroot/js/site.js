(() => {
    const labels = { Draft: 'Nháp', Active: 'Đã kích hoạt', Scheduled: 'Chưa đến giờ', Open: 'Đang nhận thí sinh', Closed: 'Đã đóng nhận · Còn lượt đang làm', Completed: 'Đã kết thúc', Cancelled: 'Đã hủy', Postponed: 'Đã hoãn' };
    document.querySelectorAll('.status-tag[data-status], option[data-status]').forEach(el => { el.textContent = labels[el.dataset.status] || el.dataset.status; });
    document.querySelectorAll('time[data-local-time]').forEach(el => {
        const raw = el.dataset.localTime;
        const date = new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(raw) ? raw : raw + 'Z');
        if (!Number.isNaN(date.getTime())) { el.dateTime = date.toISOString(); el.textContent = new Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', dateStyle: 'short', timeStyle: 'short' }).format(date); }
    });
    document.querySelectorAll('form[data-confirm]').forEach(form => form.addEventListener('submit', event => { if (!window.confirm(form.dataset.confirm)) event.preventDefault(); }));
})();
