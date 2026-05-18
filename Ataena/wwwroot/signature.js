const urlParams = new URLSearchParams(window.location.search);
const tokenFromPath = window.location.pathname.split('/firma/')[1];
const token = tokenFromPath || urlParams.get('token') || '{TOKEN}';

const FIRMA_DOBLE_MENOR =
    typeof window.__ATAENA_FIRMA_MENOR_DOS_CAJAS__ !== 'undefined' &&
    window.__ATAENA_FIRMA_MENOR_DOS_CAJAS__ === true;

/** @type {boolean} Lo activa el checkbox de aceptación en firma.html */
function firmasPermitidas() {
    return window.__ATAENA_FIRMAS_HABILITADAS__ === true;
}

let canvas;
let ctx;
let hasSignature = false;

/** @type {{ canvas: HTMLCanvasElement, ctx: CanvasRenderingContext2D, overlay: HTMLElement | null, has: boolean }[]} */
let panelesDual = [];

document.addEventListener('DOMContentLoaded', function () {
    if (FIRMA_DOBLE_MENOR) {
        inicializarModoDual();
    } else {
        inicializarModoUnico();
    }
});

function estiloLapiz(context) {
    context.strokeStyle = '#000000';
    context.fillStyle = '#000000';
    context.lineWidth = 3;
    context.lineCap = 'round';
    context.lineJoin = 'round';
}

function posicionEnCanvas(c, clientX, clientY) {
    const rect = c.getBoundingClientRect();
    if (rect.width < 1 || rect.height < 1) {
        return { x: 0, y: 0 };
    }
    const scaleX = c.width / rect.width;
    const scaleY = c.height / rect.height;
    return {
        x: (clientX - rect.left) * scaleX,
        y: (clientY - rect.top) * scaleY
    };
}

function ajustarCanvas(c, context, overlay, conservarTrazo) {
    const rect = c.getBoundingClientRect();
    let width = Math.floor(rect.width);
    let height = Math.floor(rect.height);
    if (width < 80) {
        const parent = c.parentElement;
        if (parent) {
            const pr = parent.getBoundingClientRect();
            width = Math.max(120, Math.floor(pr.width - 32));
            height = Math.max(100, Math.min(Math.floor(width * 0.45), Math.floor(window.innerHeight * 0.25)));
        }
    }
    if (width < 80) {
        width = 280;
        height = 140;
    }

    let imagenPrev = null;
    if (conservarTrazo && c.width > 0 && c.height > 0) {
        try {
            imagenPrev = c.toDataURL('image/png');
        } catch (_) {
            imagenPrev = null;
        }
    }

    c.width = width;
    c.height = height;
    c.style.width = width + 'px';
    c.style.height = height + 'px';

    context.fillStyle = '#FFFFFF';
    context.fillRect(0, 0, width, height);
    estiloLapiz(context);

    if (imagenPrev) {
        const img = new Image();
        img.onload = function () {
            context.drawImage(img, 0, 0, width, height);
            estiloLapiz(context);
        };
        img.src = imagenPrev;
    }

    if (overlay) {
        const panel = panelesDual.find(function (p) {
            return p.canvas === c;
        });
        const tieneTrazo = panel ? panel.has : hasSignature;
        overlay.style.display = tieneTrazo ? 'none' : 'flex';
    }
}

/**
 * Dibujo táctil/ratón directamente sobre el canvas (patrón probado en móvil).
 */
function configurarEventosCanvas(c, context, overlay, onStroke) {
    let dibujando = false;
    let lx = 0;
    let ly = 0;

    function coordsTouch(ev) {
        const t = ev.changedTouches ? ev.changedTouches[0] : ev.touches[0];
        return posicionEnCanvas(c, t.clientX, t.clientY);
    }

    function coordsPointer(ev) {
        return posicionEnCanvas(c, ev.clientX, ev.clientY);
    }

    function trazarHasta(x, y) {
        context.beginPath();
        context.moveTo(lx, ly);
        context.lineTo(x, y);
        context.stroke();
        lx = x;
        ly = y;
        if (onStroke) onStroke();
    }

    function empezar(x, y) {
        if (!firmasPermitidas()) return false;
        dibujando = true;
        lx = x;
        ly = y;
        if (overlay) overlay.style.display = 'none';
        estiloLapiz(context);
        context.beginPath();
        context.arc(x, y, 2, 0, Math.PI * 2);
        context.fill();
        if (onStroke) onStroke();
        return true;
    }

    function terminar() {
        dibujando = false;
    }

    const usaPointer = typeof window.PointerEvent !== 'undefined';

    /* Touch solo si no hay Pointer Events (evita doble inicio en iOS/Android) */
    if (!usaPointer) {
        c.addEventListener(
            'touchstart',
            function (ev) {
                if (!firmasPermitidas()) return;
                ev.preventDefault();
                const p = coordsTouch(ev);
                empezar(p.x, p.y);
            },
            { passive: false }
        );

        c.addEventListener(
            'touchmove',
            function (ev) {
                if (!dibujando) return;
                ev.preventDefault();
                const p = coordsTouch(ev);
                trazarHasta(p.x, p.y);
            },
            { passive: false }
        );

        c.addEventListener(
            'touchend',
            function (ev) {
                if (!dibujando) return;
                ev.preventDefault();
                terminar();
            },
            { passive: false }
        );

        c.addEventListener('touchcancel', function () {
            terminar();
        });
    }

    if (usaPointer) {
        c.addEventListener(
            'pointerdown',
            function (ev) {
                if (!firmasPermitidas() || !ev.isPrimary) return;
                ev.preventDefault();
                try {
                    c.setPointerCapture(ev.pointerId);
                } catch (_) {
                    /* ok */
                }
                const p = coordsPointer(ev);
                empezar(p.x, p.y);
            },
            { passive: false }
        );

        c.addEventListener(
            'pointermove',
            function (ev) {
                if (!dibujando || !ev.isPrimary) return;
                ev.preventDefault();
                const p = coordsPointer(ev);
                trazarHasta(p.x, p.y);
            },
            { passive: false }
        );

        c.addEventListener(
            'pointerup',
            function (ev) {
                if (!dibujando || !ev.isPrimary) return;
                ev.preventDefault();
                terminar();
                try {
                    if (c.hasPointerCapture(ev.pointerId)) {
                        c.releasePointerCapture(ev.pointerId);
                    }
                } catch (_) {
                    /* ok */
                }
            },
            { passive: false }
        );

        c.addEventListener(
            'pointercancel',
            function (ev) {
                if (!ev.isPrimary) return;
                terminar();
            },
            { passive: false }
        );
    }

    /* Ratón en PC (pointerdown ya cubre la mayoría; mousedown por compatibilidad) */
    if (!usaPointer) {
        c.addEventListener('mousedown', function (ev) {
            if (!firmasPermitidas()) return;
            ev.preventDefault();
            const p = coordsPointer(ev);
            empezar(p.x, p.y);
        });

        c.addEventListener('mousemove', function (ev) {
            if (!dibujando) return;
            ev.preventDefault();
            const p = coordsPointer(ev);
            trazarHasta(p.x, p.y);
        });

        c.addEventListener('mouseup', function () {
            terminar();
        });

        c.addEventListener('mouseleave', function () {
            terminar();
        });
    }
}

function refrescarTodosLosCanvas() {
    if (FIRMA_DOBLE_MENOR) {
        panelesDual.forEach(function (p) {
            ajustarCanvas(p.canvas, p.ctx, p.overlay, true);
        });
    } else if (canvas && ctx) {
        ajustarCanvas(canvas, ctx, document.getElementById('canvasOverlay'), true);
    }
}

window.__ataenaRefrescarCanvasFirma = refrescarTodosLosCanvas;

function inicializarModoUnico() {
    canvas = document.getElementById('signatureCanvas');
    ctx = canvas.getContext('2d', { willReadFrequently: true });
    estiloLapiz(ctx);

    requestAnimationFrame(function () {
        ajustarCanvas(canvas, ctx, document.getElementById('canvasOverlay'), false);
    });

    window.addEventListener('resize', function () {
        hasSignature = false;
        ajustarCanvas(canvas, ctx, document.getElementById('canvasOverlay'), false);
        document.getElementById('btnEnviar').disabled = true;
    });

    configurarEventosCanvas(canvas, ctx, document.getElementById('canvasOverlay'), function () {
        if (!hasSignature) {
            hasSignature = true;
            document.getElementById('btnEnviar').disabled = false;
        }
    });

    document.getElementById('btnLimpiar').addEventListener('click', limpiarFirma);
    document.getElementById('btnEnviar').addEventListener('click', enviarFirmaUnica);
}

function dualActualizarBotonEnviar() {
    const ok = panelesDual.every(function (p) {
        return p.has;
    });
    document.getElementById('btnEnviarDual').disabled = !ok;
}

function inicializarModoDual() {
    const cr = document.getElementById('signatureCanvasRepresentante');
    const cm = document.getElementById('signatureCanvasMenor');
    const ctxR = cr.getContext('2d', { willReadFrequently: true });
    const ctxM = cm.getContext('2d', { willReadFrequently: true });

    panelesDual = [
        {
            canvas: cr,
            ctx: ctxR,
            overlay: document.getElementById('canvasOverlayRepresentante'),
            has: false
        },
        {
            canvas: cm,
            ctx: ctxM,
            overlay: document.getElementById('canvasOverlayMenor'),
            has: false
        }
    ];

    function resizeDual() {
        panelesDual.forEach(function (p) {
            const was = p.has;
            ajustarCanvas(p.canvas, p.ctx, p.overlay, was);
            p.has = was;
        });
        dualActualizarBotonEnviar();
    }

    requestAnimationFrame(resizeDual);
    window.addEventListener('resize', resizeDual);

    panelesDual.forEach(function (p) {
        configurarEventosCanvas(p.canvas, p.ctx, p.overlay, function () {
            if (!p.has) {
                p.has = true;
                dualActualizarBotonEnviar();
            }
        });
    });

    document.getElementById('btnLimpiarDual').addEventListener('click', function () {
        panelesDual.forEach(function (p) {
            p.has = false;
            ajustarCanvas(p.canvas, p.ctx, p.overlay, false);
        });
        dualActualizarBotonEnviar();
        ocultarMensaje();
    });

    document.getElementById('btnEnviarDual').addEventListener('click', enviarFirmaDual);
}

function limpiarFirma() {
    hasSignature = false;
    ajustarCanvas(canvas, ctx, document.getElementById('canvasOverlay'), false);
    document.getElementById('btnEnviar').disabled = true;
    ocultarMensaje();
}

function canvasToBase64(c) {
    return c.toDataURL('image/png');
}

async function enviarFirmaUnica() {
    if (!hasSignature) {
        mostrarMensaje('Por favor, firma primero', 'error');
        return;
    }
    const btnEnviar = document.getElementById('btnEnviar');
    const btnLimpiar = document.getElementById('btnLimpiar');
    btnEnviar.disabled = true;
    btnLimpiar.disabled = true;
    btnEnviar.textContent = '⏳ Enviando...';

    try {
        const response = await fetch('/firma/' + token, {
            method: 'POST',
            headers: { 'Content-Type': 'text/plain' },
            body: canvasToBase64(canvas)
        });
        if (response.ok) {
            exitoUiFirma(btnEnviar, canvas);
        } else {
            throw new Error('HTTP');
        }
    } catch (error) {
        console.error(error);
        mostrarMensaje('❌ Error al enviar la firma. Intenta de nuevo.', 'error');
        btnEnviar.disabled = false;
        btnLimpiar.disabled = false;
        btnEnviar.textContent = '✓ Enviar firma';
    }
}

async function enviarFirmaDual() {
    if (!panelesDual.every(function (p) {
        return p.has;
    })) {
        mostrarMensaje('Faltan firmas: representante legal y menor.', 'error');
        return;
    }
    const btn = document.getElementById('btnEnviarDual');
    const btnL = document.getElementById('btnLimpiarDual');
    btn.disabled = true;
    btnL.disabled = true;
    btn.textContent = '⏳ Enviando...';

    try {
        const body = JSON.stringify({
            firmaRepresentanteLegal: canvasToBase64(panelesDual[0].canvas),
            firmaMenor: canvasToBase64(panelesDual[1].canvas)
        });
        const response = await fetch('/firma/' + token, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: body
        });
        if (response.ok) {
            panelesDual.forEach(function (p) {
                p.canvas.style.pointerEvents = 'none';
                p.canvas.style.opacity = '0.7';
            });
            mostrarMensaje('✓ Ambas firmas enviadas correctamente', 'success');
            btn.textContent = '✓ Enviado';
            btn.style.backgroundColor = '#10b981';
        } else {
            throw new Error('HTTP');
        }
    } catch (error) {
        console.error(error);
        mostrarMensaje('❌ Error al enviar. Intenta de nuevo.', 'error');
        btn.disabled = false;
        btnL.disabled = false;
        btn.textContent = '✓ Enviar ambas firmas';
    }
}

function exitoUiFirma(btnEnviar, c) {
    mostrarMensaje('✓ Firma enviada correctamente', 'success');
    c.style.pointerEvents = 'none';
    c.style.opacity = '0.7';
    btnEnviar.textContent = '✓ Enviado';
    btnEnviar.style.backgroundColor = '#10b981';
}

function mostrarMensaje(mensaje, tipo) {
    if (tipo === 'info') tipo = 'error';
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.textContent = mensaje;
    statusDiv.className = 'status-message status-' + tipo;
    statusDiv.style.display = 'block';
    if (tipo !== 'success') {
        setTimeout(ocultarMensaje, 5000);
    }
}

function ocultarMensaje() {
    document.getElementById('statusMessage').style.display = 'none';
}
