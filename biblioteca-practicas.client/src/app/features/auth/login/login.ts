import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html'
})
export class LoginComponent {
  email = '';
  password = '';
  cargando = false;
  error = '';
  mostrarPassword = false;
  anioActual = new Date().getFullYear();
  mensajePasswordActualizada = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  mensajeSesionExpirada = false;

  onSubmit(): void {
    this.error = '';
    this.cargando = true;

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.cargando = false;
        if (err.status === 403) {
          this.router.navigate(['/verificar-correo'], {
            queryParams: { email: this.email.trim().toLowerCase(), origen: 'login' }
          });
          return;
        }
        if (err.status === 401) {
          this.error = 'Email o contraseña incorrectos.';
        } else {
          this.error = 'Ocurrió un error. Intenta de nuevo.';
        }
      }
    });
  }

  ngOnInit(): void {
  this.route.queryParams.subscribe((params) => {
    this.mensajeSesionExpirada = params['motivo'] === 'sesion-expirada';
    this.mensajePasswordActualizada = params['motivo'] === 'password-actualizada';
  });
}
}