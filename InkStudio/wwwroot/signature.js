// Obtener el token de la URL
const urlParams = new URLSearchParams(window.location.search);
const tokenFromPath = window.location.pathname.split('/firma/')[1];
const token = tokenFromPath || urlParams.get('token') || '{TOKEN}';

// Variables globales
let canvas, ctx;
let isDrawing = false;
let lastX = 0;
let lastY = 0;
let hasSignature = false;

// Inicialización cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    inicializarCanvas();
    configurarEventos();
});

/**
 * Inicializa el canvas de firma.
 */
function inicializarCanvas() {
    canvas = document.getElementById('signatureCanvas');
    ctx = canvas.getContext('2d');
    
    // Ajustar tamaño del canvas
    ajustarTamañoCanvas();
    
    // Configurar estilo del lápiz
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    
    // Redimensionar canvas si cambia el tamaño de la ventana
    window.addEventListener('resize', ajustarTamañoCanvas);
}

/**
 * Ajusta el tamaño del canvas al contenedor.
 */
function ajustarTamañoCanvas() {
    const container = canvas.parentElement;
    const rect = container.getBoundingClientRect();
    
    // Tamaño del canvas (con padding)
    const width = rect.width - 40; // 20px padding a cada lado
    const height = Math.min(width * 0.6, window.innerHeight * 0.4); // Proporción 3:2, máximo 40% de altura
    
    // Establecer tamaño real del canvas
    canvas.width = width;
    canvas.height = height;
    
    // Establecer tamaño CSS
    canvas.style.width = width + 'px';
    canvas.style.height = height + 'px';
    
    // Limpiar canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    // Ocultar overlay si hay firma
    const overlay = document.getElementById('canvasOverlay');
    if (hasSignature) {
        overlay.style.display = 'none';
    } else {
        overlay.style.display = 'flex';
    }
}

/**
 * Configura los eventos del canvas y botones.
 */
function configurarEventos() {
    // Eventos táctiles (móvil)
    canvas.addEventListener('touchstart', iniciarDibujo, { passive: false });
    canvas.addEventListener('touchmove', dibujar, { passive: false });
    canvas.addEventListener('touchend', finalizarDibujo, { passive: false });
    canvas.addEventListener('touchcancel', finalizarDibujo, { passive: false });
    
    // Eventos de mouse (PC/tableta)
    canvas.addEventListener('mousedown', iniciarDibujo);
    canvas.addEventListener('mousemove', dibujar);
    canvas.addEventListener('mouseup', finalizarDibujo);
    canvas.addEventListener('mouseleave', finalizarDibujo);
    
    // Botones
    document.getElementById('btnLimpiar').addEventListener('click', limpiarFirma);
    document.getElementById('btnEnviar').addEventListener('click', enviarFirma);
    
    // Prevenir scroll mientras se dibuja
    canvas.addEventListener('touchmove', function(e) {
        if (isDrawing) {
            e.preventDefault();
        }
    }, { passive: false });
}

/**
 * Obtiene las coordenadas del evento (touch o mouse).
 */
function obtenerCoordenadas(e) {
    const rect = canvas.getBoundingClientRect();
    
    if (e.touches && e.touches.length > 0) {
        // Evento táctil
        return {
            x: e.touches[0].clientX - rect.left,
            y: e.touches[0].clientY - rect.top
        };
    } else {
        // Evento de mouse
        return {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };
    }
}

/**
 * Inicia el dibujo.
 */
function iniciarDibujo(e) {
    e.preventDefault();
    isDrawing = true;
    
    const coords = obtenerCoordenadas(e);
    lastX = coords.x;
    lastY = coords.y;
    
    // Ocultar overlay
    document.getElementById('canvasOverlay').style.display = 'none';
}

/**
 * Dibuja en el canvas.
 */
function dibujar(e) {
    if (!isDrawing) return;
    e.preventDefault();
    
    const coords = obtenerCoordenadas(e);
    
    ctx.beginPath();
    ctx.moveTo(lastX, lastY);
    ctx.lineTo(coords.x, coords.y);
    ctx.stroke();
    
    lastX = coords.x;
    lastY = coords.y;
    
    // Marcar que hay firma
    if (!hasSignature) {
        hasSignature = true;
        document.getElementById('btnEnviar').disabled = false;
    }
}

/**
 * Finaliza el dibujo.
 */
function finalizarDibujo(e) {
    if (isDrawing) {
        isDrawing = false;
    }
}

/**
 * Limpia el canvas y reinicia el estado.
 */
function limpiarFirma() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    hasSignature = false;
    document.getElementById('btnEnviar').disabled = true;
    document.getElementById('canvasOverlay').style.display = 'flex';
    
    ocultarMensaje();
}

/**
 * Convierte el canvas a imagen base64.
 */
function canvasToBase64() {
    return canvas.toDataURL('image/png');
}

/**
 * Envía la firma al servidor.
 */
async function enviarFirma() {
    if (!hasSignature) {
        mostrarMensaje('Por favor, firma primero', 'error');
        return;
    }
    
    const btnEnviar = document.getElementById('btnEnviar');
    const btnLimpiar = document.getElementById('btnLimpiar');
    
    // Deshabilitar botones
    btnEnviar.disabled = true;
    btnLimpiar.disabled = true;
    btnEnviar.textContent = '⏳ Enviando...';
    
    try {
        // Obtener imagen base64
        const imagenBase64 = canvasToBase64();
        
        // Enviar al servidor
        const response = await fetch(`/firma/${token}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'text/plain'
            },
            body: imagenBase64
        });
        
        if (response.ok) {
            mostrarMensaje('✓ Firma enviada correctamente', 'success');
            
            // Deshabilitar canvas
            canvas.style.pointerEvents = 'none';
            canvas.style.opacity = '0.7';
            
            // Cambiar botón
            btnEnviar.textContent = '✓ Enviado';
            btnEnviar.style.backgroundColor = '#10b981';
            
            // Redirigir después de 2 segundos
            setTimeout(() => {
                mostrarMensaje('Puedes cerrar esta página', 'info');
            }, 2000);
        } else {
            throw new Error('Error al enviar la firma');
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarMensaje('❌ Error al enviar la firma. Intenta de nuevo.', 'error');
        
        // Rehabilitar botones
        btnEnviar.disabled = false;
        btnLimpiar.disabled = false;
        btnEnviar.textContent = '✓ Enviar Firma';
    }
}

/**
 * Muestra un mensaje de estado.
 */
function mostrarMensaje(mensaje, tipo = 'info') {
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.textContent = mensaje;
    statusDiv.className = `status-message status-${tipo}`;
    statusDiv.style.display = 'block';
    
    // Auto-ocultar después de 5 segundos (excepto success)
    if (tipo !== 'success') {
        setTimeout(ocultarMensaje, 5000);
    }
}

/**
 * Oculta el mensaje de estado.
 */
function ocultarMensaje() {
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.style.display = 'none';
}

