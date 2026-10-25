// wwwroot/js/goods-receipt-index.js

let currentReceiptId = null;

function openReceiptPopup(receiptId) {
    currentReceiptId = receiptId;
    const modalEl = document.getElementById('receiptModal');
    const body = document.getElementById('receiptModalBody');

    body.innerHTML = '<div class="text-center py-4"><i class="fas fa-spinner fa-spin"></i> Chargement…</div>';
    bootstrap.Modal.getOrCreateInstance(modalEl).show();

    fetch(`/GoodsReceipt/Details/${receiptId}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(r => {
            if (!r.ok) throw new Error(r.statusText);
            return r.text();
        })
        .then(html => body.innerHTML = html)
        .catch(err => {
            console.error(err);
            body.innerHTML = '<div class="gr-alert gr-alert--danger"><i class="fas fa-exclamation-circle"></i> Erreur lors du chargement.</div>';
        });
}

function printReceipt(receiptId) {
    const w = window.open(`/GoodsReceipt/Details/${receiptId}?print=true`, '_blank');
    if (w) {
        w.addEventListener('load', () => w.print());
    }
}

function printReceiptFromModal() {
    if (currentReceiptId) printReceipt(currentReceiptId);
}