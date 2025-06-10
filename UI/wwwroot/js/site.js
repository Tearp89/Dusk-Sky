// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
console.log("✅ site.js cargado correctamente");

document.addEventListener("DOMContentLoaded", function () {
    // --- Like button AJAX and animation logic ---
    const likeForms = document.querySelectorAll(".like-form");

    likeForms.forEach(form => {
        const likeButton = form.querySelector(".btn-like");
        const reviewIdInput = form.querySelector('input[name="ReviewId"]');
        const likeTextSpan = form.querySelector('.like-text');
        const likeCountSpan = form.querySelector('.like-count');

        // *** Importante: Asegúrate de que el botón en tu HTML tenga type="button" ***
        // <button type="button" class="btn ... btn-like">
        // De esta manera, el formulario no se envía de forma tradicional y el JS tiene control total.

        // Agregamos una verificación robusta para asegurar que todos los elementos existen
        if (!likeButton || !reviewIdInput || !likeTextSpan || !likeCountSpan) {
            console.warn('Skipping form due to missing essential elements (button, input, like-text span, like-count span). Form:', form);
            return; // Salta este formulario si falta algún elemento crucial
        }

        likeButton.addEventListener('click', async function (e) {
            e.preventDefault(); // Previene el envío normal del formulario

            // Inicia la animación de pulse inmediatamente al hacer clic
            likeButton.classList.add("pulse");

            const reviewId = reviewIdInput.value;
            
            if (!reviewId) {
                console.error('ReviewId is missing for like action for form:', form);
                alert('Error: ID de reseña no encontrado.');
                // Quita la animación si hay un error antes de la llamada AJAX
                likeButton.classList.remove("pulse");
                return;
            }

            // Prepara los datos del formulario
            const formData = new FormData();
            formData.append('ReviewId', reviewId);

            try {
                // Obtener el token de verificación de solicitudes para seguridad anti-CSRF
                const requestVerificationToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                
                const response = await fetch(form.action, {
                    method: form.method,
                    body: formData,
                    headers: {
                        'RequestVerificationToken': requestVerificationToken // Incluye el token aquí
                    }
                });

                if (response.ok) {
                    // Actualiza el UI directamente
                    let currentLikes = parseInt(likeCountSpan.textContent);
                    let userLiked = likeButton.classList.contains('btn-danger'); // Chequea el estado actual

                    if (userLiked) {
                        // Si ya le había dado like, ahora lo quitó
                        likeButton.classList.remove('btn-danger');
                        likeButton.classList.add('btn-outline-danger');
                        likeButton.querySelector('.bi').classList.remove('bi-heart-fill');
                        likeButton.querySelector('.bi').classList.add('bi-heart');
                        likeTextSpan.textContent = 'Me gusta';
                        currentLikes--;
                    } else {
                        // Si no le había dado like, ahora lo dio
                        likeButton.classList.remove('btn-outline-danger');
                        likeButton.classList.add('btn-danger');
                        likeButton.querySelector('.bi').classList.remove('bi-heart');
                        likeButton.querySelector('.bi').classList.add('bi-heart-fill');
                        likeTextSpan.textContent = 'Quitar like';
                        currentLikes++;
                    }
                    likeCountSpan.textContent = currentLikes; // Actualiza el contador
                } else {
                    // Si el servidor devuelve un error HTTP (ej. 400, 500)
                    console.error('Error al alternar like:', response.status, response.statusText);
                    alert('Hubo un error al procesar el like.');
                }
            } catch (error) {
                // Si hay un error de red o un error JavaScript antes de la respuesta HTTP
                console.error('Error de red o JavaScript inesperado:', error);
                console.error('Error name:', error.name);
                console.error('Error message:', error.message);
                if (error.stack) {
                    console.error('Error stack:', error.stack);
                }
                alert('Error de conexión o aplicación.');
            } finally {
                // Asegúrate de quitar la clase 'pulse' después de que la operación (exitosa o fallida) termine
                setTimeout(() => {
                    likeButton.classList.remove("pulse");
                }, 300); // Pequeño retraso para que la animación sea visible, ajusta si es necesario
            }
        });
    });
});