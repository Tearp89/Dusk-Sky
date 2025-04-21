

# Dusk Sky - Plataforma Social para Videojuegos

Dusk Sky es una plataforma social diseñada para la comunidad gamer, inspirada en Letterboxd, pero enfocada en videojuegos. Permite a los usuarios compartir sus experiencias, calificar videojuegos, crear listas personalizadas y participar en una comunidad activa. Dusk Sky está construido para proporcionar una experiencia inmersiva, ofreciendo una interfaz amigable y funcional para jugadores, moderadores y administradores.

## Características

- **Registro y Autenticación**: Los usuarios pueden crear una cuenta, autenticar sus credenciales y recuperar su contraseña si es necesario.
- **Reseñas y Calificaciones**: Los usuarios pueden escribir reseñas detalladas y calificar videojuegos.
- **Listas Personalizadas**: Los usuarios pueden organizar sus juegos en listas personalizadas como "Jugados", "Jugando", "En Espera" y "Abandonados".
- **Interacción Social**: Los jugadores pueden seguir a otros usuarios, comentar en reseñas y mensajes privados.
- **Moderación de Contenido**: Los moderadores pueden eliminar comentarios, suspender y banear usuarios que infrinjan las normas.
- **Gestión de Videojuegos**: Solo los administradores pueden agregar, modificar o eliminar videojuegos de la plataforma.
- **Perfiles Personalizables**: Los usuarios pueden personalizar su perfil con Markdown y agregar contenido multimedia como videos.
- **Integración API**: Dusk Sky se integra con la API de Steam para obtener información sobre los videojuegos.

## Requerimientos Funcionales

### Gestión de Usuarios
- RF-01: Registro de nuevos usuarios.
- RF-02: Autenticación segura de usuarios.
- RF-03: Personalización de perfiles con contenido multimedia.
- RF-04: Seguir a otros jugadores y visualizar su actividad.
- RF-05: Cambio de contraseñas o eliminación de cuenta.

### Interacción Social
- RF-06: Publicación de reseñas y calificaciones.
- RF-07: Creación y edición de listas personalizadas.
- RF-08: Comentarios en publicaciones y reseñas.
- RF-09: Gestión de lista de amigos y solicitudes.
- RF-10: Sistema de chat privado entre amigos.

### Moderación y Seguridad
- RF-18: Suspensión y baneo de usuarios.
- RF-19: Eliminación de comentarios y publicaciones inapropiadas.
- RF-20: Gestión de reportes de contenido inapropiado.

### Gestión de Videojuegos (Solo Administradores)
- RF-11: Agregar nuevos videojuegos.
- RF-12: Modificar videojuegos existentes.
- RF-13: Eliminar videojuegos (cambio de estado, no eliminación definitiva).
- RF-14: Consultar información de los videojuegos.

### Seguimiento de Juegos
- RF-15: Agregar videojuegos a la lista personalizada.
- RF-16: Clasificar videojuegos según estado (jugado, jugando, en espera, abandonado).
- RF-17: Mostrar la lista de juegos y su estado en el perfil del usuario.

## Requerimientos No Funcionales

### Escalabilidad
- RNF-01: Plataforma accesible desde Web, Desktop y Móvil.
- RNF-02: Arquitectura escalable para futuras ampliaciones.

### Seguridad
- RNF-03: Cifrado de contraseñas en la base de datos.

### Disponibilidad
- RNF-04: Tolerancia a fallos menores.

### Integraciones y Compatibilidad
- RNF-06: API basada en SOAP, con soporte para integraciones futuras.

## Restricciones

### Restricciones Técnicas
- RN-13: Las imágenes de perfil no pueden superar los 5 MB.
- RN-14: Las contraseñas deben ser cifradas y no almacenadas en texto plano.

### Restricciones de Seguridad
- RN-16: No se permite contenido NSFW o ilegal. Los moderadores pueden eliminar publicaciones inapropiadas.

## Arquitectura del Sistema

Dusk Sky está compuesto por tres capas principales:

1. **Capa de Servicios**: Servicios RESTful y RPC para la interacción entre el frontend y el backend.
2. **Capa de Negocio**: Contiene la lógica de negocio para la gestión de usuarios, videojuegos, reseñas, etc.
3. **Capa de Datos**: Utiliza bases de datos como MySQL para los videojuegos y MongoDB para la interacción social.

## Integración de APIs

Dusk Sky se integra con la **API de Steam** para obtener información sobre los videojuegos, lo que permite tener datos actualizados y completos sobre los títulos disponibles en la plataforma.

## Instalación

Para comenzar con el desarrollo de Dusk Sky:

1. **Clona el repositorio**:
   ```bash
   git clone https://github.com/tu_usuario/dusk-sky.git
   cd dusk-sky
   ```

2. **Instala las dependencias**:
   ```bash
   dotnet restore
   ```

3. **Configura las conexiones a bases de datos** en el archivo `appsettings.json`:
   - **MySQL** para videojuegos.
   - **MongoDB** para usuarios y comentarios.

4. **Levanta el proyecto**:
   ```bash
   dotnet run
   ```



## Licencia

Este proyecto está bajo la licencia MIT.

