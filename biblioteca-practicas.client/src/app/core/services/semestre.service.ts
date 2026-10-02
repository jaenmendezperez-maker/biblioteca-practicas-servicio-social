import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Semestre } from '../models/semestre.model';
import { Libro } from '../models/libro.model';

@Injectable({
  providedIn: 'root'
})
export class SemestreService {
  private apiUrl = 'http://localhost:5250/api/Semestres';

  constructor(private http: HttpClient) {}

  getSemestres(): Observable<Semestre[]> {
    return this.http.get<Semestre[]>(this.apiUrl);
  }

  getLibrosDeSemestre(id: number): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/${id}/libros`);
  }
}