import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Libro } from '../models/libro.model';

@Injectable({
  providedIn: 'root'
})
export class LibroService {
  private apiUrl = 'http://localhost:5250/api/Libros';

  constructor(private http: HttpClient) {}

  getLibros(): Observable<Libro[]> {
    return this.http.get<Libro[]>(this.apiUrl);
  }

  getTodosLosLibros(): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/todos`);
  }

  getLibro(id: number): Observable<Libro> {
    return this.http.get<Libro>(`${this.apiUrl}/${id}`);
  }

  subirLibro(formData: FormData): Observable<Libro> {
    return this.http.post<Libro>(this.apiUrl, formData);
  }

  editarLibro(id: number, datos: { titulo: string; autor?: string; descripcion?: string; categoriaId: number }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, datos);
  }

  cambiarEstado(id: number, activo: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/estado`, activo);
  }

  descargarArchivo(ruta: string, nombreArchivo: string): void {
    const partes = ruta.split('/').filter(p => p);
    const carpeta = partes[1];
    const nombreArchivoServidor = partes[2];
    const url = `http://localhost:5250/api/archivos/${carpeta}/${nombreArchivoServidor}`;
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const urlBlob = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = urlBlob;
        a.download = nombreArchivo || 'documento.pdf';
        a.click();
        window.URL.revokeObjectURL(urlBlob);
      },
      error: (err) => console.error('Error al descargar el archivo', err)
    });
  }
  
  registrarDescarga(id: number): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/${id}/registrar-descarga`,{});
  }

  reemplazarPdf(id: number, archivo: File): Observable<void> {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.put<void>(`${this.apiUrl}/${id}/reemplazar-pdf`, formData);
  }
}