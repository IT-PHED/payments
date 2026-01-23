// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", () => {

    document.querySelectorAll("form.js-loading-form").forEach(form => {

        form.addEventListener("submit", () => {
            const btn = form.querySelector("button[type=submit]");
            if (!btn) return;

            setLoading(btn, true);
        });

    });

});

function setLoading(button, isLoading) {
    const spinner = button.querySelector(".spinner-border");
    const text = button.querySelector(".btn-text");

    if (isLoading) {
        button.disabled = true;
        spinner?.classList.remove("d-none");
        text?.classList.add("d-none");
    } else {
        button.disabled = false;
        spinner?.classList.add("d-none");
        text?.classList.remove("d-none");
    }
}
