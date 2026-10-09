import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verificar-correo',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './verificar-correo.html'
})
export class VerificarCorreoComponent implements OnInit, OnDestroy {
  email = '';
  codigo = '';
  cargando = false;
  reenviando = false;
  error = '';
  aviso = '';
  segundosRestantes = 0;
  anioActual = new Date().getFullYear();

  private temporizador: ReturnType<typeof setInterval> | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    this.email = params.get('email') ?? '';

    if (!this.email) {
      this.router.navigate(['/registro']);
      return;
    }

    // Si viene del registro, el código se acaba de enviar: espera 60 s para reenviar.
    // Si viene del login, puede que el código ya venciera: se permite pedir otro de inmediato.
    if (params.get('origen') !== 'login') {
      this.iniciarCuentaAtras();
    } else {
      this.aviso = 'Tu cuenta aún no está verificada. Escribe el código que te enviamos o pide uno nuevo.';
    }
  }

  ngOnDestroy(): void {
    if (this.temporizador) clearInterval(this.temporizador);
  }

  soloNumeros(): void {
    this.codigo = this.codigo.replace(/\D/g, '').slice(0, 6);
  }

  onSubmit(): void {
    this.error = '';
    this.aviso = '';

    if (this.codigo.length !== 6) {
      this.error = 'El código tiene 6 dígitos.';
      return;
    }

    this.cargando = true;

    this.authService.verificarCorreo(this.email, this.codigo).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/']);
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

  reenviar(): void {
    if (this.segundosRestantes > 0 || this.reenviando) return;

    this.error = '';
    this.aviso = '';
    this.reenviando = true;

    this.authService.reenviarCodigo(this.email).subscribe({
      next: () => {
        this.reenviando = false;
        this.codigo = '';
        this.aviso = 'Si el correo está pendiente de verificación, te enviamos un nuevo código.';
        this.iniciarCuentaAtras();
      },
      error: (err) => {
        this.reenviando = false;
        this.error =
          typeof err.error === 'string' && err.error
            ? err.error
            : 'No se pudo enviar el código. Intenta más tarde.';
        // Si el servidor dijo "espera un minuto", evita que insista de inmediato
        if (err.status === 400) this.iniciarCuentaAtras();
      }
    });
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