import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

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
    private authService: AuthService,
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

    this.cargando = true;

    this.http.post(this.apiUrl, {
      nombreCompleto: this.nombreCompleto,
      email: this.email,
      password: this.password,
      rol: 2 // Alumno — el backend lo ignora y fuerza este valor de todas formas
    }).subscribe({
      next: () => {
        // Registro exitoso: inicia sesión automáticamente
        this.authService.login({ email: this.email, password: this.password }).subscribe({
          next: () => {
            this.cargando = false;
            this.router.navigate(['/']);
          },
          error: () => {
            this.cargando = false;
            this.router.navigate(['/login']);
          }
        });
      },
      error: (err) => {
        this.cargando = false;
        if (err.status === 400) {
          this.error = 'Ya existe una cuenta con ese correo.';
        } else {
          this.error = 'Ocurrió un error al crear la cuenta. Intenta de nuevo.';
        }
      }
    });
  }
}