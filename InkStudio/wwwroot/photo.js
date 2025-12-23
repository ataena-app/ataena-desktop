const video = document.getElementById('video');
const canvas = document.getElementById('canvas');
const btnCapture = document.getElementById('btnCapture');
const statusText = document.getElementById('status');

let stream = null;

async function initCamera() {
  try {
    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
    video.srcObject = stream;
    statusText.textContent = 'Cámara lista. Encadra y pulsa en "Tomar foto".';
  } catch (err) {
    console.error('Error al acceder a la cámara', err);
    statusText.textContent = '❌ No se pudo acceder a la cámara. Revisa permisos.';
  }
}

function getTokenFromUrl() {
  const parts = window.location.pathname.split('/');
  return parts[parts.length - 1] || '';
}

async function sendPhoto(dataUrl) {
  const token = getTokenFromUrl();
  if (!token) {
    statusText.textContent = '❌ Token no encontrado en la URL.';
    return;
  }

  try {
    statusText.textContent = '📤 Enviando foto...';
    const response = await fetch(`/foto/${token}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'text/plain'
      },
      body: dataUrl
    });

    if (response.ok) {
      statusText.textContent = '✅ Foto enviada correctamente. Ya puedes volver a la app.';
      if (stream) {
        stream.getTracks().forEach(t => t.stop());
      }
    } else {
      statusText.textContent = '❌ Error al enviar la foto.';
    }
  } catch (err) {
    console.error('Error al enviar la foto', err);
    statusText.textContent = '❌ Error al enviar la foto.';
  }
}

btnCapture.addEventListener('click', () => {
  if (!video.videoWidth || !video.videoHeight) {
    statusText.textContent = '⏳ Espera a que la cámara esté lista.';
    return;
  }

  const width = video.videoWidth;
  const height = video.videoHeight;

  canvas.width = width;
  canvas.height = height;

  const ctx = canvas.getContext('2d');
  ctx.drawImage(video, 0, 0, width, height);

  const dataUrl = canvas.toDataURL('image/jpeg', 0.9);
  sendPhoto(dataUrl);
});

initCamera();


