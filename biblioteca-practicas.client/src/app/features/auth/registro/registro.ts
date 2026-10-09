import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registro.html'
})
export class RegistroComponent {
  nombreCompleto = '';
  email = '';
  password = '';
  confirmarPassword = '';
  mostrarPassword = false;
  mostrarConfirmarPassword = false;

  cargando = false;
  error = '';

  anioActual = new Date().getFullYear();

  private apiUrl = 'http://localhost:5250/api/Usuarios';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  onSubmit(): void {
    this.error = '';

    if (!this.nombreCompleto.trim() || !this.email.trim() || !this.password) {
      this.error = 'Completa todos los campos.';
      return;
    }

    if (this.password.length < 6) {
      this.error = 'La contraseña debe tener al menos 6 caracteres.';
      return;
    }

    if (this.password !== this.confirmarPassword) {
      this.error = 'Las contraseñas no coinciden.';
      return;
    }

    const correo = this.email.trim().toLowerCase();
    const dominiosPermitidos = ['@alumno.buap.mx', '@correo.buap.mx', '@buap.mx'];
    if (!dominiosPermitidos.some(dominio => correo.endsWith(dominio))){
      this.error = 'Usa tu correo instutucional de la BUAP para registrarte.';
      return;
    }

    this.cargando = true;

    this.http.post(this.apiUrl, {
      nombreCompleto: this.nombreCompleto,
      email: this.email,
      password: this.password,
      rol: 2 // Alumno — el backend lo ignora y fuerza este valor de todas formas
    }).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/verificar-correo'], {
          queryParams: { email: this.email.trim().toLowerCase() }
        });
      },
      error: (err) => {
        if (err.status === 400 && typeof err.error === 'string' && err.error.includes('falta verificarlo')) {
          this.router.navigate(['/verificar-correo'], {
            queryParams: { email: this.email.trim().toLowerCase(), origen: 'login' }
          });
          return;
        }
        this.cargando = false;
        if (err.status === 400 && typeof err.error === 'string' && err.error) {
          this.error = err.error;
        } else {
          this.error = 'Ocurrió un error al crear la cuenta. Intenta de nuevo.';
        }
      }
    });
  }
}