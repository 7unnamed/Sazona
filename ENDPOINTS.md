# Qué se puede hacer hoy con la API

Esta es una lista simple de todo lo que el sistema permite hacer en este momento, agrupado por tema. Todo se accede a través de una única puerta de entrada (el Gateway), en `http://<dominio-o-ip>/...`.

Salvo el registro, el inicio de sesión y la renovación de sesión, **todo lo demás requiere haber iniciado sesión primero** (se debe enviar el token que entrega el login en cada pedido).

## Cuentas de usuario (`/auth`)

- **Crear una cuenta nueva** — registra un usuario con nombre de usuario, email y contraseña.
- **Iniciar sesión** — con usuario y contraseña, entrega dos cosas: un token de sesión (dura 1 hora) y un token de renovación (dura 30 días).
- **Renovar la sesión** — con el token de renovación, entrega un token de sesión nuevo sin tener que volver a escribir usuario y contraseña. Cada vez que se usa, se entrega también un token de renovación nuevo (el anterior queda inválido).
- **Cerrar sesión** — invalida el token de renovación a propósito, antes de que expire solo.

## Catálogo de ingredientes (`/meals/ingredientes`)

Un ingrediente existe **una sola vez** acá, aunque se use en muchos platos distintos.

- **Ver todos los ingredientes del catálogo**
- **Ver el detalle de un ingrediente puntual**
- **Agregar un ingrediente nuevo al catálogo** — nombre, país de procedencia, categoría (verdura, fruta, proteína, lácteo, grano, condimento, otro) y una descripción opcional.
- **Editar un ingrediente del catálogo**
- **Eliminar un ingrediente del catálogo** — no se borra de verdad, queda marcado como eliminado (se conserva por auditoría).

## Platos (`/meals/platos`)

- **Ver todos los platos**
- **Ver el detalle de un plato puntual** — incluye sus ingredientes, con nombre traído del catálogo, y la cantidad/unidad específica para ese plato.
- **Crear un plato nuevo** — con nombre, tipo de comida (desayuno/almuerzo/cena/snack), porciones base, y opcionalmente una lista de ingredientes (referenciando ingredientes que ya existan en el catálogo, con su cantidad y unidad para este plato).
- **Editar un plato** — nombre, tipo de comida, porciones.
- **Eliminar un plato** — no se borra de verdad, queda marcado como eliminado y deja de aparecer en las consultas.

## Ingredientes dentro de un plato (`/meals/platos/{plato}/ingredientes`)

Esto es distinto del catálogo — es la relación "este plato lleva este ingrediente, en esta cantidad".

- **Agregar un ingrediente del catálogo a un plato existente** — con su cantidad y unidad (ej. "2 tazas").
- **Editar la cantidad/unidad de un ingrediente ya agregado a un plato**
- **Quitar un ingrediente de un plato** — no afecta al catálogo ni a otros platos que usen el mismo ingrediente; queda marcado como eliminado.

## Historial de comidas planificadas (`/planner/historial`)

- **Ver todo el historial**
- **Ver el detalle de una entrada puntual**
- **Registrar una entrada nueva** — qué plato, para qué fecha, qué tipo de comida, y si ya quedó confirmada.
- **Editar una entrada del historial**
- **Eliminar una entrada** — mismo criterio de eliminación "suave" que en los platos.

## Qué queda registrado automáticamente

Cada vez que se crea, edita o elimina algo, el sistema guarda solo **quién** lo hizo y **cuándo**, sin que nadie tenga que ingresarlo a mano. También queda un registro de cada pedido que llega al sistema (quién lo hizo, qué pidió, y si funcionó o no), útil para revisar actividad o investigar un problema después.

## Qué todavía no existe

- No hay forma de "recuperar" algo eliminado desde la API (queda en la base, pero no hay un botón para restaurarlo todavía).
- Los usuarios no tienen permisos diferenciados en la práctica: aunque existe un campo de rol (Usuario/Administrador), hoy cualquier usuario logueado puede hacer cualquier acción.
- No hay forma de cerrar sesión "en todos los dispositivos a la vez" — cerrar sesión solo invalida el token puntual que se envía.
