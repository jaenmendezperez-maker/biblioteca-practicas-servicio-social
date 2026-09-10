import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  mensaje: string;
  tipo: 'exito' | 'error' | 'info';
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toasts = signal<Toast[]>([]);
  private siguienteId = 1;

  mostrar(mensaje: string, tipo: 'exito' | 'error' | 'info' = 'exito'): void {
    const id = this.siguienteId++;
    this.toasts.update((actual) => [...actual, { id, mensaje, tipo }]);

    setTimeout(() => this.cerrar(id), 3500);
  }

  exito(mensaje: string): void {
    this.mostrar(mensaje, 'exito');
  }

  error(mensaje: string): void {
    this.mostrar(mensaje, 'error');
  }

  info(mensaje: string): void {
    this.mostrar(mensaje, 'info');
  }

  cerrar(id: number): void {
    this.toasts.update((actual) => actual.filter((t) => t.id !== id));
  }
}