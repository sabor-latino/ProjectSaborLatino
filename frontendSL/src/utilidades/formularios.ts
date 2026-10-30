import type { FocusEvent, FormEvent } from 'react';

type Control = HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement;

// Marca el campo como inválido para lectores de pantalla (aria-invalid) cuando la persona
// sale de él o cuando el navegador lo rechaza al enviar. Va junto con aria-errormessage.
function marcar(e: FocusEvent<Control> | FormEvent<Control>) {
  const campo = e.currentTarget;
  if (campo.checkValidity()) {
    campo.removeAttribute('aria-invalid');
  } else {
    campo.setAttribute('aria-invalid', 'true');
  }
}

// Uso: <input {...validacionAccesible} aria-errormessage="correo-error" />
export const validacionAccesible = {
  onBlur: marcar,
  onInvalid: marcar,
};
