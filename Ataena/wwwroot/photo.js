const canvas = document.getElementById('canvas');
const btnUpload = document.getElementById('btnUpload');
const fileInput = document.getElementById('fileInput');
const statusText = document.getElementById('status');

// Debug: Log para ver qué está pasando
console.log('photo.js cargado');
console.log('fileInput:', fileInput);
console.log('btnUpload:', btnUpload);

// Mensaje inicial
statusText.textContent = 'Pulsa en "📁 Seleccionar foto" para elegir una imagen desde tu galería o cámara.';

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
    } else {
      statusText.textContent = '❌ Error al enviar la foto.';
    }
  } catch (err) {
    console.error('Error al enviar la foto', err);
    statusText.textContent = '❌ Error al enviar la foto.';
  }
}

// Botón para abrir galería - múltiples métodos para máxima compatibilidad
btnUpload.addEventListener('click', (e) => {
  e.preventDefault();
  e.stopPropagation();
  console.log('Botón upload clickeado');
  
  try {
    // Método 1: Click directo en el input (funciona en la mayoría de navegadores)
    fileInput.click();
    console.log('fileInput.click() ejecutado');
  } catch (err) {
    console.error('Error al hacer click en fileInput:', err);
    // Método 2: Si falla, intentar hacer el input visible momentáneamente
    statusText.textContent = '⚠️ Si no se abre la galería, toca directamente en el área del botón de nuevo.';
    
    // Forzar focus y click alternativo
    setTimeout(() => {
      fileInput.focus();
      fileInput.click();
    }, 100);
  }
});

// También permitir click directo en el input si es visible
fileInput.addEventListener('click', (e) => {
  console.log('Input file clickeado directamente');
});

fileInput.addEventListener('change', (e) => {
  console.log('Input file cambió, archivos:', e.target.files);
  const file = e.target.files[0];
  if (!file) {
    console.log('No se seleccionó ningún archivo');
    return;
  }

  console.log('Archivo seleccionado:', file.name, file.type, file.size);
  statusText.textContent = '📤 Procesando foto...';

  const reader = new FileReader();
  reader.onerror = (err) => {
    console.error('Error al leer archivo:', err);
    statusText.textContent = '❌ Error al leer la foto. Intenta con otra imagen.';
  };
  
  reader.onload = (event) => {
    console.log('Archivo leído, tamaño:', event.target.result.length);
    const img = new Image();
    img.onerror = (err) => {
      console.error('Error al cargar imagen:', err);
      statusText.textContent = '❌ Error al procesar la imagen. Asegúrate de que sea un formato válido (JPG, PNG).';
    };
    
    img.onload = () => {
      console.log('Imagen cargada, dimensiones:', img.width, 'x', img.height);
      canvas.width = img.width;
      canvas.height = img.height;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0);
      const dataUrl = canvas.toDataURL('image/jpeg', 0.9);
      console.log('Imagen convertida a data URL, tamaño:', dataUrl.length);
      sendPhoto(dataUrl);
    };
    img.src = event.target.result;
  };
  
  reader.readAsDataURL(file);
});


