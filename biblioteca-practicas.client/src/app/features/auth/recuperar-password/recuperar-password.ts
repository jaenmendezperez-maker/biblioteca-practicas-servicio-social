import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-recuperar-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recuperar-password.html'
})
export class RecuperarPasswordComponent implements OnDestroy {
  paso: 1 | 2 = 1;

  email = '';
  codigo = '';
  nuevaPassword = '';
  confirmarPassword = '';
  mostrarPassword = false;

  cargando = false;
  error = '';
  aviso = '';
  segundosRestantes = 0;
  anioActual = new Date().getFullYear();

  private temporizador: ReturnType<typeof setInterval> | null = null;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnDestroy(): void {
    if (this.temporizador) clearInterval(this.temporizador);
  }

  soloNumeros(): void {
    this.codigo = this.codigo.replace(/\D/g, '').slice(0, 6);
  }

  // Paso 1: pedir el código
  enviarCodigo(): void {
    this.error = '';
    this.aviso = '';

    const correo = this.email.trim().toLowerCase();
    if (!correo || !correo.includes('@')) {
      this.error = 'Escribe tu correo electrónico.';
      return;
    }

    this.cargando = true;

    this.authService.solicitarRecuperacion(correo).subscribe({
      next: () => {
        this.cargando = false;
        this.email = correo;
        this.paso = 2;
        this.aviso = 'Si el correo está registrado, te enviamos un código de 6 dígitos. Vence en 10 minutos.';
        this.iniciarCuentaAtras();
      },
      error: (err) => {
        this.cargando = false;
        this.error =
          typeof err.error === 'string' && err.error
            ? err.error
            : 'Ocurrió un error. Intenta de nuevo.';
      }
    });
  }

  reenviar(): void {
    if (this.segundosRestantes > 0 || this.cargando) return;

    this.error = '';
    this.aviso = '';
    this.cargando = true;

    this.authService.solicitarRecuperacion(this.email).subscribe({
      next: () => {
        this.cargando = false;
        this.codigo = '';
        this.aviso = 'Si el correo está registrado, te enviamos un código nuevo.';
        this.iniciarCuentaAtras();
      },
      error: () => {
        this.cargando = false;
        this.error = 'No se pudo enviar el código. Intenta más tarde.';
      }
    });
  }

  // Paso 2: código + contraseña nueva
  restablecer(): void {
    this.error = '';
    this.aviso = '';

    if (this.codigo.length !== 6) {
      this.error = 'El código tiene 6 dígitos.';
      return;
    }
    if (this.nuevaPassword.length < 6) {
      this.error = 'La contraseña debe tener al menos 6 caracteres.';
      return;
    }
    if (this.nuevaPassword !== this.confirmarPassword) {
      this.error = 'Las contraseñas no coinciden.';
      return;
    }

    this.cargando = true;

    this.authService.restablecerPassword(this.email, this.codigo, this.nuevaPassword).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/login'], { queryParams: { motivo: 'password-actualizada' } });
      },
      error: (err) => {
        this.cargando = false;
        this.error =
          err.status === 400 && typeof err.error === 'string' && err.error
            ? err.error
            : 'Ocurrió un error. Intenta de nuevo.';
      }
    });
  }

  volverAlPaso1(): void {
    this.paso = 1;
    this.codigo = '';
    this.nuevaPassword = '';
    this.confirmarPassword = '';
    this.error = '';
    this.aviso = '';
  }

  private iniciarCuentaAtras(): void {
    if (this.temporizador) clearInterval(this.temporizador);
    this.segundosRestantes = 60;
    this.temporizador = setInterval(() => {
      this.segundosRestantes--;
      if (this.segundosRestantes <= 0 && this.temporizador) {
        clearInterval(this.temporizador);
        this.temporizador = null;
      }
    }, 1000);
  }
}