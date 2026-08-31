import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginRequest, AuthResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5250/api/Auth';
  private storageKey = 'biblioteca_auth';

  // Señal reactiva con el usuario actual (null si no hay sesión)
  usuarioActual = signal<AuthResponse | null>(this.cargarSesion());

  constructor(private http: HttpClient) {}

  login(credenciales: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credenciales).pipe(
      tap((respuesta) => {
        localStorage.setItem(this.storageKey, JSON.stringify(respuesta));
        this.usuarioActual.set(respuesta);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.usuarioActual.set(null);
  }

  getToken(): string | null {
    return this.usuarioActual()?.token ?? null;
  }

  estaAutenticado(): boolean {
    return this.usuarioActual() !== null;
  }

  private cargarSesion(): AuthResponse | null {
    const guardado = localStorage.getItem(this.storageKey);
    return guardado ? JSON.parse(guardado) : null;
  }

  obtenerPerfil(): Observable<{ id: number; email: string; nombreCompleto: string; rol: string }> {
    return this.http.get<{ id: number; email: string; nombreCompleto: string; rol: string }>(`${this.apiUrl}/me`);
  }
}