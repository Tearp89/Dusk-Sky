﻿console.log("✅ site.js cargado correctamente");

async function toggleLike(button) {
  const form = button.closest("form");
  const reviewId = form.querySelector('input[name="ReviewId"]').value;
  const likeTextSpan = form.querySelector(".like-text");
  const likeCountSpan = form.querySelector(".like-count");
  const badgeCountSpan = document.querySelector(
    `.like-badge[data-review-id="${reviewId}"] .like-count`
  );

  button.classList.add("pulse");

  const formData = new FormData();
  formData.append("ReviewId", reviewId);

  const requestVerificationToken = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  )?.value;

  try {
    console.log("➡️ Enviando fetch a:", form.action);

    const response = await fetch(form.action, {
      method: form.method,
      body: formData,
      headers: {
        RequestVerificationToken: requestVerificationToken,
      },
    });

    if (!response.ok) {
      console.error("Error del servidor:", response.status);
      alert("Hubo un error al procesar el like.");
      return;
    }

    const result = await response.json();
    const nowLiked = result.liked;

    let currentLikes = parseInt(likeCountSpan.textContent);

    if (nowLiked) {
      button.classList.remove("btn-outline-danger");
      button.classList.add("btn-danger");
      button.querySelector(".bi").classList.remove("bi-heart");
      button.querySelector(".bi").classList.add("bi-heart-fill");
      likeTextSpan.textContent = "Quitar like";
      currentLikes++;
    } else {
      button.classList.remove("btn-danger");
      button.classList.add("btn-outline-danger");
      button.querySelector(".bi").classList.remove("bi-heart-fill");
      button.querySelector(".bi").classList.add("bi-heart");
      likeTextSpan.textContent = "Me gusta";
      currentLikes--;
    }

    likeCountSpan.textContent = currentLikes;
    if (badgeCountSpan) {
      badgeCountSpan.textContent = currentLikes;
    }
  } catch (error) {
    console.error("Error de red o JS:", error);
    alert("Error de red o del sistema.");
  } finally {
    setTimeout(() => {
      button.classList.remove("pulse");
    }, 300);
  }
}

document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll("[clickable-review-card]").forEach((card) => {
    card.addEventListener("click", function () {
      const href = card.getAttribute("data-href");
      if (href) {
        window.location.href = href;
      }
    });
  });
});

document.addEventListener("DOMContentLoaded", function () {
  const input = document.getElementById("gameSearchInput");
  const container = document.getElementById("gameListContainer");

  if (!input || !container) return;

  input.addEventListener("input", async function () {
    const term = this.value.trim();
    container.innerHTML = ""; // Limpiamos el contenedor al empezar

    // La validación de la longitud mínima es correcta
    if (term.length < 2) return;

    try {
      // La llamada fetch sigue siendo la misma
      const response = await fetch(
        // NOTA: Asegúrate que esta URL sea la correcta para tu proyecto.
        // Si tu página es /Games/Search, la URL podría ser solo `?handler=Search&term=...`
        `/Games/GameSearch?handler=Search&term=${encodeURIComponent(term)}`
      );

      // El manejo de errores de red sigue siendo correcto
      if (!response.ok) {
        throw new Error(`Error de red: ${response.status}`);
      }

      // MODIFICADO: La respuesta ahora es un objeto, no un array directamente.
      const result = await response.json();

      // NUEVO: Verificamos si la operación en el servidor fue exitosa.
      if (result && result.success) {
        // MODIFICADO: El array de juegos ahora está dentro de `result.data`.
        const games = result.data;

        if (!games || games.length === 0) {
          container.innerHTML = `<li class="list-group-item text-muted">No se encontraron resultados</li>`;
          return;
        }

        // El código para mostrar los resultados no cambia, ya que `games` sigue siendo el array.
        games.forEach((game) => {
          const li = document.createElement("li");
          li.className =
            "list-group-item list-group-item-action d-flex align-items-center gap-2";

          const img = document.createElement("img");
          img.src = game.headerUrl || "/Images/noImage.png";
          img.alt = game.name;
          img.style.width = "40px";
          img.style.height = "40px";
          img.style.objectFit = "cover";
          img.className = "rounded";

          const span = document.createElement("span");
          span.textContent = game.name;

          li.appendChild(img);
          li.appendChild(span);

          li.onclick = () => {
            window.location.href = `/Reviews/Creator?gameId=${game.id}`;
          };

          container.appendChild(li);
        });
      } else {
        // NUEVO: Manejo para cuando el servidor responde con success: false
        const errorMessage = result.message || "Ocurrió un error inesperado en el servidor.";
        console.error("Error reportado por el servidor:", errorMessage);
        container.innerHTML = `<li class="list-group-item text-danger">${errorMessage}</li>`;
      }
    } catch (error) {
      // Este bloque ahora captura errores de red o de parseo del JSON.
      console.error("Error en la función de búsqueda:", error);
      container.innerHTML = `<li class="list-group-item text-danger">No se pudo conectar con el servidor.</li>`;
    }
  });
});

function openLogGameModal(game) {
  document.getElementById("logGameImage").src =
    game.headerUrl || "/Images/noImage.png";
  document.getElementById("logGameName").textContent = game.title;
  document.getElementById("playedDate").valueAsDate = new Date();
  document.getElementById("gameReviewText").value = "";
  document.getElementById("tagInput").value = "";
  document.getElementById("gameLikeToggle").classList.remove("bi-heart-fill");
  document.getElementById("gameLikeToggle").classList.add("bi-heart");
  document.getElementById("logGameFormModal").dataset.gameId = game.id;


  new bootstrap.Modal(document.getElementById("logGameFormModal")).show();
}



/*
    function toggleTracking(button) {
        const icon = button.querySelector('i');
        const span = button.querySelector('span');
        const type = button.dataset.trackingType;
        const isActive = button.dataset.active === "true";
        const gameId = "@Model.Review.GameId"; // o usa Review.GameId directamente

        // Cambiar visualmente
        let newIcon = "", newText = "";
        switch (type) {
            case "watch":
                newIcon = isActive ? "bi-eye" : "bi-eye-fill";
                newText = isActive ? "Watch" : "Watched";
                break;
            case "like":
                newIcon = isActive ? "bi-heart" : "bi-heart-fill";
                newText = isActive ? "Like" : "Liked";
                break;
            case "watchlist":
                newIcon = isActive ? "bi-bookmark-plus" : "bi-bookmark-check-fill";
                newText = isActive ? "Watchlist" : "Saved";
                break;
        }

        icon.className = "bi " + newIcon;
        span.textContent = newText;
        button.dataset.active = (!isActive).toString();

        // Cambiar estilo visual
        button.classList.toggle("btn-secondary", !isActive);
        button.classList.toggle("btn-outline-secondary", isActive);

        // Enviar cambio al backend
        fetch("/GameTracking/Toggle", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
            },
            body: JSON.stringify({
                gameId: gameId,
                type: type,
                active: !isActive
            })
        });
    }

*/

async function toggleTracking(button) {
        const reviewId = button.dataset.reviewId;
        const type = button.dataset.trackingType;

        const response = await fetch("?handler=ToggleTrackingAjax", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ reviewId, type })
        });

        const result = await response.json();
        if (!result.success) return;

        // Update button style
        button.dataset.active = result.isActive.toString();
        const icon = button.querySelector("i");
        const span = button.querySelector("span");

        if (type === "watch") {
            icon.className = result.isActive ? "bi bi-eye-fill" : "bi bi-eye";
            span.textContent = result.isActive ? "Watched" : "Watch";
        } else if (type === "like") {
            icon.className = result.isActive ? "bi bi-heart-fill" : "bi bi-heart";
            span.textContent = result.isActive ? "Liked" : "Like";
        } else if (type === "watchlist") {
            icon.className = result.isActive ? "bi bi-bookmark-check-fill" : "bi bi-bookmark-plus";
            span.textContent = result.isActive ? "Saved" : "Watchlist";
        }

        button.classList.toggle("btn-secondary", result.isActive);
        button.classList.toggle("btn-outline-secondary", !result.isActive);
    }

 

function togglePlayedBefore() {
    const watchedOn = document.getElementById("WatchedOnEnabled");
    const playedBeforeContainer = document.getElementById("playedBeforeContainer");

    if (!watchedOn || !playedBeforeContainer) return;

    playedBeforeContainer.style.display = watchedOn.checked ? "block" : "none";
}

// --- SOLUCIÓN JAVASCRIPT DEFINITIVA para el Modal Atrapado ---
document.addEventListener('DOMContentLoaded', function () {
  const deleteModalElement = document.getElementById('confirmDeleteModal');

  if (deleteModalElement) {
    // Esta línea es la clave: Mueve el modal para que sea un hijo directo del <body>.
    // Esto lo saca de cualquier "caja" o "contexto de apilamiento" problemático.
    document.body.appendChild(deleteModalElement);
  }
});