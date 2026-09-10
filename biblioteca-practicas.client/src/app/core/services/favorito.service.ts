import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Libro } from '../models/libro.model';

@Injectable({
  providedIn: 'root'
})
export class FavoritoService {
  private apiUrl = 'http://localhost:5250/api/Favoritos';

  constructor(private http: HttpClient) {}

  getFavoritos(): Observable<number[]> {
    return this.http.get<number[]>(this.apiUrl);
  }

  agregarFavorito(libroId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${libroId}`, {});
  }

  quitarFavorito(libroId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${libroId}`);
  }

  getLibrosFavoritos(): Observable<Libro[]> {
    return this.http.get<Libro []>(`${this.apiUrl}/libros`);
  }
}